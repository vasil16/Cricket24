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
        pickScript.interactionSystem.enabled = true;
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
        if (IsBallComingAtFielder() && !this.gameObject.CompareTag("DeepFielder") && ballComp.groundShot)
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

    float reachDistance = 1.2f; // The physical length of the arm + small buffer
    float prepareDistance = 4.0f; // Distance to start slowing down/blending animation
    float chaseThreshold = 1.5f; // If ball is this far behind us, switch to chase

    IEnumerator RunToBall(bool restart = false)
    {
        if (restart) Debug.Log("second time");
        ikControl.PlayAnimation(runningClip);

        while (!ballComp.stopTriggered)
        {
            // 1. Calculate Distances
            float distToBall = Vector3.Distance(transform.position, ball.position);

            // 2. Check for "Dead" ball
            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                yield break;
            }

            // 3. IMMEDIATE PICKUP CHECK: Are we physically close enough?
            // We check this every frame regardless of logic to prevent missing a ball we are standing on.
            if (distToBall <= reachDistance)
            {
                Debug.Log("Ball in hand range - Picking up");
                ballComp.fieldedPlayer = this.gameObject;
                StartCoroutine(ReachedBall());
                yield break;
            }

            // 4. Movement Logic
            if (!ballComp.groundShot)
            {
                // Aerial logic (Keep existing logic or simplify)
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f)
                {
                    StartCoroutine(WaitForBall()); // Switch to waiting logic
                    yield break;
                }
            }
            else
            {
                // GROUND SHOT LOGIC
                ComputeTarget(restart); // Ensure this updates targetPosition

                // 5. Chase Mode Check
                // If the ball is moving away from us or we passed it
                if (ShouldChase(ball, transform.position))
                {
                    chaseMode = true;
                    // Adjust collider for diving/chasing if needed
                    transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, -1.47F);
                }

                if (chaseMode)
                {
                    // In chase mode, we run directly at the ball, not the intercept point
                    targetPosition = ball.position;
                }

                // 6. Look & Move
                Vector3 moveDirection = (targetPosition - transform.position).normalized;
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f); // Increased rotation speed
                }

                // Move
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);

                // 7. Transition to Waiting/Preparing
                // Only wait if the ball is incoming and we are close to the intercept line
                if (!chaseMode && distToBall < prepareDistance && IsBallComingAtFielder())
                {
                    // Don't stop completely, move into the WaitForBall for fine-tuning
                    StartCoroutine(WaitForBall());
                    yield break;
                }
            }
            yield return null;
        }
    }

    void StopAll()
    {
        //agent.Stop();
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


            // 1. FAILSAFE: Switch back to Run if ball passes us
            if (ShouldChase(ball, transform.position) || distToBall > prepareDistance + 2f)
            {
                Debug.Log("Ball passed while waiting - switching back to Run/Chase");
                StartCoroutine(RunToBall(true));
                yield break;
            }

            // 2. PICKUP TRIGGER: Fixed distance
            // We use reachDistance (e.g. 1.2f). We do NOT multiply by velocity.
            // If the ball is fast, we need to trigger slightly earlier to account for frame drops? 
            // No, Physics.Overlap or Distance check is usually enough. 
            // If you really need velocity prediction:
            float dynamicReach = reachDistance + (ballRb.velocity.magnitude * Time.deltaTime * 2);

            if (distToBall <= dynamicReach)
            {
                StartCoroutine(ReachedBall(true));
                yield break;
            }

            // 3. Fine Tuning Position while waiting
            // Do not stand still. Rotate to face ball and micro-adjust position.
 
            // If ball is slowing down significantly, creep towards it
            if (ballRb.velocity.magnitude < 5.0f)
            {
                transform.position = Vector3.MoveTowards(transform.position, ball.position, (runSpeed * 0.5f) * Time.deltaTime);
            }
            // If ball is fast, ensure we are standing directly in its path (Lateral movement)
            else
            {
                // Project ball velocity to find closest point on line
                Vector3 closestPointOnLine = ClosestPointOnLine(ball.position, ballRb.velocity, transform.position);
                transform.position = Vector3.MoveTowards(transform.position, closestPointOnLine, (runSpeed * 0.2f) * Time.deltaTime);
            }

            yield return null;
        }
        StartCoroutine(ReachedBall(true));
    }

    // Helper to keep fielder in the ball's path while waiting
    Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
    {
        var vVector1 = vPoint - vA;
        var vVector2 = (vB - vA).normalized;
        var d = Vector3.Distance(vA, vB);
        var t = Vector3.Dot(vVector2, vVector1);
        if (t <= 0) return vA;
        if (t >= d) return vB;
        var vVector3 = vVector2 * t;
        var vClosestPoint = vA + vVector3;
        return vClosestPoint;
    }

    IEnumerator ReachedBall(bool waited = false)
    {
        Debug.Log("111");

        Debug.Log("222");

        //ikControl.PlayAnimation(pickUpClip);

        float pickupDuration = 0.5f;

        if (ballRb.velocity.magnitude < 20f && !chaseMode)
        {
            Debug.Log("slow front");
            pickupDuration = .4f;
        }

        if (waited)
        {
            Debug.Log("waited");
            //ikControl.PlayAnimation(kneelClip);
        }

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
                
        Debug.Log("333");        
    }

    Vector3 latePos;

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
        pickScript.interactionSystem.enabled = false;
        yield return new WaitForEndOfFrame();
        Vector3 latePos = ball.position;
        Vector3 lateScale = new Vector3(.3822f,.3822f,.3822f);
        Debug.Log("early pos " + latePos);
        ball.SetParent(null, false);
        ball.position = latePos;
        ball.localScale = lateScale;
        Debug.Log("later pos " + ball.position);
        // --- NEW LOGIC

        Vector3 ballPosFlat = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 keeperPosFlat = new Vector3(fm.keeper.position.x, 0, fm.keeper.position.z);
        float distToKeeper = Vector3.Distance(ballPosFlat, keeperPosFlat);

        float pitchRatio = (distToKeeper > 25f) ? .6f : 1f;

        Vector3 dirToKeeper = (fm.keeper.position - ball.position).normalized;
        Vector3 pitchPoint = ballPosFlat + (dirToKeeper * (distToKeeper * 1));

        if (distToKeeper <= 25f) pitchPoint -= dirToKeeper * 3.0f;

        Debug.Log("throw call to "+pitchPoint);
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
        pickScript.StartDrop();
        throwable = true;
    }
}
