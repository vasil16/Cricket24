using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score, angleDiff, timeToReachLanding;
    private Vector3 targetPosition, actualPos, actualRot;
    BallHit ballComp;
    Rigidbody ballRb;
    public Transform ball;
    public bool canReachInTime, startedRun, reachedBall, reachedInterim;
    private bool ballWasAirborne;
    [SerializeField] FieldManager fm;
    //NavMeshAgent agent;


    public Animator animator; // Reference to the Animator
    public float weight = 1.0f; // Weight of the IK effect

    void OnAnimatorIK(int layerIndex)
    {
        if (animator && gameObject.name == "keeper")
        {
            // Set the overall weight for IK
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);

            if (ball)
            {
                // **1. Adjust Upper Body Rotation Toward Ball**
                animator.SetLookAtWeight(1.0f, 0.6f, 0.6f, 1.0f, 0.5f); // Customize weights as needed
                animator.SetLookAtPosition(ball.position);

                // **2. IK for Right Hand**
                animator.SetIKPosition(AvatarIKGoal.RightHand, ball.position);
                Vector3 rightHandToBallDirection = ball.position - animator.GetIKPosition(AvatarIKGoal.RightHand);
                Quaternion rightHandRotation = Quaternion.LookRotation(rightHandToBallDirection, Vector3.up);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandRotation);

                // **3. IK for Left Hand (Offset for Two-Handed Catch)**
                Vector3 leftHandPosition = ball.position + new Vector3(-0.15f, 0, 0); // Slightly offset to the left
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
                Vector3 leftHandToBallDirection = ball.position - leftHandPosition;
                Quaternion leftHandRotation = Quaternion.LookRotation(leftHandToBallDirection, Vector3.up);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRotation);

                // **4. Optional: Adjust Spine Rotation**
                Transform chestBone = animator.GetBoneTransform(HumanBodyBones.Chest); // Use UpperChest if needed
                if (chestBone != null)
                {
                    Vector3 chestToBallDirection = ball.position - chestBone.position;
                    Quaternion chestRotation = Quaternion.LookRotation(chestToBallDirection, Vector3.up);
                    chestBone.rotation = Quaternion.Lerp(chestBone.rotation, chestRotation, 0.5f); // Blend for smoothness
                }
            }
            else
            {
                // Reset IK weights if no ball
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetLookAtWeight(0);
            }
        }
    }



    private void OnEnable()
    {
        //agent = GetComponent<NavMeshAgent>();
        //agent.speed = 30;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
    }

    IEnumerator StartField()
    {
        if (IsBallComingAtFielder() && ballComp.groundShot)
        {
            Debug.Log("Coming to fielder");
            StartCoroutine(WaitForBall());
        }

        else
        {
            Debug.Log("away from fielder");
            GetComponent<Animator>().Play("running");
            StartCoroutine(RunToBall());
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            if (moveDirection != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            }
        }
        yield break;
    }

    private bool IsBallComingAtFielder()
    {
        Vector3 toBall = (ball.position - transform.position).normalized;

        float angleToBall = Vector3.Angle(transform.forward, toBall);

        float thresholdAngle = 30f;
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    IEnumerator RunToBall()
    {
        while(!reachedBall)
        {
            if (Gameplay.instance.deliveryDead || reachedBall)
            {
                ReachedBall();
                yield break;
            }
                            
            if(ballComp.groundShot)
            {            
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 3f)
                {
                    Debug.Log(gameObject.name + "  reached ball ground");
                    reachedBall = true;                        
                }
                else if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2 (targetPosition.x, targetPosition.z)) < 3f && !reachedBall && !reachedInterim)
                {
                    Debug.Log(gameObject.name + " reached target go for collection");
                        
                    reachedInterim = true;
                    if(IsBallComingAtFielder())
                    {
                        GetComponent<Animator>().SetBool("Stop", true);
                        GetComponent<Animator>().SetTrigger("StopField");
                        StartCoroutine(WaitForBall());                            
                    }
                    else
                    {                      
                        StartCoroutine(CollectBall());
                    }
                    yield break;
                }
            }
            else
            {
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 3f && !reachedBall && !reachedInterim)
                {
                    //agent.ResetPath();
                    GetComponent<Animator>().SetTrigger("StopField");
                    GetComponent<Animator>().SetBool("Stop", true);
                    Debug.Log(gameObject.name + " reached target go for");
                    StartCoroutine(CollectBall());
                    reachedInterim = true;
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 2f)
                    {
                        reachedBall = true;
                        ReachedBall();
                        ballRb.isKinematic = true;
                        if (!ballComp.groundShot)
                        {
                            Gameplay.instance.Out();
                        }
                        Gameplay.instance.deliveryDead = true;
                    }

                    yield break;
                }
            }
            Vector3 moveDirection = (UpdateTargetPosition() - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            transform.position = Vector3.MoveTowards(transform.position, UpdateTargetPosition(), runSpeed * Time.deltaTime);
            yield return null;
        }
        Debug.Log("runningtoball");
        
        ReachedBall();
    }

    void ReachedBall()
    {
        //agent.isStopped = true;
        ballRb.isKinematic = true;
        if (!ballComp.groundShot)
        {
            Gameplay.instance.Out();
        }
        Gameplay.instance.deliveryDead = true;
        GetComponent<Animator>().SetBool("Stop", true);
        GetComponent<Animator>().SetTrigger("StopField");
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + "  waiting for ball");
        while(!reachedBall)
        {
            Vector3 moveDirection = (ball.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            if (reachedBall||Gameplay.instance.deliveryDead)
            {
                GetComponent<Animator>().SetTrigger("StopField");
                GetComponent<Animator>().SetBool("Stop", true);
                yield break;
            }
            if (ballRb.velocity.magnitude < 35)
            {
                Debug.Log("slowed beyound thrshold");
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
            }

            else
            {
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPosition.x, transform.position.y, targetPosition.z), runSpeed * Time.deltaTime);
            }

            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 3)
            {
                Debug.Log("slowed beyound thrshold reached");                
                reachedBall = true;
            }
            yield return null;
        }
        ReachedBall();
        
    }

    IEnumerator CollectBall()
    {
        GetComponent<Animator>().SetTrigger("Slow");
        while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 3f)
        {
            if (Gameplay.instance.deliveryDead || reachedBall)
            {
                GetComponent<Animator>().SetTrigger("StopField");
                GetComponent<Animator>().SetBool("Stop", true);
                yield break;
            }
            Vector3 moveDirection = (ball.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * 0.8f * Time.deltaTime);
            yield return null;
        }
        Debug.Log("reached collection");
        ReachedBall();
        
        reachedBall = true;
        yield return null;
    }

    private Vector3 UpdateTargetPosition()
    {
        Vector3 target;

        if (ballRb.velocity.magnitude < 20f || Vector3.Distance(transform.position, ball.position) < 0.5f)
        {
            target = ball.position;
        }
        else if (ballComp.groundShot)
        {
            if (ballWasAirborne || ShouldChase(ball.position, ballRb.velocity, transform.position))
            {
                target = ball.position;
                target.y = transform.position.y;
            }
            else
            {
                target = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
                target.y = transform.position.y;
            }
        }
        else
        {
            Debug.Log("ball still air");

            if (FielderCanReachOnTime())
            {
                target = targetPosition;
            }
            else
            {
                target = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
                target.y = transform.position.y;
            }
        }

        if (!Gameplay.instance.stadiumBounds.Contains(target))
        {
            Vector3 direction = (target - transform.position).normalized;

            Vector3 boundaryPoint = Gameplay.instance.stadiumBounds.ClosestPoint(target);

            Vector3 offset = -direction * 3f;
            target = boundaryPoint + offset;

        }
        target.y = transform.position.y;

        return target;
    }

    bool FielderCanReachOnTime()
    {
        float distance = Vector2.Distance(new Vector2(fm.marker.position.x, fm.marker.position.z), new Vector2(transform.position.x, transform.position.z))-0.3f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(FieldManager.hitBallPos, FieldManager.hitVelocity, timeReq);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2 (transform.position.x, transform.position.z), new Vector2 (positionAtReqTime.x, positionAtReqTime.z));
        return fielderDistanceToPredictedPos / runSpeed <= timeReq;
    }

    public Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float time)
    {
        Vector3 returnVec = initialPosition + velocity * time;
        returnVec.y = transform.position.y;
        return returnVec;
    }

    public static bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition)
    {
        Vector3 fielderToBall = ballPosition - fielderPosition;

        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        return Vector3.Dot(fielderToBall, ballDirection) < 0;
    }

    public Vector3 CalculateInterceptPosition(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition, Vector3 fielderForward)
    {
        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        float denominator = Vector3.Dot(ballDirection, fielderForward);

        if (Math.Abs(denominator) < 0.001f)
        {
            Vector3 toFielder = fielderPosition - ballPosition;
            float projection = Vector3.Dot(toFielder, ballDirection);
            return ballPosition + ballDirection * projection;
        }

        Vector3 ballToFielder = fielderPosition - ballPosition;

        float t = Vector3.Dot(ballToFielder, fielderForward) / denominator;

        Vector3 intersectionPoint = ballPosition + ballDirection * t;

        Vector3 toBall = ballPosition - intersectionPoint;

        if (Vector3.Dot(toBall, ballDirection) > 0)
        {
            Vector3 toFielder = fielderPosition - ballPosition;
            float projection = Vector3.Dot(toFielder, ballDirection);
            return ballPosition + ballDirection * projection;
        }

        return intersectionPoint;
    }


    public void Initiate(Vector3 position, Transform ball)
    {
        reachedBall = false;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;
        ballWasAirborne = ballComp.groundShot==false;
        if (ballWasAirborne)
        {
            targetPosition = position;
        }
        else
        {
            targetPosition = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
            //targetPosition = UpdateTargetPosition();
        }
        targetPosition.y = transform.position.y;
        StartCoroutine(StartField());        
    }

    public void Reset()
    {
        //agent.Stop();
        //agent.ResetPath();
        StopCoroutine(StartField());
        ball = null;
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        reachedBall = false;
        reachedInterim = false;
        GetComponent<Animator>().ResetTrigger("Stop");
        GetComponent<Animator>().ResetTrigger("StopField");
        this.enabled = false;
    }
}
