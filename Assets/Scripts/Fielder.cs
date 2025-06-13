using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.AI;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score, angleDiff, timeToReachLanding;
    private Vector3 actualPos, actualRot, idleRightHand, idleLeftHand;
    public Vector3 targetPosition;
    BallHit ballComp;
    Rigidbody ballRb;
    public Transform ball, throwingArm, rightHand, leftHand;
    public bool canReachInTime, startedRun;
    [SerializeField] FieldManager fm;
    [SerializeField] AnimationClip runClip;
    [SerializeField] MultiAimConstraint headAim, neckAim;
    public FielderIK ikControl;
    List<Effector> effectors;
    [SerializeField] GameObject rayTestObject;
    [SerializeField] NavMeshAgent agent;
    Vector3 initialTarget;
    bool chaseMode, attackMode;

    public Animator animator;

    private void OnEnable()
    {        
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        idleRightHand = rightHand.localPosition;
        idleLeftHand = leftHand.localPosition;
        animator = GetComponent<Animator>();
        ikControl = GetComponent<FielderIK>();        
    }

    private void OnDisable()
    {
        rayTestObject.SetActive(true);
    }

    public AnimationClip idleClip, runningClip, jumpClip, crouchClip, moveRightClip, moveLeftClip, diveRightClip, diveLeftClip,chasePickupClip, pickUpClip, throwClip, kneelClip;

    #region keeper
    public void KeeperRecieve(Vector3 targetPosition, Transform ball)
    {
        //animator.enabled = true;
        ballComp = ball.GetComponent<BallHit>();
        this.ball = ball;
        if (targetPosition == Vector3.zero)
        {
            targetPosition = ball.position;
        }

        if (targetPosition.z > -2.13f)
        {
            //ikControl.PlayAnimation(moveLeftClip);
        }
        if (targetPosition.z < -6.44f)
        {
            //ikControl.PlayAnimation(moveRightClip);
        }
        if (targetPosition.y > 6.5f)
        {
            //ikControl.PlayAnimation(jumpClip);
        }
        else if (targetPosition.y < 0.27f)
        {
            //ikControl.PlayAnimation(crouchClip);
        }

        if (!ballComp.secondTouch)
        {
            StartCoroutine(SetTarget());
            rightHand.position = leftHand.position = targetPosition;
            StartCoroutine(ReleaseTarget(targetPosition.z));
        }
    }

    IEnumerator SetTarget()
    {
        //Debug.Log("recive start");
        float time = 0;
        float duration = .3f;
        float lerpValue = 0;
        while (time <= duration)
        {
            if (ballComp.secondTouch) yield break;
            time += Time.deltaTime;
            lerpValue = Mathf.Lerp(0, 1, time / duration);
            ikControl.SetIKWeight(lerpValue);
            yield return null;
        }
    }

    IEnumerator ReleaseTarget(float z)
    {
        float timer = 0;
        while (!ballComp.stopTriggered && timer < 3f && !Gameplay.instance.deliveryDead && !ballComp.keeperExit)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        float time = 0;
        float duration = 0.5f;
        float lerpValue = 1;
        while (time <= duration)
        {
            time += Time.deltaTime;
            lerpValue = Mathf.Lerp(1, 0, time / duration);
            ikControl.SetIKWeight(lerpValue);
            yield return null;
        }
        Gameplay.instance.deliveryDead = true;
        //Debug.Log("recive done");
    }
    #endregion

    public void Initiate(Vector3 position, Transform ball)
    {

        targetPosition.y = transform.position.y;
        initialTarget = targetPosition;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;

        if (!ballComp.groundShot)
        {
            targetPosition = PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, 5f);
            fm.marker.position = targetPosition;
            if(!FielderCanReachOnTime(PredictBallPosition(ballComp.shotPoint, ballComp.shotForce, 5f)))
            {
                targetPosition = initialTarget;
            }
        }
        if (!Gameplay.instance.stadiumBounds.Contains(targetPosition))
        {
            targetPosition = Gameplay.instance.stadiumBounds.ClosestPoint(targetPosition);
            targetPosition.y = transform.position.y;
        }
        StartCoroutine(StartField());
    }

    IEnumerator StartField()
    {
        yield return new WaitForSeconds(0.1f);
        if(ballRb.velocity.magnitude<10)
        {
            targetPosition = new Vector3(ball.position.x, transform.position.y, ball.position.z);
        }
        if (IsBallComingAtFielder() && ballComp.groundShot)
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

    IEnumerator RunToBall()
    {
        bool targetBall = false;
        ikControl.PlayAnimation(runningClip);
        while (!ballComp.stopTriggered)
        {
            if(targetBall)
            {
                if(chaseMode)
                {
                    Vector3 directionVector = new Vector3(ball.transform.position.x - transform.position.x,0, ball.transform.position.z - transform.position.z);
                    Vector3 normalVector = directionVector.normalized;
                    targetPosition = new Vector3(ball.transform.position.x + normalVector.x*8f, transform.position.y, ball.transform.position.z + normalVector.z *8f);
                }
                else
                {
                    targetPosition = new Vector3(ball.transform.position.x, transform.position.y, ball.transform.position.z);
                }
            }

            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            if (moveDirection.sqrMagnitude > 0.01f) // prevent NaNs when target is too close
            {
                Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 3f);
            }

            Debug.Log("runningtoball");
            if (ballRb.velocity.magnitude<25 && !chaseMode && !targetBall)
            {
                Debug.Log("ball slowed");
                transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, -.37f);
                targetBall = true;
            }
            if (ballComp.keeperReceive)
            {
                StopAll();
                Gameplay.instance.deliveryDead = true;
                break;
            }
            if(ShouldChase(ball.position,ballRb.velocity,transform.position)&&ballComp.groundShot)
            {
                if(targetBall)
                {

                }
                transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, -1.2f);
                Debug.Log("chasee");
                chaseMode = true;
                targetBall = true;
            }

            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f && !ballComp.fielderReached && !targetBall)
            {
                Debug.Log(gameObject.name + " reached target go for ball");
                targetBall = true;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
            //agent.SetDestination(targetPosition);

            if (Gameplay.instance.deliveryDead)
            {
                StopAll();
                yield break;
            }

            if (!ballComp.groundShot)
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f)
                {
                    if(ballComp.groundShot)
                    {
                        targetPosition = initialTarget;
                        yield break;
                    }
                    if(ballComp.fielderReached)
                    {
                        StartCoroutine(ReachedBall());
                        yield break;
                    }
                    Debug.Log(gameObject.name + " reached target go for");
                    targetBall = true;
                }
            }
                
            if(ballComp.fielderReached && ballComp.fieldedPlayer == this.gameObject)
            {
                ikControl.PlayAnimation(idleClip);
                Debug.Log(gameObject.name + " reached ball");
                StartCoroutine(ReachedBall());
                yield break;
            }
            yield return null;
        }        
        Debug.Log("other fiedler completed");
        StopAll();
        yield break;
        
    }

    void StopAll()
    {
        //agent.Stop();
        ikControl.PlayAnimation(idleClip);
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + "  waiting for ball");
        while (!ballComp.fielderReached)
        {
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
            if (ballRb.velocity.magnitude < 5)
            {
                Debug.Log("slowed beyound thrshold");
                //transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
                agent.SetDestination(new Vector3(ball.position.x, transform.position.y, ball.position.z));                
            }
            yield return null;
        }
        StartCoroutine(ReachedBall());
    }

    IEnumerator ReachedBall()
    {
        if (ballComp.fieldedPlayer == this.gameObject)
        {
            Vector3 targetPosition = ball.position;

            // Get the character's transform (assuming 'transform' is the character)
            Vector3 localBallOffset = transform.InverseTransformPoint(targetPosition);

            // Clamp forward reach (X and Z in world, but Z in local space usually means forward)
            float maxReachForward = 0.5f; // Adjust as needed
            localBallOffset.z = Mathf.Clamp(localBallOffset.z, -maxReachForward, maxReachForward);

            // Optionally limit side reach (X axis)
            float maxReachSide = 0.3f;
            localBallOffset.x = Mathf.Clamp(localBallOffset.x, -maxReachSide, maxReachSide);

            // Recalculate the final position in world space
            Vector3 adjustedPosition = transform.TransformPoint(localBallOffset);
            if(ballRb.velocity.magnitude<0.2f)
            {
                adjustedPosition = ball.position;
            }

            // Move hands to adjusted position
            //rightHand.position = adjustedPosition;
            //leftHand.position = adjustedPosition;

            //transform.position = new Vector3(transform.position.x, actualPos.y, transform.position.z);
            
            if (ballRb.velocity.magnitude < 20f && !chaseMode)
            {
                ikControl.PlayAnimation(kneelClip);
            }
            else if(chaseMode)
            {
                ikControl.PlayAnimation(chasePickupClip);
            }
            else
            {
                ikControl.PlayAnimation(pickUpClip);    
            }
            ikControl.SetIKWeight(1);
            float timer = 0;
            float duration = 0.3f;
            float lerpValue = 0;
            while (timer <= duration)
            {
                timer += Time.deltaTime;
                lerpValue = Mathf.Lerp(0, 1, timer / duration);
                rightHand.position = leftHand.position = Vector3.Lerp(rightHand.position, adjustedPosition, timer/duration);
                //ikControl.SetIKWeight(lerpValue);
                yield return null;
            }

            

            timer = 0;
            duration = 0.3f;
            lerpValue = 1;
            while (timer <= duration)
            {
                timer += Time.deltaTime;
                lerpValue = Mathf.Lerp(1, 0, timer / duration);
                ikControl.SetIKWeight(lerpValue);
                yield return null;
            }

            if (!ballComp.stopTriggered)
            {               
                ballComp.fielderReached = false;
                StartCoroutine(RunToBall());
                yield break;
            }
            else
            {
                if (!ballComp.groundShot)
                {
                    ikControl.PlayAnimation(idleClip);
                    ballRb.isKinematic = true;
                    Gameplay.instance.deliveryDead = true;
                    Debug.Log("caught");
                    Gameplay.instance.Out();
                    yield break;
                }
                Debug.Log("commp");
                ball.transform.position = throwingArm.position;
                ball.transform.SetParent(throwingArm);
                #region dive/pick action
                //Vector3 toBall = ball.position - transform.position;
                //float distance = toBall.magnitude;
                //Vector3 toBallNormalized = toBall.normalized;

                //float side = Vector3.Dot(transform.right, toBallNormalized);     // + right, - left
                //float forward = Vector3.Dot(transform.forward, toBallNormalized); // + in front, - behind

                //// Set some tuning thresholds
                //float sideThresholdToDive = 0.5f;
                //float diveDistanceThreshold = 2.5f;
                //float frontThreshold = 0.6f;

                //if (forward > frontThreshold)
                //{
                //    if (Mathf.Abs(side) > sideThresholdToDive && distance > diveDistanceThreshold)
                //    {
                //        // Ball is far to the side → dive
                //        if (side > 0)
                //            animator.SetTrigger("DiveRight");
                //        else
                //            animator.SetTrigger("DiveLeft");
                //    }
                //    else
                //    {
                //        // Ball is close or centered → pick from front
                //        if (ball.position.y > 6f)
                //        {
                //            animator.Play("jump");
                //        }
                //        else
                //        {
                //            animator.SetTrigger("Pick");
                //        }
                //    }
                //}
                //else
                //{
                //    // Ball is on side or behind, close enough to pick
                //    if (side > 0)
                //        animator.SetTrigger("Pick");
                //    else
                //        animator.SetTrigger("Pick");
                //}
                #endregion
                StartCoroutine(FielderPickupThrow());
            }            
        }              
    }

    IEnumerator FielderPickupThrow()
    {        
        ikControl.PlayAnimation(idleClip);
        if (this.name == "keeper")
        {
            Debug.Log("fld done");
            KeeperRecieve(ball.position, ball);
            //Gameplay.instance.deliveryDead = true;
            yield break;
        }

        if (Vector2.Distance(new Vector2(transform.position.x,transform.position.z), new Vector2(fm.keeper.position.x, fm.keeper.position.z))<=80)
        {
            Debug.Log("fld done");
            Gameplay.instance.deliveryDead = true;
            yield break;
        }
        ikControl.PlayAnimation(throwClip);

        Vector3 lookDirection = (fm.stumps.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        transform.rotation = lookRotation;

        float time = 0;
        float duration = 1f;
        float lerpValue = 1;

        //while (time <= duration)
        //{
        //    time += Time.deltaTime;
        //    lerpValue = Mathf.Lerp(1, 0, time / duration);
        //    ikControl.SetIKWeight(lerpValue);
        //    yield return null;
        //}

        yield return new WaitForSeconds(1f);
        Vector3 direction = (fm.keeper.position - ball.position).normalized;
        float distance = Vector3.Distance(ball.position, fm.keeper.position);

        Debug.DrawRay(ball.position, direction, Color.green, 10f);

        float baseSpeed = 6f; 
        float speed = baseSpeed + distance * 0.1f;

        Vector3 force = direction * speed;

        ball.SetParent(null, true);
        ballRb.isKinematic = false;
        ballRb.AddForce(force, ForceMode.Force);

        time = 0;
        duration = 2f;

        while (!ballComp.keeperReceive && time < duration)
        {
            time += Time.deltaTime;
            yield return null;
        }

        fm.keeper.GetComponent<Fielder>().KeeperRecieve(Vector3.zero, ball);
        Debug.Log("fld done");

        yield return new WaitForSeconds(0.3f);

        Gameplay.instance.deliveryDead = true;

        StopAllCoroutines();
    }

    #region HelperMethods

    private bool IsBallComingAtFielder()
    {
        Vector3 toBall = (ball.position - transform.position).normalized;

        float angleToBall = Vector3.Angle(transform.forward, toBall);

        float thresholdAngle = 30f;
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    bool FielderCanReachOnTime(Vector3 position)
    {
        float distance = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(transform.position.x, transform.position.z)) + 0.57f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(FieldManager.hitBallPos, FieldManager.hitVelocity, timeReq);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(positionAtReqTime.x, positionAtReqTime.z));
        return fielderDistanceToPredictedPos / runSpeed <= timeReq;
    }

    Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float time)
    {
        // Gravity effect on ball
        float gravity = Physics.gravity.y; // Usually -9.81
        Vector3 displacement = velocity * time + 0.5f * new Vector3(0, gravity, 0) * time * time;

        Vector3 returnVec = initialPosition + displacement;
        returnVec.y = transform.position.y; // Align height with fielder
        return returnVec;
    }

    bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition)
    {
        // Convert to 2D for ground movement
        Vector2 fielderToBall = new Vector2(ballPosition.x, ballPosition.z) - new Vector2(fielderPosition.x, fielderPosition.z);
        Vector2 ballDirection = new Vector2(ballVelocity.x, ballVelocity.z).normalized;

        // Check if the ball is moving in the direction of the fielder and has passed the fielder
        float distanceAlongDirection = Vector2.Dot(fielderToBall, ballDirection);

        // The fielder should chase the ball if the ball is moving past the fielder in the direction of travel
        return distanceAlongDirection > 0;
    }    
    #endregion

    public void KeeperReset()
    {
        //agent.Stop();
        chaseMode = false;
        attackMode = false;
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
        chaseMode = false;
        attackMode = false;
        transform.GetChild(transform.childCount - 1).GetComponent<BoxCollider>().center = new Vector3(0, 0.09848619f, 0.02f);
        ikControl.PlayAnimation(idleClip);
        StopCoroutine(StartField());
        rightHand.localPosition = idleRightHand;
        leftHand.localPosition = idleLeftHand;
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        ikControl.SetIKWeight(0);
        this.enabled = false;
    }
}
