using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score;
    private Vector3 targetPosition, actualPos;
    public bool reachedBall;
    BallHit ballComp;
    Rigidbody ballRb;
    Transform ball;
    public bool active, startedRun;

    private void OnEnable()
    {
        actualPos = transform.position;
    }

    bool pot;

    private void Update()
    {
        if(active)
        {
            if (IsBallComingAtFielder() || pot)
            {
                pot = true;
                Debug.Log("Coming to fielder");
                
                if (!reachedBall)
                {
                    if (!ballComp.groundShot)
                    {
                        RunToBall();
                    }
                    else
                    {
                        WaitForBall();
                    }
                }
                else
                {
                    if(!ballComp.groundShot)
                    {
                        Pusher.instance.Out();
                    }
                    Pusher.instance.deliveryDead = true;
                    ballRb.isKinematic = true;
                }
            }
            else
            {
                if (pot) return;
                Debug.Log("away from fielder");
                if(!reachedBall)
                {
                    RunToBall();
                }
                else
                {
                    if (!ballComp.groundShot)
                    {
                        Pusher.instance.Out();
                    }
                    Pusher.instance.deliveryDead = true;
                    ballRb.isKinematic = true;
                }
            }
        }
    }

    private bool IsBallComingAtFielder()
    {
        // Get the direction from the fielder to the ball
        Vector3 toBall = (ball.position - transform.position).normalized;

        // Calculate the angle between the fielder's forward direction and the direction to the ball
        float angleToBall = Vector3.Angle(transform.forward, toBall);

        // If the angle is close to 180 degrees, the ball is coming straight at the fielder
        float thresholdAngle = 5f; // Adjust this threshold as needed
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    void RunToBall()
    {
        if(!ballComp.groundShot)
        {
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 1f)
            {
                if(ball.position.y<=transform.position.y)
                {
                    Debug.Log("run to reeach air");
                    ballRb.isKinematic = true;
                    reachedBall = true;
                }
            }
        }
        else
        {
            if(Vector2.Distance(new Vector2 (transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z))<=1f)
            {
                Debug.Log("run to reeach ground");
                ballRb.isKinematic = true;
                reachedBall = true;
            }
            if (Vector3.Distance(transform.position, aimPos) < 1f && !reachedBall)
            {
                Debug.Log("reeach aim go for");
                StartCoroutine(CollectBall());
                dontRun = true;
                return;
            }
        }
        if(!dontRun)
        {
            Debug.Log("runnn");
            transform.position = Vector3.MoveTowards(transform.position, UpdateTargetPosition(), runSpeed * Time.deltaTime);
        }
    }

    bool dontRun;

    IEnumerator CollectBall()
    {
        while(Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 1f)
        { 
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
            yield return null;
        }
        yield return null;
    }

    void WaitForBall()
    {
        if (ballRb.velocity.magnitude < 35)
        {
            Debug.Log("slowed beyound thrshold");
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ball.position.x, transform.position.y, ball.position.z), runSpeed * Time.deltaTime);
            //transform.position += -1 * transform.forward * runSpeed * Time.deltaTime;
        }

        else
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(aimPos.x, transform.position.y, aimPos.z), runSpeed * Time.deltaTime);
        }
        
        if (Vector3.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 10)
        {
            Debug.Log("slowed beyound thrshold reached");
            ballRb.isKinematic = true;
            reachedBall = true;
        }        
    }

    private Vector3 UpdateTargetPosition()
    {        
        if (ballComp.groundShot)
        {
            if (ShouldChase(ball.position, ballRb.velocity, transform.position, aimPos))
            {
                Debug.Log("chase");
                return PredictBallPosition(ball.position, ballRb.velocity, Time.deltaTime);
            }
            else
            {
                Debug.Log("no chase");
                return aimPos;
            }
        }
        else
        {            
            return targetPosition;
        }
    }

    public Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float time)
    {
        Vector3 returnVec = initialPosition + velocity * time;
        returnVec.y = transform.position.y;
        return returnVec;
    }

    public static bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity,
        Vector3 fielderPosition, Vector3 intersectionPoint)
    {
        Vector3 ballToIntersection = intersectionPoint - ballPosition;
        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        // If the dot product is negative, the intersection point is behind the ball
        return Vector3.Dot(ballToIntersection, ballDirection) < 0;
    }

    public static Vector3 CalculateInterceptPosition(Vector3 ballPosition, Vector3 ballVelocity,
        Vector3 fielderPosition, Vector3 fielderForward)
    {
        // Normalize the ball velocity to get direction
        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        // Calculate the perpendicular line from fielder's position along their forward direction
        // We use the parametric equation of a line: P = P0 + t * V
        // Where P0 is fielder position, V is fielder's forward direction
        // And the perpendicular line equation: (P - P1) • V1 = 0
        // Where P1 is ball position and V1 is ball direction

        // Calculate intersection using the dot product method
        float denominator = Vector3.Dot(ballDirection, fielderForward);

        // Check if lines are parallel (or nearly parallel)
        if (Math.Abs(denominator) < 0.001f)
        {
            // Lines are parallel, return the closest point on ball's path to fielder
            Vector3 toFielder = fielderPosition - ballPosition;
            float projection = Vector3.Dot(toFielder, ballDirection);
            return ballPosition + ballDirection * projection;
        }

        // Calculate the vector from ball to fielder
        Vector3 ballToFielder = fielderPosition - ballPosition;

        // Calculate intersection parameter
        float t = Vector3.Dot(ballToFielder, fielderForward) / denominator;

        // Calculate intersection point
        Vector3 intersectionPoint = ballPosition + ballDirection * t;

        // Check if intersection point is behind the ball's current position
        Vector3 toBall = ballPosition - intersectionPoint;
        if (Vector3.Dot(toBall, ballDirection) > 0)
        {
            // Ball has already passed this point, calculate chase position
            // For simplicity, we'll project the fielder's position onto the ball's path
            Vector3 toFielder = fielderPosition - ballPosition;
            float projection = Vector3.Dot(toFielder, ballDirection);
            return ballPosition + ballDirection * projection;
        }

        return intersectionPoint;
    }

    /// <summary>
    /// Calculates the ball's position at a given time assuming constant velocity
    /// </summary>

    Vector3 aimPos;

    public void StartField(Vector3 position, Transform ball)
    {
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;
        targetPosition = position;
        aimPos = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
        aimPos.y = transform.position.y;
    }

    
    private void OnReachedTarget()
    {
        Debug.Log($"{gameObject.name} has reached the target position.");
        reachedBall = true;
        enabled = false; 
    }

    public void Reset()
    {
        pot = false;
        dontRun = false;
        transform.position = actualPos;
        reachedBall = false;
        this.enabled = false;
    }
}
