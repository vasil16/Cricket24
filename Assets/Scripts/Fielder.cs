using System.Collections;
using UnityEngine;
using RootMotion.FinalIK;

public class Fielder : MonoBehaviour
{
    [SerializeField] float runSpeed;
    [SerializeField] float groundY = .001f;
    [SerializeField] float distanceToTarget;
    [SerializeField] float handReachDuration = 0.52362f;
    [SerializeField] float handReachRadius = 0.0038362f;
    
    [SerializeField] bool chaseMode, isRightHanded, isKeeper, throwable;
    public bool startedRun;

    private Vector3 actualPos, actualRot;
    public Vector3 targetPosition;

    [SerializeField] GameObject rayTestObject;
    public Transform ball, throwingArm, fielderTargetMark;

    public Animator animator;
    BallHit ballComp;
    Rigidbody ballRb;

    [SerializeField] FieldManager fm;
    [SerializeField] SmoothInteractionPickup pickScript;

    public AnimationClip idleClip, runningClip, jumpClip, crouchClip, moveRightClip, moveLeftClip, diveRightClip, diveLeftClip,chasePickupClip, pickUpClip, throwClip, kneelClip;

    private void OnEnable()
    {        
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        animator = GetComponent<Animator>();
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
                //if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 60f)
                //    break;
                if(Mathf.Abs(targetPosition.x - transform.position.x) < 1f)
                {
                    animator.Play("idle");
                    //break;
                }
                float direction = (targetPosition.x > transform.position.x) ? -.16f : .16f;
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
                //ikControl.Play(jumpClip);
            }
            else if (targetPosition.y < 3f)
            {
                // ikControl.Play(crouchClip);
            }
        }

        if (!ballComp.secondTouch)
        {
            float addConstant = targetPosition.x > transform.position.x ? -1 : 1;
            fm.marker.position = targetPosition;
        }

        Debug.Log("wait recive");
        yield return new WaitUntil(() => Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 65);
        //yield return new WaitUntil(() => CanStartPickup());

        Debug.Log("recive start");

        pickScript.pickupObject = ball.GetComponent<InteractionObject>();

        pickScript.StartPickup(false);
    }

    public void DropBall(bool keeper)
    {
        //Debug.Log("drop call , keeper? " + keeper);
        StartCoroutine(ReleaseTarget(keeper));
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
            targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, ballRb.linearDamping);
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

    void ComputeTarget(bool restart)
    {        
        if (restart)
        {
            targetPosition = ball.position;
        }
        else if (chaseMode)
        {
            Vector3 ballVelocity = ballRb.linearVelocity;
            Vector3 moveDir = new Vector3(ballVelocity.x, 0f, ballVelocity.z);

            if (moveDir.magnitude < 0.1f)
            {
                targetPosition = ball.position;
            }
            else
            {
                moveDir.Normalize();

                moveDir.Normalize();

                // Check if fielder is already ahead of the ball
                Vector3 toFielder = transform.position - ball.position;
                toFielder.y = 0f;

                bool fielderAhead = Vector3.Dot(toFielder.normalized, moveDir) > 0.3f;

                if (fielderAhead)
                {
                    // Do NOT lead — go directly to ball
                    targetPosition = ball.position;
                }
                else
                {
                    float leadDistance = 1.5f;

                    Vector3 lateralDir = Vector3.Cross(Vector3.up, moveDir).normalized;
                    Vector3 sideOffset = (isRightHanded ? lateralDir : -lateralDir) * 1.2f;

                    targetPosition = ball.position + (moveDir * leadDistance) + sideOffset;
                }
            }

            targetPosition.y = groundY;
        }
    }    

    float GetCollectionStartDistance()
    {
        float ballSpeed = ballRb.linearVelocity.magnitude;
        
        float reqDistance = ballSpeed * handReachDuration;
        return reqDistance;
    }

    IEnumerator RunToBall(bool restart)
    {
        if (restart) Debug.Log("second time");
        animator.Play("running");
        float distToBall = 0;
        while (!ballComp.stopTriggered)
        {
            distToBall = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z));
            distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z));
            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                yield break;
            }

            if (!ballComp.groundShot)
            {
                if (CanStartPickup())
                {
                    Debug.Log("air ball reached taret");
                    StartCoroutine(GrabBall());
                    yield break;
                }
                //if (CanStartPickup(Vector3.Distance(ball.position, transform.position)))
                //{
                //    Debug.Log("air ball reached taret");
                //    StartCoroutine(GrabBall());
                //    yield break;
                //}
            }
            else
            {
                ComputeTarget(restart);

                if (ShouldChase(ball, transform.position) && !chaseMode)
                {
                    Debug.Log(gameObject.name + " has to chase");
                    chaseMode = true;
                }

                if (distanceToTarget <= 2f)
                {
                    if (!chaseMode && IsBallComingAtFielder() && ballRb.linearVelocity.magnitude > 10)
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

                if (chaseMode)
                {
                    if (CanStartPickup())
                    {
                        if(fm.tryingPickup)
                        {
                            while(fm.tryingPickup)
                            {
                                if(ballComp.stopTriggered)
                                {
                                    yield break;
                                }
                                yield return null;
                            }
                        }
                        Debug.Log("Ball in hand range for chase- Picking up " + distToBall);
                        ballComp.fieldedPlayer = this.gameObject;
                        fm.marker.position = ball.position;
                        animator.Play("idle");
                        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                        pickScript.StartPickup(false);
                        fm.tryingPickup = true;
                        yield break;
                    }
                }
                else
                {
                    //if(distToBall<GetCollectionStartDistance())
                    {
                        if (CanStartPickup())
                        {
                            if (fm.tryingPickup)
                            {
                                while (fm.tryingPickup)
                                {
                                    if (ballComp.stopTriggered)
                                    {
                                        yield break;
                                    }
                                    yield return null;
                                }
                            }
                            Debug.Log("Ball in hand range - Picking up " + distToBall);
                            ballComp.fieldedPlayer = this.gameObject;
                            fm.marker.transform.position = ball.position;
                            animator.Play("idle");
                            pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                            pickScript.StartPickup(false);
                            yield break;
                        }

                    }
                    
                }
            }
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + " Enter WaitForBall");
        animator.Play("idle");

        while (!ballComp.fielderReached)
        {
            //float distToBall = Vector3.Distance(transform.position, ball.position);
            float distToBall = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z));

            Vector3 lookDir = (ball.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

            if (CanStartPickup())
            {
                if (fm.tryingPickup)
                {
                    while (fm.tryingPickup)
                    {
                        if (ballComp.stopTriggered)
                        {
                            yield break;
                        }
                        yield return null;
                    }
                }
                fm.marker.position = ball.position;
                pickScript.pickupObject = ball.GetComponent<InteractionObject>();
                pickScript.StartPickup(false);
                yield break;
            }

            if (ballRb.linearVelocity.magnitude < 5.0f)
            {
                Debug.Log("restart runn");
                StartCoroutine(RunToBall(true));
                yield break;
            }
            yield return null;
        }
        animator.Play("idle");
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        if (fm.tryingPickup)
        {
            while (fm.tryingPickup)
            {
                if (ballComp.stopTriggered)
                {
                    yield break;
                }
                yield return null;
            }
        }
        pickScript.StartPickup(false);
    }

    private bool CanStartPickup()
    {
        Vector3 ballPos = ball.position;
        Vector3 fielderPos = transform.position;

        ballPos.y = 0f;
        fielderPos.y = 0f;

        Vector3 ballVel = ballRb.linearVelocity;
        ballVel.y = 0f;
        // Fielder velocity (based on current movement direction)
        Vector3 fielderVel = (targetPosition - transform.position).normalized * runSpeed;
        fielderVel.y = 0f;

        // Relative position and velocity (ball relative to fielder)
        Vector3 relPos = ballPos - fielderPos;
        Vector3 relVel = ballVel - fielderVel;

        float relSpeedSq = relVel.sqrMagnitude;

        if (relSpeedSq < 0.01f)
            return false;

        // Time to closest approach
        float t = -Vector3.Dot(relPos, relVel) / relSpeedSq;

        // If closest approach is in the past, ignore
        if (t < 0f || t > handReachDuration)
            return false;

        // Distance at closest approach
        Vector3 closest = relPos + relVel * t;
        float closestDist = closest.magnitude;

        return closestDist <= handReachRadius; // pickup radius
    }

    public void StopAll()
    {
        animator.Play("idle");
    }

    IEnumerator GrabBall()
    {
        animator.Play("idle");
        while(Vector3.Distance(ball.position,transform.position)<GetCollectionStartDistance())
        {
            yield return null;
        }
        pickScript.pickupObject = ball.GetComponent<InteractionObject>();
        if (fm.tryingPickup)
        {
            while (fm.tryingPickup)
            {
                if (ballComp.stopTriggered)
                {
                    yield break;
                }
                yield return null;
            }
        }
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

        animator.Play("throw");
        yield return new WaitUntil(() => throwable);
        pickScript.interactionSystem.enabled = false;
        yield return new WaitForEndOfFrame();
        Vector3 latePos = ball.position;
        Vector3 lateScale = new Vector3(0.06116f, 0.06116f, 0.06116f);
        ball.SetParent(null, false);
        ball.position = latePos;
        ball.localScale = lateScale;
        // --- NEW LOGIC

        Vector3 ballPosFlat = new Vector3(ball.position.x, 0, ball.position.z);
        Vector3 keeperPosFlat = new Vector3(fm.keeper.position.x, 0, fm.keeper.position.z);
        float distToKeeper = Vector3.Distance(ballPosFlat, keeperPosFlat);

        float pitchRatio = (distToKeeper > 21f) ? .8f : 1f;

        Vector3 dirToKeeper = (fm.keeper.position - ball.position).normalized;
        Vector3 pitchPoint = ballPosFlat + (dirToKeeper * (distToKeeper * 1));

        if (distToKeeper <= 21f) pitchPoint -= dirToKeeper * .80f;
        //pitchPoint.y = 0;

        float maxArcHeight = 2.4f;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float verticalVelocity = Mathf.Sqrt(2 * gravity * maxArcHeight);

        float flightTime = 2.5f * (verticalVelocity / gravity);

        float distanceToPitch = Vector3.Distance(ballPosFlat, pitchPoint);
        float horizontalSpeed = distanceToPitch / flightTime;
        

        Vector3 velocity = dirToKeeper * horizontalSpeed;
        velocity.y = verticalVelocity;
        Debug.DrawLine(ball.position, pitchPoint, Color.red, 15f);
        ballRb.WakeUp();
        ballRb.isKinematic = false;
        ballRb.linearVelocity = velocity;

        fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);

        yield return new WaitForSeconds(0.5f);

        Gameplay.instance.deliveryDead = true;

        StopAllCoroutines();
    }

    #region HelperMethods

    private bool IsBallComingAtFielder(float maxLateralDistance = 0.6f)
    {
        Vector3 ballPos = ball.position;
        Vector3 fielderPos = transform.position;

        Vector3 v = ballRb.linearVelocity;
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
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        // 1. Get the ball's movement direction on the horizontal plane
        Vector2 ballVel2D = new Vector2(ballRb.linearVelocity.x, ballRb.linearVelocity.z);

        // If the ball isn't moving, no need to "chase" it
        if (ballVel2D.sqrMagnitude < 0.1f) return false;

        Vector2 ballDir = ballVel2D.normalized;

        // 2. Vector from the ball TO the fielder
        Vector2 ballToFielder = new Vector2(fielderPosition.x - ball.position.x,
                                            fielderPosition.z - ball.position.z);

        // 3. Dot Product: 
        // If the ball's direction and the vector to the fielder are OPPOSITE, 
        // it means the ball is moving away from the fielder (Chase mode!).
        float dot = Vector2.Dot(ballDir, ballToFielder);

        // If dot < 0, the fielder is "behind" the ball's current travel vector
        return dot < 0;
    }

    #endregion

    public void StopField()
    {
        animator.Play("idle");
        if (isKeeper) KeeperReset();
        else
            Reset();
    }

    public void KeeperReset()
    {
        //agent.Stop();
        throwable = false;
        chaseMode = false;
        transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, 0.02f);
        animator.Play("idle");
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        this.enabled = false;
    }

    public void Reset()
    {
        //agent.Stop();
        throwable = false;
        chaseMode = false;
        transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, 0.02f);
        animator.Play("idle");
        StopAllCoroutines();
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        this.enabled = false;
    }

    public void Throw()
    {        
        Debug.Log("throw");
        pickScript.StartDrop();
        throwable = true;
    }    
}
