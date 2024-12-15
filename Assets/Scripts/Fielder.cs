using System;
using System.Collections;
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

    private void OnEnable()
    {
        //agent = GetComponent<NavMeshAgent>();
        //agent.speed = 30;
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
    }

    IEnumerator StartField()
    {
        while(startedRun)
        {
            if (!Gameplay.instance.deliveryDead)
            {
                if(Vector2.Distance(new Vector2 (transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z))<1)
                {
                    ReachedBall();
                    if (!ballComp.groundShot)
                    {
                        Gameplay.instance.Out();
                    }
                    Gameplay.instance.deliveryDead = true;
                    ballRb.isKinematic = true;
                    yield break; ;
                }
                Vector3 moveDirection = (targetPosition - transform.position).normalized;
                if (moveDirection != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
                }
                if (IsBallComingAtFielder() && ballComp.groundShot)
                {
                    Debug.Log("Coming to fielder");
                    WaitForBall();
                }
                else
                {
                    Debug.Log("away from fielder");
                    if (!reachedBall)
                    {
                        RunToBall();
                    }
                    else
                    {
                        ReachedBall();
                        if (!ballComp.groundShot)
                        {
                            Gameplay.instance.Out();
                        }
                        Gameplay.instance.deliveryDead = true;
                        ballRb.isKinematic = true;
                    }
                }
            }
            else
            {
                Reset();
            }
            yield return null;
        }
        Reset();
    }

    void ReachedBall()
    {
        //agent.isStopped = true;
        GetComponent<Animator>().SetTrigger("StopField");
    }

    private bool IsBallComingAtFielder()
    {
        Vector3 toBall = (ball.position - transform.position).normalized;

        float angleToBall = Vector3.Angle(transform.forward, toBall);

        float thresholdAngle = 2f;
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    void RunToBall()
    {
        if (reachedInterim || reachedBall) return;
        if (!ballComp.groundShot)
        {
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 3f)
            {
                if (ball.position.y < 7f)
                {
                    Debug.Log("reached ball air");
                    ballRb.isKinematic = true;
                    Gameplay.instance.Out();
                    reachedBall = true;
                }
            }
        }
        else
        {
            
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 2f)
            {
                Debug.Log("reached ball ground");
                ReachedBall();
                Gameplay.instance.deliveryDead = true;
                ballRb.isKinematic = true;
                reachedBall = true;
            }
            else if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2 (targetPosition.x, targetPosition.z)) < 1f && !reachedBall && !reachedInterim)
            {
                //agent.ResetPath();
                Debug.Log("reached target go for");
                StartCoroutine(CollectBall());
                reachedInterim = true;
                return;
            }
        }        
        Debug.Log("runnn");
        //agent.SetDestination(targetPosition);
        transform.position = Vector3.MoveTowards(transform.position, UpdateTargetPosition(), runSpeed * Time.deltaTime);        
    }    

    IEnumerator CollectBall()
    {
        while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 1f)
        {
            //agent.SetDestination(new Vector3(ball.position.x, transform.position.y, ball.position.z));
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
            yield return null;
        }
        Debug.Log("reached collection");
        ReachedBall();
        Gameplay.instance.deliveryDead = true;
        ballRb.isKinematic = true;
        reachedBall = true;
        yield return null;
    }

    void WaitForBall()
    {
        if (ballRb.velocity.magnitude < 35)
        {
            Debug.Log("slowed beyound thrshold");
            //agent.SetDestination(new Vector3(ball.position.x, transform.position.y, ball.position.z));
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
        }

        else
        {
            //agent.SetDestination(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPosition.x, transform.position.y, targetPosition.z), runSpeed * Time.deltaTime);
        }

        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 3)
        {
            Debug.Log("slowed beyound thrshold reached");
            ReachedBall();
            Gameplay.instance.deliveryDead = true;
            ballRb.isKinematic = true;
            reachedBall = true;
        }
    }
    

    private Vector3 UpdateTargetPosition()
    {
        if (ballRb.velocity.magnitude < 20f || Vector3.Distance(transform.position, ball.position) < 0.5f)
        {
            return ball.position;
        }

        if (ballComp.groundShot)
        {
            if (ballWasAirborne)
            {
                targetPosition = ball.position;
                targetPosition.y = transform.position.y;
                return targetPosition;
            }
            else
            {
                Vector3 updatedPos = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.right);
                updatedPos.y = transform.position.y;
                return updatedPos;
            }
        }
        else
        {
            Debug.Log("ball still air");
            if(FielderCanReachOnTime())
            {
                return fm.marker.position;
            }
            else
            {
                Vector3 updatedPos = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.right);
                updatedPos.y = transform.position.y;
                return updatedPos;
            }
        }        
    }

    bool FielderCanReachOnTime()
    {
        float distance = Vector2.Distance(new Vector2(fm.marker.position.x, fm.marker.position.z), new Vector2(transform.position.x, transform.position.z))-0.3f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(FieldManager.hitBallPos, FieldManager.hitVelocity, timeReq);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2 (transform.position.x, transform.position.z), new Vector2 (positionAtReqTime.x, positionAtReqTime.z));
        Debug.Log("fielder" + gameObject.name + " will reach on ttime " + (fielderDistanceToPredictedPos / runSpeed <= timeReq));
        return fielderDistanceToPredictedPos / runSpeed <= timeReq;
    }

    Vector3 NewPos()
    {
        float distance = Vector2.Distance(new Vector2(fm.marker.position.x, fm.marker.position.z), new Vector2(transform.position.x, transform.position.z)) - 0.3f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(FieldManager.hitBallPos, FieldManager.hitVelocity, timeReq);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(positionAtReqTime.x, positionAtReqTime.z));
        Debug.Log("fielder" + gameObject.name + " will reach on ttime " + (fielderDistanceToPredictedPos / runSpeed <= timeReq));
        if (fielderDistanceToPredictedPos / runSpeed <= timeReq)
        {
            return positionAtReqTime;
        }
        else
        {
            return Vector3.zero;
        }
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
            fm.marker.position = targetPosition;
        }
        StartCoroutine(StartField());
        //GetComponent<Animator>().Play("running");
    }

    public void Reset()
    {
        //agent.Stop();
        //agent.ResetPath();
        StopCoroutine(StartField());
        GetComponent<Animator>().SetBool("Stop", true);
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        reachedBall = true;
        GetComponent<Animator>().ResetTrigger("Stop");
        this.enabled = false;
    }
}
