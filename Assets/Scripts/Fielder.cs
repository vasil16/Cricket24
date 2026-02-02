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
    public Vector3 actualFetchPosition;
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

        yield return new WaitUntil(()=>ShouldStartCollection(ball, out neededSpeed));

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

    [SerializeField] float handReachDuration = 0.5109f; // tune once
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
        targetPosition.y = groundY;

        if(ballRb.velocity.magnitude<10)
        {
            targetPosition = new Vector3(ball.position.x, groundY, ball.position.z);
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
                targetPosition = new Vector3(interceptPoint.x, groundY, interceptPoint.z);
            }
            else
            {
                targetPosition = new Vector3(ball.transform.position.x, groundY, ball.transform.position.z);
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
            targetPosition.y = groundY;
        }

        fielderTargetMark.position = targetPosition;
        return targetPosition;
    }

    public bool isRightHanded, targetMode;


    float GetCollectionStartDistance()
    {
        float ballSpeed = ballRb.velocity.magnitude; // m/s
        float collectionTime = .50253f; // seconds
        float reqDistance = ballSpeed * collectionTime;
        return Mathf.Clamp(reqDistance, 1, 12);
    }

    // Returns TRUE when it's the exact frame to start the pickup.
    // 'speedMultiplier' tells you how fast to play the animation (1x, 2x, etc) to ensure the catch.
    public bool ShouldStartCollection(Transform ball, out float speedMultiplier)
    {
        speedMultiplier = 1.0f;
        float standardAnimTime = 0.50253f; // Your animation length
        float maxPickupDistance = 3.0f;    // NEVER start pickup further than this (visual limit)

        // 1. Calculate Relative Velocity (Crucial for chasing vs head-on)
        // If we are running towards ball, the gap closes faster.
        Vector3 myVelocity = Vector3.zero;
        // Assuming you have a Rigidbody or know your speed. If not, use zero (acceptable for basic logic)
        // if (GetComponent<Rigidbody>()) myVelocity = GetComponent<Rigidbody>().velocity; 

        Vector3 relativeVelocity = ball.GetComponent<Rigidbody>().velocity - myVelocity;
        Vector3 toFielder = transform.position - ball.position;

        // 2. Calculate Closing Speed (How fast is the ball coming at me?)
        float closingSpeed = Vector3.Dot(relativeVelocity, toFielder.normalized);

        // If ball is moving away or stopped, fallback to simple distance check
        if (closingSpeed <= 0.1f) return toFielder.magnitude <= 1.5f;

        // 3. Time To Impact (The most important number)
        float timeToImpact = toFielder.magnitude / closingSpeed;

        // 4. THE LOGIC
        // We start IF:
        // (We have exactly enough time for the normal animation)
        // AND
        // (The ball is physically close enough to look natural)

        // Check 1: Are we running out of time?
        bool timeIsCritical = timeToImpact <= standardAnimTime;

        // Check 2: Are we within the visual limit?
        bool withinVisualRange = toFielder.magnitude <= maxPickupDistance;

        // 5. The "Fast Ball" Compensation
        // If the ball is super fast, 'timeIsCritical' will be true at 15m away.
        // But 'withinVisualRange' is false. So we WAIT.
        // We keep waiting until the ball hits 'maxPickupDistance' (3m).
        // At that point, 'timeToImpact' will be very short (e.g., 0.1s).
        // So we must speed up the animation to match the remaining time.

        if (withinVisualRange && timeIsCritical)
        {
            // Calculate needed speed. 
            // Example: If we have 0.1s left, but anim is 0.5s, we need 5x speed.
            speedMultiplier = standardAnimTime / Mathf.Max(timeToImpact, 0.01f);

            // Clamp it so it doesn't go crazy (e.g., max 3x speed)
            speedMultiplier = Mathf.Clamp(speedMultiplier, 1.0f, 3.0f);

            return true;
        }

        return false;
    }

    float neededSpeed;


    IEnumerator RunToBall(bool restart = false)
    {
        if (restart) Debug.Log("second time");
        ikControl.PlayAnimation(runningClip);
        float distToBall = 0;
        while (!ballComp.stopTriggered)
        {
            // 1. Calculate Distances
            distToBall = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z));
            distanceToTarget= Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z));
            // 2. Check for "Dead" ball
            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                yield break;
            }

            // 3. IMMEDIATE PICKUP CHECK: Are we physically close enough?
            // We check this every frame regardless of logic to prevent missing a ball we are standing on.

            // 4. Movement Logic
            if (!ballComp.groundShot)
            {
                // Aerial logic (Keep existing logic or simplify)
                if (distanceToTarget<1f)
                {
                    Debug.Log("air ball reached taret");
                    StartCoroutine(GrabBall()); // Switch to waiting logic
                    yield break;
                }
            }
            else
            {
                // GROUND SHOT LOGIC
                ComputeTarget(restart); // Ensure this updates targetPosition


                if(ShouldStartCollection(ball, out neededSpeed))
                {
                    Debug.Log("Ball in hand range - Picking up " + distToBall);
                    ballComp.fieldedPlayer = this.gameObject;
                    //StartCoroutine(ReachedBall());
                    //ikControl.PlayAnimation(idleClip);
                    ikControl.PlayAnimation(idleClip);
                    pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                    pickScript.StartPickup(false);
                    yield break;
                }

                //if (distToBall <= GetCollectionStartDistance())
                //{
                //    Debug.Log("Ball in hand range - Picking up "+distToBall);
                //    ballComp.fieldedPlayer = this.gameObject;
                //    //StartCoroutine(ReachedBall());
                //    //ikControl.PlayAnimation(idleClip);
                //    ikControl.PlayAnimation(idleClip);
                //    pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                //    pickScript.StartPickup(false);
                //    yield break;
                //}

                // 5. Chase Mode Check
                // If the ball is moving away from us or we passed it
                if (ShouldChase(ball, transform.position))
                {
                    chaseMode = true;
                }

                if (chaseMode||targetMode)
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

                if (!chaseMode && IsBallComingAtFielder() && ballRb.velocity.magnitude > 10)
                {
                    StartCoroutine(WaitForBall());
                    yield break;
                }

                if(distanceToTarget<=2f)
                {
                    targetMode = true;
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

            if (distToBall <= GetCollectionStartDistance())
            {
                //StartCoroutine(ReachedBall(true));
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

    [SerializeField] float refDistance, refSpeed;

    IEnumerator FielderPickupThrow()
    {
        if (gameObject.name == "keeper")
        {
            Debug.Log("fld done");
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

        fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);

        //Vector3 keeperRight = fm.keeper.right;

        //while (Vector2.Distance(new Vector2(fm.stumps.position.x, fm.stumps.position.z), new Vector2(fm.keeper.position.x, fm.keeper.position.z)) > 2f)
        //{
        //    Vector3 toBall = ball.position - fm.keeper.position;
        //    float lateralOffset = Vector3.Dot(toBall, keeperRight);
        //    //if (lateralOffset > 0.2f)
        //    //{
        //    //    fm.keeper.GetComponent<FielderIK>().PlayAnimation(moveRightClip);
        //    //}
        //    //else if (lateralOffset < -0.2f)
        //    //{
        //    //    fm.keeper.GetComponent<FielderIK>().PlayAnimation(moveLeftClip);
        //    //}
        //    Vector3 sidewaysMove = keeperRight * lateralOffset;

        //    Vector3 newPos = Vector3.MoveTowards(fm.keeper.position, fm.stumps.position + sidewaysMove, Time.deltaTime * 118);
        //    newPos.z = fm.keeper.position.z;
        //    fm.keeper.position = newPos;
            
        //    yield return null;
        //}
        
        //Debug.Log("fld done");

        yield return new WaitForSeconds(0.5f);

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
        float timeStep = 0.02f; // Matches FixedUpdate default

        // Simulate until the ball hits the ground (y <= 0)
        // Limit to 500 iterations to prevent infinite loops if the ball never falls
        for (int i = 0; i < 500; i++)
        {
            // 1. Apply Gravity
            currentVel += Physics.gravity * timeStep;

            // 2. Apply Drag (Unity's formula: vel = vel * (1 - timeStep * drag))
            currentVel *= (1f - timeStep * drag);

            // 3. Move position
            currentPos += currentVel * timeStep;

            // 4. Check if we hit the ground level
            if (currentPos.y <= 0)
            {
                // Set y to 0 for a clean marker placement
                currentPos.y = 0;
                return currentPos;
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
