using System;
using System.Collections;
using UnityEngine;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score;
    private Vector3 targetPosition, actualPos, actualRot;
    public bool reachedBall;
    BallHit ballComp;
    Rigidbody ballRb;
    Transform ball;
    public bool startedRun;

    private void OnEnable()
    {
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
    }


    private void Update()
    {
        if (startedRun)
        {
            Vector3 groundUp = Vector3.Cross(transform.right, ball.position - transform.position);

            //transform.LookAt(ball, groundUp);
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, groundUp.y, transform.rotation.eulerAngles.z);
            if (IsBallComingAtFielder())
            {
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
                    if (!ballComp.groundShot)
                    {
                        Gameplay.instance.Out();
                    }
                    Gameplay.instance.deliveryDead = true;
                    ballRb.isKinematic = true;
                }
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
                    if (!ballComp.groundShot)
                    {
                        Gameplay.instance.Out();
                    }
                    Gameplay.instance.deliveryDead = true;
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
        float thresholdAngle = 20f; // Adjust this threshold as needed
        return Mathf.Abs(angleToBall - 180f) < thresholdAngle;
    }

    void RunToBall()
    {
        if (!ballComp.groundShot)
        {
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 1f)
            {
                if (ball.position.y < 7f)
                {
                    Debug.Log("run to reeach air");
                    ballRb.isKinematic = true;
                    reachedBall = true;
                }
            }
        }
        else
        {
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 1f)
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
        if (!dontRun)
        {
            Debug.Log("runnn");
            transform.position = Vector3.MoveTowards(transform.position, UpdateTargetPosition(), runSpeed * Time.deltaTime);
        }
    }

    bool dontRun;

    IEnumerator CollectBall()
    {
        while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 1f)
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

    private bool ballWasAirborne;

    //private Vector3 UpdateTargetPosition()
    //{

    //    if (ballRb.velocity.magnitude < 1f || Vector3.Distance(transform.position, ball.position) < 0.5f)
    //    {
    //        return ball.position; // Aim directly at the ball
    //    }
    //    if (ballComp.groundShot && ballWasAirborne)
    //    {
    //        //if (ShouldChase(ball.position, ballRb.velocity, transform.position, aimPos))
    //        //{
    //        //    Debug.Log("Chase ball to interception point");
    //        //    return PredictBallPosition(ball.position, ballRb.velocity, Time.deltaTime);
    //        //}
    //        //else
    //        //{
    //        //    Debug.Log("Move to aim position");
    //        //    return aimPos;
    //        //}
    //        aimPos = ball.position;
    //        aimPos.y = transform.position.y;
    //        //Debug.Log("Ball landed, updating aimPos to ball's current position");
    //        return aimPos;
    //    }
    //    if (ballComp.groundShot && !ballWasAirborne)
    //    {
    //        if (ShouldChase(ball.position, ballRb.velocity, transform.position, aimPos))
    //        {
    //            Debug.Log("Chase ball to interception point");
    //            return PredictBallPosition(ball.position, ballRb.velocity, Time.deltaTime);
    //        }
    //        else
    //        {
    //            Debug.Log("Move to aim position");
    //            return aimPos;
    //        }
    //    }
    //    //if (ballComp.groundShot || !ballWasAirborne)
    //    //{
    //    //    // If the ball was grounded at the start, use the original logic
    //    //    if (ShouldChase(ball.position, ballRb.velocity, transform.position, aimPos))
    //    //    {
    //    //        Debug.Log("Chase ball to interception point");
    //    //        return PredictBallPosition(ball.position, ballRb.velocity, Time.deltaTime);
    //    //    }
    //    //    else
    //    //    {
    //    //        Debug.Log("Move to aim position");
    //    //        return aimPos;
    //    //    }
    //    //}
    //    else
    //    {
    //        // If the ball was airborne and has now landed, update aimPos to the ball's current position
    //        //aimPos = ball.position;
    //        //aimPos.y = transform.position.y;
    //        //Debug.Log("Ball landed, updating aimPos to ball's current position");
    //        return aimPos;
    //    }
    //}

    private Vector3 UpdateTargetPosition()
    {
        // Ball velocity and proximity checks
        if (ballRb.velocity.magnitude < 1f || Vector3.Distance(transform.position, ball.position) < 0.5f)
        {
            return ball.position; // Aim directly at the ball
        }

        if (ballComp.groundShot)
        {
            if (ballWasAirborne)
            {
                aimPos = ball.position; // Ball has landed, aim at its current position
            }
            else
            {
                // Decide between interception and current aim position
                aimPos = ShouldChase(ball.position, ballRb.velocity, transform.position, aimPos)
                    ? PredictBallPosition(ball.position, ballRb.velocity, Time.deltaTime)
                    : aimPos;
            }
        }
        else
        {
            // Ball is still airborne, calculate interception point
            //aimPos = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
            Debug.Log("ball still air");
            targetPosition.y = transform.position.y;
            return targetPosition;
        }

        // Ensure the fielder maintains their y-coordinate
        aimPos.y = transform.position.y;

        return aimPos;
    }


    public Vector3 PredictBallPosition(Vector3 initialPosition, Vector3 velocity, float time)
    {
        Vector3 returnVec = initialPosition + velocity * time;
        returnVec.y = transform.position.y;
        return returnVec;
    }

    //public static bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition, Vector3 intersectionPoint)
    //{
    //    Vector3 ballToIntersection = intersectionPoint - ballPosition;
    //    Vector3 ballDirection = Vector3.Normalize(ballVelocity);

    //    // If the dot product is negative, the intersection point is behind the ball
    //    return Vector3.Dot(ballToIntersection, ballDirection) < 0;
    //}

    public static bool ShouldChase(Vector3 ballPosition, Vector3 ballVelocity, Vector3 fielderPosition, Vector3 intersectionPoint)
    {
        // Vector from the fielder to the ball
        Vector3 fielderToBall = ballPosition - fielderPosition;

        // Normalize the ball's velocity to get its direction
        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        // Check if the ball is behind the fielder
        // If the dot product is negative, the ball is behind the fielder
        return Vector3.Dot(fielderToBall, ballDirection) < 0;
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

        // Determine if the ball is airborne at the time the script is enabled
        ballWasAirborne = ball.GetComponent<BallHit>().groundShot==false;

        // Calculate aimPos based on the ball's current state
        if (ballWasAirborne)
        {
            aimPos = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
        }
        else
        {
            aimPos = ball.position; // Grounded ball targets its current position
        }
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
        startedRun = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);
        reachedBall = false;
        this.enabled = false;
    }
}
