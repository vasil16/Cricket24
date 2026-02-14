using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.AI;
using RootMotion.FinalIK;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
    [SerializeField] GameObject rayTestObject;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] bool chaseMode;
    float groundY = .001f;
    public Animator animator;
    public SmoothInteractionPickup pickScript;
    public AnimationClip idleClip, runningClip, jumpClip, crouchClip, moveRightClip, moveLeftClip, diveRightClip, diveLeftClip,chasePickupClip, pickUpClip, throwClip, kneelClip;

    private void OnEnable()
    {        
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        animator = GetComponent<Animator>();
        ikControl = GetComponent<FielderIK>();
        pickScript = this.GetComponent<SmoothInteractionPickup>();
        groundY = transform.position.y;
    }


    #region keeper


    [SerializeField] float handReachDuration = 0.52362f; // tune once
    [SerializeField] float earlyBias = 0.03f; // micro-anticipation (human-like)

    public void KeeperRecieve(Vector3 targetPosition, Transform ball, bool isEdge=false)
    {        
        StartCoroutine(SetTarget(targetPosition, ball, isEdge));
    }

    IEnumerator SetTarget(Vector3 targetPosition, Transform ball, bool isEdge = false)
    {
        ballComp = ball.GetComponent<BallHit>();
        this.ball = ball;
        ballRb = ball.GetComponent<Rigidbody>();
        if (targetPosition == Vector3.zero)
        {
            while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > GetCollectionStartDistance())
            {
                yield return null;
            }
            pickScript.pickupObject = ball.GetComponent<InteractionObject>();
            pickScript.StartPickup(false);
            yield break;
        }

        else
        {
            while (Mathf.Abs(targetPosition.x - transform.position.x) > .2f)
            {
                Debug.Log("target x " + targetPosition.x + " current x " + transform.position.x);
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 60f)
                    break;
                if(Mathf.Abs(targetPosition.x - transform.position.x) < 1f)
                {
                    ikControl.PlayAnimation(idleClip);
                    break;
                }
                float direction = (targetPosition.x > transform.position.x) ? -1f : 1f;
                if (targetPosition.x > transform.position.x)
                {
                    Debug.Log("Stepping Left...");                                             
                    yield return new WaitUntil(() => pickScript.StrafeStep(direction));
                    Debug.Log("Stepcom");
                }
                else
                {
                    Debug.Log("Stepping Right...");
                    yield return new WaitUntil(() => pickScript.StrafeStep(direction));
                }

                yield return null;
                
            }
            if (targetPosition.y > 12.93f)
            {
                Debug.Log("jump..");
                //ikControl.PlayAnimation(jumpClip);
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
        }

        //yield return new WaitUntil(() => Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 75);
        yield return new WaitUntil(() => CanStartPickup(Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)),true));
        //yield return new WaitUntil(()=>ShouldStartCollection(ball, out neededSpeed));

        Debug.Log("recive start");

        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        float ballArrivalTime = GetBallArrivalTime(fm.marker.position, ball.GetComponent<Rigidbody>());
        float delay = ballArrivalTime - handReachDuration - earlyBias;

        delay = Mathf.Clamp(delay, 0f, 1.2f);

        //if (delay > 0f)
        //    yield return new WaitForSeconds(delay);

        pickScript.StartPickup(false);
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
        pickScript.interactionSystem.enabled = true;
        StartCoroutine(StartField(ball));
    }

    IEnumerator StartField(Transform ball)
    {
        yield return new WaitForSeconds(0.2f);

        targetPosition.y = groundY;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;

        fm.marker.position = targetPosition;

        if (!ballComp.groundShot)
        {
            Debug.Log("airball cal");
            targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.drag);
            fm.marker.position = targetPosition;
            StartCoroutine(RunToBall(false));
        }        

        else if (IsBallComingAtFielder() && !this.gameObject.CompareTag("DeepFielder") && ballComp.groundShot)
        {
            Debug.Log("Coming to fielder");
            StartCoroutine(WaitForBall());
        }

        else
        {
            Debug.Log("away from fielder");
            StartCoroutine(RunToBall(false));
        }
    }


    public Transform fielderTargetMark;

    public float distanceToTarget;

    float checkDistanceThreshold = 1;

    float minLookAhead = 1.2f;        // never run directly to ball
    float lookAheadFactor = 0.15f;

    void ComputeTarget(bool restart)
    {        
        if (restart)
        {
            //Debug.Log("resss");
            //Vector3 ballVel = ballRb.velocity;
            //Vector3 flatDir = new Vector3(ballVel.x, 0f, ballVel.z).normalized;
            //float speed = ballVel.magnitude;
            //float lookAhead = Mathf.Max(minLookAhead, speed * lookAheadFactor);
            //Vector3 interceptPoint = ball.transform.position + flatDir * lookAhead;
            //targetPosition = new Vector3(interceptPoint.x, groundY, interceptPoint.z);
            targetPosition = ball.position;
        }
        else if (chaseMode)
        {
            Vector3 ballVelocity = ballRb.velocity;
            Vector3 ballDir = new Vector3(ballVelocity.x, 0f, ballVelocity.z).normalized;
            float ballSpeed = ballVelocity.magnitude;

            float interceptDistance = ballSpeed * 0.25f;   // your logic is fine
            float sideBuffer = 500f;                          // this WILL work now

            Vector3 lateral = Vector3.Cross(Vector3.up, ballDir).normalized;
            Vector3 throwingHandSide = isRightHanded ? lateral : -lateral;

            targetPosition =
                ball.position +
                (ballDir * interceptDistance) +
                (throwingHandSide * sideBuffer);

            targetPosition.y = groundY;
        }
        else if(!ballComp.groundShot)
        {

        }
    }

    public bool isRightHanded;

    float GetCollectionStartDistance()
    {
        float ballSpeed = ballRb.velocity.magnitude;
        
        float reqDistance = ballSpeed * handReachDuration;
        return reqDistance;
    }

    IEnumerator RunToBall(bool restart)
    {
        if (restart) Debug.Log("second time");
        ikControl.PlayAnimation(runningClip);
        float distToBall = 0;
        while (!ballComp.stopTriggered)
        {
            //distToBall = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z));
            distToBall = Vector3.Distance(transform.position, ball.position);
            distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z));
            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                yield break;
            }

            if (!ballComp.groundShot)
            {
                //if (distanceToTarget<1f)
                //{
                //    Debug.Log("air ball reached taret");
                //    StartCoroutine(GrabBall());
                //    yield break;
                //}
                if (CanStartPickup(Vector3.Distance(ball.position,transform.position)))
                {
                    Debug.Log("air ball reached taret");
                    StartCoroutine(GrabBall());
                    yield break;
                }
            }
            else
            {
                ComputeTarget(restart);

                if (ShouldChase(ball, transform.position))
                {
                    chaseMode = true;
                }

                Vector3 moveDirection = (targetPosition - transform.position).normalized;
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); 
                }                

                if (distanceToTarget <= 2f)
                {
                    if (!chaseMode && IsBallComingAtFielder() && ballRb.velocity.magnitude > 30)
                    {
                        StartCoroutine(WaitForBall());
                        yield break;
                    }
                    else
                    {

                        if (!chaseMode)
                            restart = true;
                    }
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
            
            Vector3 moveDir = (targetPosition - transform.position).normalized;
            if (chaseMode)
            {
                if(distanceToTarget<=1)
                {
                    Debug.Log("Ball in hand range for chase- Picking up " + distToBall);
                    ballComp.fieldedPlayer = this.gameObject;
                    //StartCoroutine(ReachedBall());
                    //ikControl.PlayAnimation(idleClip);
                    fm.marker.position = ball.position;
                    ikControl.PlayAnimation(idleClip);
                    pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                    pickScript.StartPickup(false);
                    yield break;
                }
            }
            else
            {
                if (CanStartPickup(distToBall))
                {
                    Debug.Log("Ball in hand range - Picking up " + distToBall);
                    ballComp.fieldedPlayer = this.gameObject;
                    //StartCoroutine(ReachedBall());
                    //ikControl.PlayAnimation(idleClip);
                    fm.marker.transform.position = ball.position;
                    ikControl.PlayAnimation(idleClip);
                    pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                    pickScript.StartPickup(false);
                    yield break;
                }

                //if()
            }
            yield return null;
        }
    }


    //public bool CanStartPickup(float distanceToBall, bool keeper=false)
    //{
    //    if(keeper)
    //    {
    //        if(distanceToBall < GetCollectionStartDistance())
    //        {
    //            return true;
    //        }
    //    }
    //    else if(distanceToBall<15&&distanceToBall<GetCollectionStartDistance())
    //    {
    //        return true;
    //    }
    //    return false;
    //}

    public bool CanStartPickup(float distanceToBall, bool isKeeper = false)
    {
        // 1. Setup Constants
        // The fixed duration of your picking action

        // How far the character can physically reach (in Unity units/meters)
        // The keeper usually has a longer reach (diving/arms) than a normal player.
        float maxReach = isKeeper ? 2.5f : 2.5f;

        // 2. Calculate Prediction
        // How far the ball moves while the animation is playing
        float ballSpeed = ballRb.velocity.magnitude;
        float predictionBuffer = ballSpeed * handReachDuration;

        // 3. Final Decision
        // We add the buffer to the current distance. 
        // If the ball is moving away, this predicts where it will be when the hand closes.
        // If (Current Dist + Movement) is still inside MaxReach, we can pick it up.
        if ((distanceToBall + predictionBuffer) <= maxReach)
        {
            return true;
        }

        return false;
    }



    public void StopAll()
    {
        ikControl.PlayAnimation(idleClip);
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + " Enter WaitForBall");
        ikControl.PlayAnimation(idleClip);

        while (!ballComp.fielderReached)
        {
            float distToBall = Vector3.Distance(transform.position, ball.position);

            Vector3 lookDir = (ball.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

            if (CanStartPickup(distToBall))
            {
                //StartCoroutine(ReachedBall(true));
                fm.marker.position = ball.position;
                pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                pickScript.StartPickup(false);
                yield break;
            }

            if (ballRb.velocity.magnitude < 5.0f)
            {
                Debug.Log("restart runn");
                StartCoroutine(RunToBall(true));
                yield break;
            }
            yield return null;
        }
        ikControl.PlayAnimation(idleClip);
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        pickScript.StartPickup(false);
    }

    IEnumerator GrabBall()
    {
        ikControl.PlayAnimation(idleClip);
        while(Vector3.Distance(ball.position,transform.position)<GetCollectionStartDistance())
        {
            yield return null;
        }
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        pickScript.StartPickup(false);
        
    }


    IEnumerator FielderPickupThrow()
    {
        if (gameObject.name == "keeper")
        {
            Debug.Log("fld done");
            Gameplay.instance.deliveryDead = true;
            yield break;
        }
        yield return new WaitUntil(() => pickScript.isTransitioning == false);
        ball.transform.position = throwingArm.position;
        ball.transform.SetParent(throwingArm);
        Vector3 lookDirection = (fm.keeper.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        transform.rotation = lookRotation;
        yield return new WaitForSeconds(.7f);

        ikControl.PlayAnimation(throwClip);
        yield return new WaitUntil(() => throwable);
        pickScript.interactionSystem.enabled = false;
        yield return new WaitForEndOfFrame();
        Vector3 latePos = ball.position;
        Vector3 lateScale = new Vector3(.3822f,.3822f,.3822f);
        ball.SetParent(null, false);
        ball.position = latePos;
        ball.localScale = lateScale;
        // --- NEW LOGIC

        Vector3 ballPosFlat = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 keeperPosFlat = new Vector3(fm.keeper.position.x, 0, fm.keeper.position.z);
        float distToKeeper = Vector3.Distance(ballPosFlat, keeperPosFlat);

        float pitchRatio = (distToKeeper > 45f) ? .5f : 1f;

        Vector3 dirToKeeper = (fm.keeper.position - ball.position).normalized;
        Vector3 pitchPoint = ballPosFlat + (dirToKeeper * (distToKeeper * 1));

        if (distToKeeper <= 45f) pitchPoint -= dirToKeeper * 3.0f;
        //pitchPoint.y = 0;

        float maxArcHeight = 14f;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float verticalVelocity = Mathf.Sqrt(2 * gravity * maxArcHeight);

        float flightTime = 1.8f * (verticalVelocity / gravity);

        float distanceToPitch = Vector3.Distance(ballPosFlat, pitchPoint);
        float horizontalSpeed = distanceToPitch / flightTime;
        

        Vector3 velocity = dirToKeeper * horizontalSpeed;
        velocity.y = verticalVelocity;
        Debug.DrawLine(ball.position, pitchPoint, Color.red, 15f);
        ballRb.WakeUp();
        ballRb.isKinematic = false;
        ballRb.velocity = velocity;

        fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);

        yield return new WaitForSeconds(0.5f);

        Gameplay.instance.deliveryDead = true;

        StopAllCoroutines();
    }

    #region HelperMethods

    //private bool IsBallComingAtFielder()
    //{
    //    Vector3 ballVelocity = ballRb.velocity;
    //    ballVelocity.y = 0f;

    //    if (ballVelocity.sqrMagnitude < 0.01f)
    //        return false;

    //    Vector3 ballToFielder = transform.position - ball.position;
    //    ballToFielder.y = 0f;

    //    float dot = Vector3.Dot(ballVelocity.normalized, ballToFielder.normalized);

    //    // dot > 0 → ball moving toward fielder
    //    return dot > 0.9f; // ~53 degrees cone
    //}

    private bool IsBallComingAtFielder(float maxLateralDistance = 0.6f)
    {
        Vector3 ballPos = ball.position;
        Vector3 fielderPos = transform.position;

        Vector3 v = ballRb.velocity;
        v.y = 0f;

        if (v.sqrMagnitude < 0.01f)
            return false;

        Vector3 toFielder = fielderPos - ballPos;
        toFielder.y = 0f;

        // Time of closest approach along velocity vector
        float t = Vector3.Dot(toFielder, v) / v.sqrMagnitude;

        // Ball is moving away or already passed
        if (t <= 0f)
            return false;

        // Closest point on ball trajectory
        Vector3 closestPoint = ballPos + v * t;

        // Horizontal miss distance
        float missDistance = Vector3.Distance(
            new Vector3(closestPoint.x, 0, closestPoint.z),
            new Vector3(fielderPos.x, 0, fielderPos.z)
        );

        return missDistance <= maxLateralDistance;
    }


    bool FielderCanReachOnTime(Vector3 position)
    {
        float distance = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(transform.position.x, transform.position.z)) + 0.57f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.drag);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(positionAtReqTime.x, positionAtReqTime.z));
        return fielderDistanceToPredictedPos / runSpeed <= timeReq;
    }

    //Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float ballDrag)
    //{
    //    float timestep = Time.fixedDeltaTime;
    //    Vector3 currentPosition = initialPosition;
    //    Vector3 previousPosition = initialPosition;
    //    Vector3 currentVelocity = velocity;

    //    int maxSteps = 1000;

    //    for (int i = 0; i < maxSteps; i++)
    //    {
    //        previousPosition = currentPosition;

    //        currentVelocity += Physics.gravity * timestep;

    //        currentVelocity /= (1f + ballDrag * timestep);

    //        currentPosition += currentVelocity * timestep;

    //        //if (Physics.Linecast( previousPosition, currentPosition, out RaycastHit hit))
    //        //{
    //        //    if (hit.collider.gameObject.CompareTag("rayTest")) return hit.point;
    //        //}

    //        if (currentPosition.y <= groundY)
    //        {
    //            return currentPosition;
    //        }
    //    }

    //    return currentPosition;
    //}

    public Vector3 PredictBallPosition(Vector3 startPos, Vector3 velocity, float drag)
    {
        Vector3 currentPos = startPos;
        Vector3 currentVel = velocity;

        float timeStep = Time.fixedDeltaTime; // Always use engine value
        Vector3 previousPos = currentPos;

        for (int i = 0; i < 1000; i++)
        {
            previousPos = currentPos;

            // Apply gravity
            currentVel += Physics.gravity * timeStep;

            // Apply drag (Unity Rigidbody formula)
            currentVel *= 1f / (1f + drag * timeStep);

            // Move
            currentPos += currentVel * timeStep;

            // Ground crossing check
            if (currentPos.y <= 0f)
            {
                // Interpolate exact hit point
                float t = previousPos.y / (previousPos.y - currentPos.y);
                Vector3 hitPoint = Vector3.Lerp(previousPos, currentPos, t);
                hitPoint.y = 0f;
                return hitPoint;
            }
        }

        return currentPos;
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
        pickScript.StartDrop();
        throwable = true;
    }
}
