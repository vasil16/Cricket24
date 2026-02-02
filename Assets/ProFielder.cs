//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using RootMotion.FinalIK;

//public class ProFielder : MonoBehaviour
//{
//    [Header("Attributes")]
//    public FielderType fielderType = FielderType.Outfield;
//    public float maxRunSpeed = 7.5f;
//    public float throwPower = 20f;
//    public bool isRightHanded = true;

//    [Header("State")]
//    [SerializeField] private FielderState currentState;
//    private Vector3 startPosition;
//    private Quaternion startRotation;

//    [Header("References")]
//    public Transform rightHand;
//    public Transform leftHand;
//    public Transform throwingArm;
//    public FielderIK ikControl;
//    public SmoothInteractionPickup pickScript;
//    public FieldManager fm; // Reference to access Keeper/Stumps

//    // Ball Tracking
//    private Transform currentBall;
//    private Rigidbody ballRb;
//    private BallHit ballHitData;

//    // Movement & Prediction
//    private Vector3 targetInterceptPoint;
//    private bool isAerialCatchPossible;
//    private float timeToBallLanding;
//    private Coroutine fieldingRoutine;

//    // Animation Clips (Keep your references)
//    public AnimationClip idleClip, runningClip, diveClip, throwClip, pickUpClip, catchClipHigh, catchClipChest;

//    public enum FielderType { Outfield, Infield, Keeper, Bowler }
//    public enum FielderState { Idle, Assessing, Running, Positioning, Fielding, Throwing, Resetting }

//    private void Start()
//    {
//        startPosition = transform.position;
//        startRotation = transform.rotation;
//        ikControl = GetComponent<FielderIK>();
//        pickScript = GetComponent<SmoothInteractionPickup>();
//        currentState = FielderState.Idle;
//    }

//    // ------------------------------------------------------------------------
//    // PHASE 1: THE EYES & BRAIN (Reaction)
//    // ------------------------------------------------------------------------

//    /// <summary>
//    /// Called immediately when the bat hits the ball.
//    /// </summary>
//    public void OnShotPlayed(Transform ballObj)
//    {
//        if (fieldingRoutine != null) StopCoroutine(fieldingRoutine);

//        currentBall = ballObj;
//        ballRb = ballObj.GetComponent<Rigidbody>();
//        ballHitData = ballObj.GetComponent<BallHit>();

//        // Enable interaction system
//        if (pickScript) pickScript.interactionSystem.enabled = true;

//        // If I am the keeper, use specific logic
//        if (fielderType == FielderType.Keeper)
//        {
//            fieldingRoutine = StartCoroutine(KeeperRoutine());
//            return;
//        }

//        // General Fielder Logic
//        fieldingRoutine = StartCoroutine(FielderBrainRoutine());
//    }

//    IEnumerator FielderBrainRoutine()
//    {
//        currentState = FielderState.Assessing;

//        // Small human reaction delay (0.1s - 0.3s)
//        yield return new WaitForSeconds(Random.Range(0.1f, 0.25f));

//        // 1. Analyze Trajectory
//        PredictBallPath(out Vector3 landingPoint, out float flightTime);

//        // 2. Can I catch it?
//        float distToLanding = Vector3.Distance(transform.position, landingPoint);
//        float timeToRunThere = distToLanding / maxRunSpeed;

//        // Note: ballHitData.groundShot checks if it started on ground, 
//        // but we also check if flightTime is enough to catch it.
//        if (!ballHitData.groundShot && timeToRunThere < flightTime)
//        {
//            // I CAN CATCH IT
//            isAerialCatchPossible = true;
//            targetInterceptPoint = landingPoint;
//            Debug.Log($"{name}: Aerial Catch Detected! Moving to {targetInterceptPoint}");
//            currentState = FielderState.Running;
//        }
//        else
//        {
//            // IT IS A GROUND FIELD / CHASE
//            isAerialCatchPossible = false;
//            currentState = FielderState.Running;
//            // Initial calculation
//            targetInterceptPoint = CalculateGroundIntercept(ballRb.velocity);
//        }

//        // 3. Execution Loop
//        while (currentState == FielderState.Running || currentState == FielderState.Positioning)
//        {
//            MoveFielder();

//            // Re-evaluate ground intercept constantly as ball drag slows it down
//            if (!isAerialCatchPossible)
//            {
//                targetInterceptPoint = CalculateGroundIntercept(ballRb.velocity);
//            }

//            // Check proximity to ball
//            float distToBall = Vector3.Distance(transform.position, currentBall.position);

//            // If ball is very close, switch to Fielding
//            if (distToBall < 1.5f || (isAerialCatchPossible && distToBall < 2.0f && currentBall.position.y > 1f))
//            {
//                currentState = FielderState.Fielding;
//            }

//            yield return null;
//        }

//        // 4. Perform the Action
//        yield return StartCoroutine(PerformFieldingAction());
//    }

//    // ------------------------------------------------------------------------
//    // PHASE 2: MOVEMENT MATH (Navigation)
//    // ------------------------------------------------------------------------

//    private void MoveFielder()
//    {
//        // Direction to target
//        Vector3 dir = (targetInterceptPoint - transform.position).normalized;
//        dir.y = 0; // Stay on ground

//        float currentDist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
//                                             new Vector3(targetInterceptPoint.x, 0, targetInterceptPoint.z));

//        // Rotate smooth
//        if (dir != Vector3.zero)
//        {
//            Quaternion lookRot = Quaternion.LookRotation(dir);
//            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 15f);
//        }

//        // Run
//        ikControl.PlayAnimation(runningClip);

//        // Slow down slightly as we approach to ensure clean pickup
//        float speed = maxRunSpeed;
//        if (currentDist < 3.0f && !isAerialCatchPossible) speed *= 0.6f;

//        transform.position += transform.forward * speed * Time.deltaTime;
//        transform.position = new Vector3(transform.position.x, 0, transform.position.z); // Clamp ground
//    }

//    private Vector3 CalculateGroundIntercept(Vector3 ballVelocity)
//    {
//        // Simple interception math:
//        // Find a point along the ball's velocity vector where the fielder can meet it.

//        Vector3 ballPosFlat = new Vector3(currentBall.position.x, 0, currentBall.position.z);
//        Vector3 fielderPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
//        Vector3 ballVelFlat = new Vector3(ballVelocity.x, 0, ballVelocity.z);

//        // If ball is stopped or extremely slow, run to ball
//        if (ballVelFlat.magnitude < 1f) return ballPosFlat;

//        // Vector from ball to fielder
//        Vector3 toFielder = fielderPosFlat - ballPosFlat;

//        // Is the ball coming towards me? (Dot product)
//        float dot = Vector3.Dot(ballVelFlat.normalized, toFielder.normalized);

//        if (dot > 0.8f) // Ball coming straight at me
//        {
//            // Just get in line with it
//            return ballPosFlat + (ballVelFlat.normalized * (toFielder.magnitude * 0.5f));
//        }
//        else if (dot < 0) // Ball going away (Chase)
//        {
//            // Run directly at the ball (greedy approach) or slightly ahead
//            return ballPosFlat + (ballVelFlat.normalized * 2.0f); // Lead the target
//        }
//        else // Cut off angle
//        {
//            // Math to find intersection of two rays is complex, approximation is better for games:
//            // Predict where ball is in t seconds
//            float t = 1.5f; // Look ahead time
//            return ballPosFlat + (ballVelFlat * t);
//        }
//    }

//    private void PredictBallPath(out Vector3 landingPos, out float time)
//    {
//        // Basic physics prediction: y = v0y * t + 0.5 * g * t^2
//        // We want to find t when y = 0

//        Vector3 v0 = ballRb.velocity;
//        float y0 = currentBall.position.y;
//        float g = Physics.gravity.y;

//        // Quadratic formula for time to hit ground: 0 = 0.5gt^2 + v0y*t + y0
//        float b = v0.y;
//        float a = 0.5f * g;
//        float c = y0;

//        float discriminant = (b * b) - (4 * a * c);

//        if (discriminant < 0)
//        {
//            // Should not happen unless gravity is reversed or already below ground
//            landingPos = currentBall.position;
//            time = 0;
//            return;
//        }

//        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
//        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

//        // We want the positive time
//        time = Mathf.Max(t1, t2);

//        // Calculate X and Z displacement ignoring drag for prediction speed (or add drag factor if needed)
//        Vector3 displacement = new Vector3(v0.x, 0, v0.z) * time;
//        landingPos = currentBall.position + displacement;
//        landingPos.y = 0;
//    }

//    // ------------------------------------------------------------------------
//    // PHASE 3: EXECUTION (Fielding/Throwing)
//    // ------------------------------------------------------------------------

//    IEnumerator PerformFieldingAction()
//    {
//        // 1. Pickup / Catch Logic
//        pickScript.pickupObject = currentBall.GetComponent<InteractionObject>();

//        if (isAerialCatchPossible)
//        {
//            // Trigger Catch Animation based on height relative to head
//            ikControl.PlayAnimation(catchClipChest); // Simplified for example
//            pickScript.StartPickup(false); // Snap to hand
//            Debug.Log("Caught!");
//            ballHitData.fielderReached = true; // Tell game logic ball is dead
//        }
//        else
//        {
//            // Ground fielding
//            ikControl.PlayAnimation(pickUpClip);

//            // Procedural Hand IK to floor
//            float t = 0;
//            while (t < 0.3f)
//            {
//                t += Time.deltaTime;
//                Vector3 reachPoint = currentBall.position;
//                if (isRightHanded) rightHand.position = Vector3.Lerp(rightHand.position, reachPoint, t * 5);
//                else leftHand.position = Vector3.Lerp(leftHand.position, reachPoint, t * 5);
//                yield return null;
//            }

//            pickScript.StartPickup(true); // Pickup logic
//            ballHitData.fielderReached = true;
//        }

//        // Wait for animation to finish pickup
//        yield return new WaitForSeconds(0.5f);

//        // 2. The Throw
//        currentState = FielderState.Throwing;
//        yield return StartCoroutine(ThrowBall());
//    }

//    IEnumerator ThrowBall()
//    {
//        // Determine Target (Keeper or Bowler?)
//        Transform targetStumps = fm.keeper; // Default to keeper

//        // Aim
//        Vector3 dirToTarget = (targetStumps.position - transform.position).normalized;
//        dirToTarget.y = 0;
//        transform.rotation = Quaternion.LookRotation(dirToTarget);

//        // Parent ball to hand for windup
//        currentBall.SetParent(throwingArm);
//        currentBall.localPosition = Vector3.zero;

//        // Play Animation
//        ikControl.PlayAnimation(throwClip);

//        // Wait for release point in animation (Approximate or use Animation Event)
//        yield return new WaitForSeconds(0.4f);

//        // Detach
//        pickScript.StartDrop(); // Helper to clean up IK interaction
//        currentBall.SetParent(null);
//        ballRb.isKinematic = false;

//        // CALCULATE PHYSICS THROW
//        // We want the ball to land near the stumps.
//        Vector3 origin = currentBall.position;
//        Vector3 target = targetStumps.position + (Vector3.up * 0.5f); // Aim at waist height

//        // Calculate velocity required to hit target
//        Vector3 throwVelocity = CalculateBallisticVelocity(origin, target, 45f); // 45 degree arc for max distance, or flatter

//        // Flatten the arc if we are close (Bullet throw)
//        if (Vector3.Distance(origin, target) < 20f)
//        {
//            throwVelocity = (target - origin).normalized * 25f; // Hard flat throw
//        }

//        ballRb.velocity = throwVelocity;

//        Debug.Log("Thrown to keeper.");

//        // Wait then reset
//        yield return new WaitForSeconds(1.0f);
//        ResetFielder();
//    }

//    // Physics solver for projectile motion
//    Vector3 CalculateBallisticVelocity(Vector3 source, Vector3 target, float angle)
//    {
//        Vector3 dir = target - source;
//        float h = dir.y;
//        dir.y = 0;
//        float dist = dir.magnitude;
//        float a = angle * Mathf.Deg2Rad;
//        dir.y = dist * Mathf.Tan(a);
//        float distTotal = dist / Mathf.Cos(a);

//        // Calculate required speed
//        float gravity = Mathf.Abs(Physics.gravity.y);
//        float vel = Mathf.Sqrt(dist * gravity / Mathf.Sin(2 * a));

//        return vel * dir.normalized;
//    }

//    // ------------------------------------------------------------------------
//    // KEEPER LOGIC (Specialized)
//    // ------------------------------------------------------------------------

//    IEnumerator KeeperRoutine()
//    {
//        // Keeper only cares about lateral movement usually, unless ball is far
//        // Assuming keeper is standing back

//        currentState = FielderState.Positioning;

//        while (!ballHitData.fielderReached && !ballHitData.stopTriggered)
//        {
//            Vector3 ballPos = currentBall.position;

//            // Calculate lateral offset
//            Vector3 relativePos = transform.InverseTransformPoint(ballPos);

//            // Side step logic
//            if (relativePos.x > 0.5f)
//            {
//                // Step Right
//                //ikControl.PlayAnimation(ikControl.moveRightClip); // Assuming access to clips in IK script or local
//                transform.position += transform.right * 2f * Time.deltaTime;
//            }
//            else if (relativePos.x < -0.5f)
//            {
//                // Step Left
//                //ikControl.PlayAnimation(ikControl.moveLeftClip);
//                transform.position -= transform.right * 2f * Time.deltaTime;
//            }

//            // Catch trigger
//            if (Vector3.Distance(transform.position, ballPos) < 1.5f)
//            {
//                pickScript.pickupObject = currentBall.GetComponent<InteractionObject>();
//                pickScript.StartPickup(false);
//                ballHitData.fielderReached = true;
//                ikControl.PlayAnimation(catchClipChest);
//                break;
//            }
//            yield return null;
//        }

//        yield return new WaitForSeconds(1f);
//        ResetFielder();
//    }

//    // ------------------------------------------------------------------------
//    // UTILITY & RESET
//    // ------------------------------------------------------------------------

//    public void ResetFielder()
//    {
//        StopAllCoroutines();
//        currentState = FielderState.Resetting;

//        // Return to start
//        StartCoroutine(ReturnToPos());
//    }

//    IEnumerator ReturnToPos()
//    {
//        ikControl.PlayAnimation(idleClip);
//        pickScript.interactionSystem.enabled = false;

//        // Walk back logic (Optional, or just snap for game flow)
//        while (Vector3.Distance(transform.position, startPosition) > 0.5f)
//        {
//            transform.position = Vector3.MoveTowards(transform.position, startPosition, 3f * Time.deltaTime);
//            transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, Time.deltaTime * 5f);
//            ikControl.PlayAnimation(runningClip);
//            yield return null;
//        }

//        transform.position = startPosition;
//        transform.rotation = startRotation;
//        ikControl.PlayAnimation(idleClip);
//        currentState = FielderState.Idle;
//    }
//}