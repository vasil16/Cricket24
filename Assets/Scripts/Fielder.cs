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
    Transform ball;
    public bool canReachInTime, startedRun, reachedBall, reachedInterim;
    private bool ballWasAirborne;
    [SerializeField] FieldManager fm;
    //NavMeshAgent agent;

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

        float thresholdAngle = 2f;
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    IEnumerator RunToBall()
    {
        while(!reachedBall)
        {
            if (Gameplay.instance.deliveryDead || reachedBall)
            {
                GetComponent<Animator>().SetTrigger("StopField");
                GetComponent<Animator>().SetBool("Stop", true);
                yield break;
            }
                            
            if(ballComp.groundShot)
            {            
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 2f)
                {
                    Debug.Log(gameObject.name + "  reached ball ground");
                    reachedBall = true;                        
                }
                else if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2 (targetPosition.x, targetPosition.z)) < 1f && !reachedBall && !reachedInterim)
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
                        //if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 2f)
                        //{
                        //    reachedBall = true;
                        //}x                        
                        StartCoroutine(CollectBall());
                    }
                    yield break;
                }
            }
            else
            {
                //if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 3f)
                //{
                //    if (ball.position.y < 7f)
                //    {
                //        Debug.Log("reached ball air");
                //        reachedBall = true;
                //    }
                //}
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) < 1f && !reachedBall && !reachedInterim)
                {
                    //agent.ResetPath();
                    Debug.Log(gameObject.name + " reached target go for");
                    StartCoroutine(CollectBall());
                    reachedInterim = true;
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 2f)
                    {
                        reachedBall = true;
                    }
                    yield break;
                }
            }
            transform.position = Vector3.MoveTowards(transform.position, UpdateTargetPosition(), runSpeed * Time.deltaTime);
            yield return null;
        }
        Debug.Log("runningtoball");
        
        ReachedBall();
        ballRb.isKinematic = true;
        if (!ballComp.groundShot)
        {
            Gameplay.instance.Out();
        }
        Gameplay.instance.deliveryDead = true;
    }

    void ReachedBall()
    {
        //agent.isStopped = true;
        GetComponent<Animator>().SetBool("Stop", true);
        GetComponent<Animator>().SetTrigger("StopField");
    }

    IEnumerator WaitForBall()
    {
        Debug.Log(gameObject.name + "  waiting for ball");
        while(!reachedBall)
        {
            if (Gameplay.instance.deliveryDead)
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
        Gameplay.instance.deliveryDead = true;
        ballRb.isKinematic = true;
    }

    IEnumerator CollectBall()
    {
        GetComponent<Animator>().SetTrigger("Slow");
        while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 1f)
        {
            if (Gameplay.instance.deliveryDead || reachedBall)
            {
                GetComponent<Animator>().SetTrigger("StopField");
                GetComponent<Animator>().SetBool("Stop", true);
                yield break;
            }
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * 0.8f * Time.deltaTime);
            yield return null;
        }
        Debug.Log("reached collection");
        ReachedBall();
        if (!ballComp.groundShot)
        {
            Gameplay.instance.Out();
        }
        Gameplay.instance.deliveryDead = true;
        ballRb.isKinematic = true;
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
            if (ballWasAirborne)
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
                target = fm.marker.position;
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

            target.y = transform.position.y;
        }

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

    public static bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition, Vector3 intersectionPoint)
    {
        Vector3 fielderToBall = ballPosition - fielderPosition;

        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        return Vector3.Dot(fielderToBall, ballDirection) < 0;
    }

    public static Vector3 CalculateInterceptPosition(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition, Vector3 fielderForward)
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
        StartCoroutine(StartField());        
    }

    public void Reset()
    {
        //agent.Stop();
        //agent.ResetPath();
        StopCoroutine(StartField());        
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
