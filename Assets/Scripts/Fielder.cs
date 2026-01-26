using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.AI;
using RootMotion.FinalIK;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score, angleDiff, timeToReachLanding;
    private Vector3 actualPos, actualRot, idleRightHand, idleLeftHand;
    public Vector3 targetPosition;
    BallHit ballComp;
    Rigidbody ballRb;
    public Transform ball, throwingArm, rightHand, leftHand, rightFoot, leftFoot;
    public bool canReachInTime, startedRun;
    [SerializeField] FieldManager fm;
    [SerializeField] MultiAimConstraint headAim, neckAim;
    public FielderIK ikControl;
    List<Effector> effectors;
    [SerializeField] GameObject rayTestObject;
    [SerializeField] NavMeshAgent agent;
    Vector3 initialTarget;
    [SerializeField] bool chaseMode;
    float groundY = .001f;
    public Animator animator;
    public Vector3 actualFetchPosition;
    public SmoothInteractionPickup pickScript;
    public AnimationClip idleClip, runningClip, jumpClip, crouchClip, moveRightClip, moveLeftClip, diveRightClip, diveLeftClip,chasePickupClip, pickUpClip, throwClip, kneelClip;

    private void OnEnable()
    {        
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        idleRightHand = rightHand.localPosition;
        idleLeftHand = leftHand.localPosition;
        animator = GetComponent<Animator>();
        ikControl = GetComponent<FielderIK>();
        pickScript = this.GetComponent<SmoothInteractionPickup>();
    }


    #region keeper
    public void KeeperRecieve(Vector3 targetPosition, Transform ball, bool isEdge=false)
    {        
        StartCoroutine(SetTarget(targetPosition, ball, isEdge));
    }

    IEnumerator SetTarget(Vector3 targetPosition, Transform ball, bool isEdge = false)
    {
        ballComp = ball.GetComponent<BallHit>();
        this.ball = ball;

        if (targetPosition == Vector3.zero)
        {
            while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 7)
            {
                yield return null;
            }
            targetPosition = ball.position;
            targetPosition.y += 1f;
            rightHand.position = leftHand.position = targetPosition;
        }

        else
        {
            while (Mathf.Abs(targetPosition.x - transform.position.x) > .2f)
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 60f)
                    break;
                if(Mathf.Abs(targetPosition.x - transform.position.x) < 1f)
                {
                    ikControl.PlayAnimation(idleClip);
                    break;
                }

                AnimationClip clipToPlay = null;
                float directionMultiplier = 0; 

                if (targetPosition.x > transform.position.x)
                {
                    Debug.Log("Stepping Left...");
                    clipToPlay = moveLeftClip;
                    directionMultiplier = 1f;
                                             
                }
                else
                {
                    Debug.Log("Stepping Right...");
                    clipToPlay = moveRightClip;
                    directionMultiplier = -1f;
                }

                if (clipToPlay != null)
                {
                    ikControl.PlayAnimation(clipToPlay);

                    yield return new WaitForSeconds(clipToPlay.length);

                    Vector3 newPos = transform.position;
                    newPos.x += (2.4f * directionMultiplier);
                    transform.position = newPos;
                }
                else
                {
                    yield return null;
                }
            }
            //ikControl.PlayAnimation(idleClip);
            if (targetPosition.y > 12.93f)
            {
                Debug.Log("jump..");
                ikControl.PlayAnimation(jumpClip);
            }
            else if (targetPosition.y < 3f)
            {
                // ikControl.PlayAnimation(crouchClip);
            }
        }

        if (!ballComp.secondTouch)
        {
            float addConstant = targetPosition.x > transform.position.x ? -1 : 1;
            fm.marker.position = targetPosition;
            rightHand.position = leftHand.position = targetPosition;
        }

        yield return new WaitUntil(() => Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 55);

        Debug.Log("recive start");

        pickScript.pickupObject = fm.marker.GetComponent<InteractionObject>();
        float ballArrivalTime = GetBallArrivalTime(fm.marker.position, ball.GetComponent<Rigidbody>());
        float delay = ballArrivalTime - handReachDuration - earlyBias;

        delay = Mathf.Clamp(delay, 0f, 1.2f);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        pickScript.StartPickup(false);

        float time = 0;
        float duration = .4f;
        float lerpValue;

        while (time <= duration)
        {
            time += Time.deltaTime;
            lerpValue = Mathf.Lerp(0, 1, time / duration);
            yield return null;
        }
    }

    public void DropBall(bool keeper)
    {
        //Debug.Log("drop call , keeper? " + keeper);
        StartCoroutine(ReleaseTarget(keeper));
    }

    float GetBallArrivalTime(Vector3 catchPoint, Rigidbody ballRb)
    {
        Vector3 toTarget = catchPoint - ballRb.position;
        Vector3 velocity = ballRb.velocity;

        float speedTowardsTarget = Vector3.Dot(velocity, toTarget.normalized);

        if (speedTowardsTarget <= 0.01f)
            return float.PositiveInfinity;

        return toTarget.magnitude / speedTowardsTarget;
    }

    [SerializeField] float handReachDuration = 0.55f; // tune once
    [SerializeField] float earlyBias = 0.03f; // micro-anticipation (human-like)


    IEnumerator ReleaseTarget(bool keeper)
    {
        //pickScript.StartDrop();

        if (keeper)
        {
            Gameplay.instance.deliveryDead = true;
        }
        else
        {
            StartCoroutine(FielderPickupThrow());
        }
        yield return null;
        //Debug.Log("recive done");
    }
    #endregion

    public void Initiate(Transform ball)
    {
        StartCoroutine(StartField(ball));
    }

    IEnumerator StartField(Transform ball)
    {
        yield return new WaitForSeconds(0.2f);

        targetPosition = actualFetchPosition;

        initialTarget = targetPosition;

        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;

        //targetPosition = FindIdealInterceptPoint(transform.position, ball.position, ballRb.velocity, gameObject.CompareTag("DeepFielder"));
        fm.marker.position = targetPosition;

        if (!ballComp.groundShot)
        {
            targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.drag);
            fm.marker.position = targetPosition;
            //if(!FielderCanReachOnTime(targetPosition))
            //{
            //    targetPosition = initialTarget;
            //}
        }
        if (!Gameplay.instance.stadiumBounds.Contains(targetPosition))
        {
            targetPosition = Gameplay.instance.stadiumBounds.ClosestPoint(targetPosition);
        }
        targetPosition.y = transform.position.y;


        //-----------------------------------------||------------------------------------------------\\


        if(ballRb.velocity.magnitude<10)
        {
            targetPosition = new Vector3(ball.position.x, transform.position.y, ball.position.z);
        }
        if (IsBallComingAtFielder() && !this.gameObject.CompareTag("DeepFielder") && ballComp.groundShot && ballRb.velocity.magnitude > 3000)
        {
            Debug.Log("Coming to fielder");
            StartCoroutine(WaitForBall());
        }

        else
        {
            Debug.Log("away from fielder");
            StartCoroutine(RunToBall());
        }
        yield break;
    }

    Vector3 FindIdealInterceptPoint(Vector3 fielderPos, Vector3 ballPos, Vector3 ballVelocity, bool isDeepFielder)
    {
        // Flatten everything to ground
        Vector3 ballDir = ballVelocity;
        ballDir.y = 0f;

        float ballSpeed = ballDir.magnitude;
        if (ballSpeed < 0.1f)
            return ballPos;

        ballDir.Normalize();

        Vector3 toBall = ballPos - fielderPos;
        toBall.y = 0f;

        // Project ball relative to fielder ONTO ball direction
        float forwardDistance = Vector3.Dot(toBall, ballDir);

        // --- CRITICAL RULE ---
        // Never allow target behind fielder
        forwardDistance = Mathf.Max(forwardDistance, 0f);

        float extraLead;

        if (isDeepFielder)
        {
            // Deep fielder commits further FORWARD
            extraLead = Mathf.Lerp(8f, 18f, Mathf.Clamp01(ballSpeed / 25f));
        }
        else
        {
            // Infield cuts early
            extraLead = Mathf.Lerp(4f, 10f, Mathf.Clamp01(ballSpeed / 20f));
        }

        float interceptDistance = forwardDistance + extraLead;

        Vector3 interceptPoint = fielderPos + ballDir * interceptDistance;
        interceptPoint.y = fielderPos.y;

        return interceptPoint;
    }

    bool targetBall = false;

    public Transform fielderTargetMark;

    public float distanceToTarget;

    float checkDistanceThreshold = 1;

    float minLookAhead = 1.2f;        // never run directly to ball
    float lookAheadFactor = 0.15f;

    Vector3 ComputeTarget(bool restart=false)
    {
        if (targetBall)
        {
            if (restart)
            {
                Vector3 ballVel = ballRb.velocity;
                Vector3 flatDir = new Vector3(ballVel.x, 0f, ballVel.z).normalized;
                float speed = ballVel.magnitude;
                float lookAhead = Mathf.Max(minLookAhead, speed * lookAheadFactor);
                Vector3 interceptPoint = ball.transform.position + flatDir * lookAhead;
                targetPosition = new Vector3(interceptPoint.x, transform.position.y, interceptPoint.z);
            }
            else
            {
                targetPosition = new Vector3(ball.transform.position.x, transform.position.y, ball.transform.position.z);
            }
        }

        else if (chaseMode)
        {
            Vector3 ballVelocity = ballRb.velocity;
            Vector3 ballDir = new Vector3(ballVelocity.x, 0f, ballVelocity.z).normalized;
            float ballSpeed = ballVelocity.magnitude;
            float interceptDistance = (ballSpeed * 5f) / 20f;
            float sideBuffer = 2f;
            Vector3 localRight = Vector3.Cross(Vector3.up, transform.forward).normalized;
            Vector3 throwingHandSide = isRightHanded ? -localRight : localRight;
            targetPosition = ball.position + (ballDir * interceptDistance) + (throwingHandSide * sideBuffer);
            targetPosition.y = transform.position.y;
        }

        fielderTargetMark.position = targetPosition;
        return targetPosition;
    }

    public bool isRightHanded;

    IEnumerator RunToBall(bool restart=false)
    {
        if (restart) Debug.Log("second time");
        ikControl.PlayAnimation(runningClip);
        while (!ballComp.stopTriggered)
        {
            float ballSpeed = ballRb.velocity.magnitude;
            Debug.Log("running");
            distanceToTarget = Vector3.Distance(transform.position,ball.position);

            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                Debug.Log("bk hr");
                yield break;
            }

            if (!ballComp.groundShot)
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f)
                {
                    if (ballComp.groundShot)
                    {
                        Debug.Log("bk hr");
                        targetPosition = initialTarget;
                        yield break;
                    }
                    if (ballComp.fielderReached)
                    {
                        Debug.Log("bk hr");
                        StartCoroutine(ReachedBall());
                        yield break;
                    }
                    Debug.Log(gameObject.name + " reached target go for");
                    //targetBall = true;
                    StartCoroutine(WaitForBall());
                    yield break;
                }
            }

            else
            {
                ComputeTarget(restart);

                Vector3 moveDirection = (targetPosition - transform.position).normalized;

                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 3f);
                }

                if (ballSpeed < 25 && !chaseMode && !targetBall)
                {
                    Debug.Log("ball slowed");
                    transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, -.37f);
                    targetBall = true;
                }

                if (ShouldChase(ball, transform.position) && !chaseMode)
                {
                    transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, -1.47F);
                    Debug.Log("chasee");
                    chaseMode = true;
                }

                if (chaseMode)
                {
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1)
                    {
                        Debug.Log("bk hr");
                        ikControl.PlayAnimation(idleClip);
                        Debug.Log(gameObject.name + " reached ball");
                        ballComp.fieldedPlayer = this.gameObject;
                        StartCoroutine(ReachedBall());
                        yield break;
                    }
                }

                else if (targetBall || restart)
                {
                    // 10 - 1
                    // x - y
                    checkDistanceThreshold = ballSpeed * 2.76f / 10;
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < checkDistanceThreshold)
                    {
                        Debug.Log("bk hr");
                        ikControl.PlayAnimation(idleClip);
                        Debug.Log(gameObject.name + " reached ball");
                        ballComp.fieldedPlayer = this.gameObject;
                        StartCoroutine(ReachedBall());
                        yield break;
                    }
                }

                else if (Vector3.Distance(transform.position, targetPosition) < 4)
                {
                    checkDistanceThreshold = ballSpeed * 1 / 10;
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < checkDistanceThreshold)
                    {
                        StartCoroutine(ReachedBall());
                        yield break;
                    }

                    if (IsBallComingAtFielder()&&!restart)
                    {
                        if (ballRb.velocity.magnitude > 22)
                        {
                            Debug.Log("bk hr");
                            StartCoroutine(WaitForBall());
                            yield break;
                        }
                        else
                        {
                            Debug.Log(gameObject.name + " reached, ball  coming towards but fast");
                        }
                    }
                    else if (!targetBall)
                    {
                        targetBall = true;
                        Debug.Log(gameObject.name + " reached, ball not coming towards");
                    }
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);              
            yield return null;
        }
        //Debug.Log("bk hr");
        //Debug.Log("other fiedler completed");
        //StopAll();
        yield return null;
        
    }

    void StopAll()
    {
        //agent.Stop();
        ikControl.PlayAnimation(idleClip);
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + "  waiting for ball");

        ikControl.PlayAnimation(idleClip);
        //while (Vector2.Distance(new Vector2(transform.position.x,transform.position.z), new Vector2(ball.position.x, ball.position.z))>4)
        while (!ballComp.fielderReached)
        {
            if (!ballComp.groundShot)
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < checkDistanceThreshold)
                {
                    StartCoroutine(ReachedBall(true));
                    yield break;
                }
            }
            else
            {
                /* 
                21     10
                x      y


                21y=10x
                y=10x/21
                */

                float checkThreshold = (1*ballRb.velocity.magnitude)/ 10;

                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < checkThreshold)
                {
                    Debug.Log("velocity controllable " + ballRb.velocity.magnitude);
                    StartCoroutine(ReachedBall());
                    yield break;
                }
                if (ballComp.keeperReceive)
                {
                    Gameplay.instance.deliveryDead = true;
                    break;
                }
                Vector3 moveDirection = (ball.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                lookRotation.x = actualRot.x;
                lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 100f);

                if (Gameplay.instance.deliveryDead)
                {
                    yield break;
                }
                if (ballRb.velocity.magnitude < 50)
                {
                    Debug.Log("slowed beyound thrshold");
                    //agent.SetDestination(new Vector3(ball.position.x, transform.position.y, ball.position.z));
                    targetBall = true;
                    StartCoroutine(RunToBall(true));
                    yield break;
                }
            }
            yield return null;
        }
        StartCoroutine(ReachedBall(true));
    }

    IEnumerator ReachedBall(bool waited = false)
    {
        Debug.Log("111");

        Debug.Log("222");

        //ikControl.PlayAnimation(pickUpClip);

        float pickupDuration = 0.5f;

        if (ballRb.velocity.magnitude < 20f && !chaseMode)
        {
            //leftFoot.position = new Vector3(ball.position.x - .2f, transform.position.y, ball.position.z);
            //rightFoot.position = new Vector3(leftFoot.position.x-.3f, transform.position.y, leftFoot.position.z-.3f);
            Debug.Log("slow front");
            pickupDuration = .4f;
        }

        if (waited)
        {
            Debug.Log("waited");
            //ikControl.PlayAnimation(kneelClip);
        }

        //else if (chaseMode)
        //{
        //    Debug.Log("pkup chase");
        //}

        else
        {
            //ikControl.PlayAnimation(pickUpClip);
            Debug.Log("pkup");
        }

        // 21 - 0.5
        // x  -  y
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        pickScript.StartPickup(chaseMode||waited? true:false);
        float checkDuration = (.5f * ballRb.velocity.magnitude) / 21;

        Vector3 incomingDir = (transform.position - ball.position);

        incomingDir.y = 0f;
        incomingDir.Normalize();

        float handAhead = 0.35f;

        Vector3 predictedXZ = transform.position + incomingDir * handAhead;

        Vector3 interceptPoint = predictedXZ;

        interceptPoint.y = ball.position.y;

        rightHand.position = interceptPoint;
        if(!chaseMode)
            leftHand.position = interceptPoint;

        float timer = 0;            
        float lerpValue = 0;
        while (timer <= checkDuration)
        {                

            timer += Time.deltaTime;
            lerpValue = Mathf.Lerp(0, 1, timer / pickupDuration);
            //ikControl.SetIKWeight(lerpValue);
            yield return null;
        }

        timer = 0;
        pickupDuration = 0.4f;
        lerpValue = 1;
        while (timer <= pickupDuration)
        {
            timer += Time.deltaTime;
            lerpValue = Mathf.Lerp(1, 0, timer / pickupDuration);
            //ikControl.SetIKWeight(lerpValue);
            yield return null;
        }

        //if (!ballComp.stopTriggered)
        //{
        //    Debug.Log("no stop");
        //    if (ShouldChase(ball,transform.position)) chaseMode = true;
        //    else targetBall = true;
        //    if(ballComp.fieldedPlayer==this)
        //    {
        //        ballComp.fieldedPlayer = null;
        //        ballComp.fielderReached = false;
        //    }
        //    StartCoroutine(RunToBall(true));
        //    yield break;
        //}

        //else
        //{
        //    if(ballComp.stopper==this.gameObject)
        //    {
        //        ikControl.PlayAnimation(idleClip);
        //        pickScript.pickupObject = null;
        //        ikControl.SetIKWeight(0);
        //        if (!ballComp.groundShot)
        //        {
        //            ikControl.PlayAnimation(idleClip);
        //            ballRb.isKinematic = true;
        //            Gameplay.instance.deliveryDead = true;
        //            Debug.Log("caught");
        //            Gameplay.instance.Out();
        //            yield break;
        //        }
        //        Debug.Log("commp");
                
        //        #region dive/pick action
        //        //Vector3 toBall = ball.position - transform.position;
        //        //float distance = toBall.magnitude;
        //        //Vector3 toBallNormalized = toBall.normalized;

        //        //float side = Vector3.Dot(transform.right, toBallNormalized);     // + right, - left
        //        //float forward = Vector3.Dot(transform.forward, toBallNormalized); // + in front, - behind

        //        //// Set some tuning thresholds
        //        //float sideThresholdToDive = 0.5f;
        //        //float diveDistanceThreshold = 2.5f;
        //        //float frontThreshold = 0.6f;

        //        //if (forward > frontThreshold)
        //        //{
        //        //    if (Mathf.Abs(side) > sideThresholdToDive && distance > diveDistanceThreshold)
        //        //    {
        //        //        // Ball is far to the side → dive
        //        //        if (side > 0)
        //        //            animator.SetTrigger("DiveRight");
        //        //        else
        //        //            animator.SetTrigger("DiveLeft");
        //        //    }
        //        //    else
        //        //    {
        //        //        // Ball is close or centered → pick from front
        //        //        if (ball.position.y > 6f)
        //        //        {
        //        //            animator.Play("jump");
        //        //        }
        //        //        else
        //        //        {
        //        //            animator.SetTrigger("Pick");
        //        //        }
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    // Ball is on side or behind, close enough to pick
        //        //    if (side > 0)
        //        //        animator.SetTrigger("Pick");
        //        //    else
        //        //        animator.SetTrigger("Pick");
        //        //}
        //        #endregion                
        //        //StartCoroutine(FielderPickupThrow());
        //    }
        //}            
                
        Debug.Log("333");        
    }

    bool ballWithinReach()
    {
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f)
        {
            return true;
        }
        return false;
    }

    IEnumerator GrabBall()
    {
        yield return new WaitForEndOfFrame();
    }

    [SerializeField] float refDistance, refSpeed;

    IEnumerator FielderPickupThrow()
    {
        if (gameObject.name == "keeper")
        {
            Debug.Log("fld done");
            KeeperRecieve(ball.position, ball);
            Gameplay.instance.deliveryDead = true;
            yield break;
        }
        ball.transform.position = throwingArm.position;
        ball.transform.SetParent(throwingArm);
        Vector3 lookDirection = (fm.keeper.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        transform.rotation = lookRotation;
        yield return new WaitForSeconds(.7f);

        ikControl.PlayAnimation(throwClip);
        yield return new WaitUntil(() => throwable);
        //ball.SetParent(null, true);
        
        // --- NEW LOGIC
        
        Vector3 ballPosFlat = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 keeperPosFlat = new Vector3(fm.keeper.position.x, 0, fm.keeper.position.z);
        float distToKeeper = Vector3.Distance(ballPosFlat, keeperPosFlat);

        float pitchRatio = (distToKeeper > 25f) ? 0.6f : 1.0f;

        Vector3 dirToKeeper = (fm.keeper.position - ball.position).normalized;
        Vector3 pitchPoint = ballPosFlat + (dirToKeeper * (distToKeeper * pitchRatio));

        //if (distToKeeper <= 25f) pitchPoint -= dirToKeeper * 3.0f;

        Debug.Log("throw call to "+pitchPoint);
        //pitchPoint.y = 0;

        float maxArcHeight = 60f;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float verticalVelocity = Mathf.Sqrt(2 * gravity * maxArcHeight);

        float flightTime = 2 * (verticalVelocity / gravity);

        float distanceToPitch = Vector3.Distance(ballPosFlat, pitchPoint);
        float horizontalSpeed = distanceToPitch / flightTime;
        Vector3 latePos = ball.position;

        Vector3 velocity = dirToKeeper * horizontalSpeed;
        velocity.y = verticalVelocity;
        Debug.DrawLine(ball.position, pitchPoint, Color.red, 15f);
        ball.SetParent(null, true);
        ball.position = latePos;
        //ballRb.WakeUp();
        //ballRb.isKinematic = false;
        //ballRb.velocity = velocity;
        
        //ballRb.AddForce(velocity);

        //ThrowToTarget(ballRb, ball.position, fm.keeper.position, 5f);

        Vector3 keeperRight = fm.keeper.right;

        while (Vector2.Distance(new Vector2(ball.position.x, ball.position.z), new Vector2(fm.keeper.position.x, fm.keeper.position.z)) > 4f)
        {
            Vector3 toBall = ball.position - fm.keeper.position;
            float lateralOffset = Vector3.Dot(toBall, keeperRight);
            if (lateralOffset > 0.2f)
            {
                fm.keeper.GetComponent<FielderIK>().PlayAnimation(moveRightClip);
            }
            else if (lateralOffset < -0.2f)
            {
                fm.keeper.GetComponent<FielderIK>().PlayAnimation(moveLeftClip);
            }
            Vector3 sidewaysMove = keeperRight * lateralOffset;

            Vector3 newPos = Vector3.MoveTowards(fm.keeper.position, fm.keeper.position + sidewaysMove, Time.deltaTime * 10);
            newPos.z = fm.keeper.position.z;
            fm.keeper.position = newPos;

            if (Vector3.Distance(ball.position, fm.keeper.position) < 4)
            {
                fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);
            }
            yield return null;
        }
        Debug.Log("keeper catchig");
        fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);
        Debug.Log("fld done");

        yield return new WaitForSeconds(0.3f);

        Gameplay.instance.deliveryDead = true;

        StopAllCoroutines();
    }

    private Vector3 CalculateVelocityForSpeed(Vector3 target, float speed)
    {
        Vector3 origin = ball.position;
        Vector3 toTarget = target - origin;

        // Calculate time based on 3D distance and speed
        float distance = toTarget.magnitude;
        float time = distance / speed;

        // X and Z components (Constant velocity)
        float vx = toTarget.x / time;
        float vz = toTarget.z / time;

        // Y component (Accounting for gravity)
        // Formula: y = v0y*t + 0.5*g*t^2  => v0y = (y - 0.5*g*t^2) / t
        float vy = (toTarget.y - (0.5f * Physics.gravity.y * time * time)) / time;

        return new Vector3(vx, vy, vz);
    }

    public static void ThrowToTarget(Rigidbody rb, Vector3 start, Vector3 target, float flightTime)
    {
        Vector3 displacement = target - start;

        Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
        Vector3 displacementY = Vector3.up * displacement.y;

        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 velocityY =
            displacementY / flightTime +
            Vector3.up * (gravity * flightTime * 0.5f);

        Vector3 velocityXZ = displacementXZ / flightTime;

        rb.velocity = velocityXZ + velocityY;
    }


    #region HelperMethods

    private bool IsBallComingAtFielder()
    {
        Vector3 ballVelocity = ballRb.velocity;
        ballVelocity.y = 0f;

        if (ballVelocity.sqrMagnitude < 0.01f)
            return false;

        Vector3 ballToFielder = transform.position - ball.position;
        ballToFielder.y = 0f;

        float dot = Vector3.Dot(ballVelocity.normalized, ballToFielder.normalized);

        // dot > 0 → ball moving toward fielder
        return dot > 0.6f; // ~53 degrees cone
    }

    bool FielderCanReachOnTime(Vector3 position)
    {
        float distance = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(transform.position.x, transform.position.z)) + 0.57f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.drag);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(positionAtReqTime.x, positionAtReqTime.z));
        return fielderDistanceToPredictedPos / runSpeed <= timeReq;
    }

    Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float ballDrag)
    {
        float timestep = Time.fixedDeltaTime;
        Vector3 currentPosition = initialPosition;
        Vector3 previousPosition = initialPosition;
        Vector3 currentVelocity = velocity;

        float groundY = transform.position.y;
        int maxSteps = 1000;

        for (int i = 0; i < maxSteps; i++)
        {
            previousPosition = currentPosition;

            currentVelocity += Physics.gravity * timestep;

            currentVelocity /= (1f + ballDrag * timestep);

            currentPosition += currentVelocity * timestep;

            //if (Physics.Linecast( previousPosition, currentPosition, out RaycastHit hit))
            //{
            //    if (hit.collider.gameObject.CompareTag("rayTest")) return hit.point;
            //}

            if (currentPosition.y <= groundY)
            {
                return currentPosition;
            }
        }

        return currentPosition;
    }


    bool ShouldChase(Transform ball, Vector3 fielderPosition)
    {
        Vector3 currentPos = ball.position;
        Vector3 previousPos = ball.position - ball.GetComponent<Rigidbody>().velocity * Time.fixedDeltaTime;

        Vector2 ballDirection = new Vector2(currentPos.x - previousPos.x, currentPos.z - previousPos.z).normalized;

        Vector2 fielderToBall = new Vector2(currentPos.x - fielderPosition.x, currentPos.z - fielderPosition.z);

        float distanceAlongDirection = Vector2.Dot(fielderToBall, ballDirection);

        return distanceAlongDirection > 0;
    }

    #endregion

    public void StopField()
    {
        ikControl.PlayAnimation(idleClip);
        if (isKeeper) KeeperReset();
        else
            Reset();
    }

    public bool isKeeper;

    public void KeeperReset()
    {
        //agent.Stop();
        throwable = false;
        chaseMode = false;
        transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, 0.02f);
        ikControl.PlayAnimation(idleClip);
        rightHand.localPosition = idleRightHand;
        leftHand.localPosition = idleLeftHand;
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        ikControl.SetIKWeight(0);
        this.enabled = false;
    }

    public void Reset()
    {
        //agent.Stop();
        throwable = false;
        chaseMode = false;
        transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, 0.02f);
        ikControl.PlayAnimation(idleClip);
        StopAllCoroutines();
        rightHand.localPosition = idleRightHand;
        leftHand.localPosition = idleLeftHand;
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        ikControl.SetIKWeight(0);
        this.enabled = false;
    }

    public bool throwable;

    public void Throw()
    {
        Debug.Log("throw");
        throwable = true;
    }
}
