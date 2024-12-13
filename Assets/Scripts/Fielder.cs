using System;
using System.Collections;
using UnityEngine;

public class Fielder : MonoBehaviour
{
    public float runSpeed, score, angleDiff;
    private Vector3 targetPosition, actualPos, actualRot;
    public bool reachedBall;
    BallHit ballComp;
    Rigidbody ballRb;
    Transform ball;
    public bool startedRun;
    public float timeToReachLanding;
    public bool canReachInTime;

    private void OnEnable()
    {
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
    }


    private void Update()
    {
        if (startedRun && !Gameplay.instance.deliveryDead)
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
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 3f)
            {
                if (ball.position.y < 7f)
                {
                    Debug.Log("run to reeach air");
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
                Debug.Log("run to reeach ground");
                Gameplay.instance.deliveryDead = true;
                ballRb.isKinematic = true;
                reachedBall = true;
            }
            if (Vector3.Distance(transform.position, targetPosition) < 1f && !reachedBall)
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
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPosition.x, transform.position.y, targetPosition.z), runSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(ball.position.x, ball.position.z)) <= 10)
        {
            Debug.Log("slowed beyound thrshold reached");
            Gameplay.instance.deliveryDead = true;
            ballRb.isKinematic = true;
            reachedBall = true;
        }
    }

    private bool ballWasAirborne;

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
                targetPosition = ball.position; // Ball has landed, aim at its current position
            }
            else
            {
                // Decide between interception and current aim position
                //aimPos = ShouldChase(ball.position, ballRb.velocity, transform.position, aimPos)
                //    ? PredictBallPosition(ball.position, ballRb.velocity, Time.deltaTime)
                //    : aimPos;
            }
        }
        else
        {
            Debug.Log("ball still air");
            if(FielderCanReachOnTime())
            {
                //targetPosition = PredictLandingPosition(ballRb, 50);
                //targetPosition.y = transform.position.y;
                //return targetPosition;
                return transform.parent.GetComponent<FieldManager>().marker.position;
            }
            else
            {
                Vector3 updatedPos = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
                updatedPos.y = transform.position.y;
                return updatedPos;
            }
        }

        // Ensure the fielder maintains their y-coordinate
        targetPosition.y = transform.position.y;

        return targetPosition;
    }

    bool FielderCanReachOnTime()
    {
        float distance = Vector2.Distance(new Vector2(transform.parent.GetComponent<FieldManager>().marker.position.x, transform.parent.GetComponent<FieldManager>().marker.position.z), new Vector2(transform.position.x, transform.position.z))-0.3f;
        float timeReq = distance / runSpeed;
        Vector3 positionAtReqTime = PredictBallPosition(FieldManager.hitBallPos, FieldManager.hitVelocity, timeReq);
        float fielderDistanceToPredictedPos = Vector2.Distance(new Vector2 (transform.position.x, transform.position.z), new Vector2 (positionAtReqTime.x, positionAtReqTime.z));
        Debug.Log("fielder" + gameObject.name + " will reach on ttime " + (fielderDistanceToPredictedPos / runSpeed <= timeReq));
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
        // Vector from the fielder to the ball
        Vector3 fielderToBall = ballPosition - fielderPosition;

        // Normalize the ball's velocity to get its direction
        Vector3 ballDirection = Vector3.Normalize(ballVelocity);

        // Check if the ball is behind the fielder
        // If the dot product is negative, the ball is behind the fielder
        return Vector3.Dot(fielderToBall, ballDirection) < 0;
    }

    Vector3 PredictLandingPosition(Rigidbody ballRb, int steps)
    {
        Vector3 position = ballRb.position;
        Vector3 velocity = ballRb.velocity;
        Vector3 gravity = Physics.gravity;

        // Time to hit the ground based on the vertical component (taking gravity into account more accurately)
        float timeToGround = 0f;

        if (velocity.y != 0f)
        {
            // If the ball is moving upwards or downwards, calculate the time to reach the ground
            timeToGround = Mathf.Abs(velocity.y) / Mathf.Abs(gravity.y);
            if (velocity.y > 0f)
            {
                // Calculate time to ground considering the upward motion
                timeToGround = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2 * gravity.y * position.y)) / Mathf.Abs(gravity.y);
            }
        }

        // Start simulating the ball's motion
        float stepTime = timeToGround / steps;  // Divide the total time by the number of steps to make it smooth

        for (int i = 0; i < steps; i++)
        {
            // Horizontal motion remains unaffected by gravity
            position.x += velocity.x * stepTime;
            position.z += velocity.z * stepTime;

            // Apply gravity to the vertical motion
            velocity.y += gravity.y * stepTime;
            position.y += velocity.y * stepTime;

            // Check if the ball hits the ground or leaves the bounds
            if (position.y <= -4.221f || !Gameplay.instance.stadiumBounds.Contains(position))
            {
                position.y = Mathf.Max(position.y, -4.221f);  // Ensure it doesn't go below the ground level
                break;
            }
        }

        return new Vector3(position.x, transform.position.y, position.z);  // Only return x and z coordinates, with a fixed y (fielderY)
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


    public void StartField(Vector3 position, Transform ball)
    {
        ballComp = ball.GetComponent<BallHit>();
        ballRb = ball.GetComponent<Rigidbody>();
        this.ball = ball;
        ballWasAirborne = ball.GetComponent<BallHit>().groundShot==false;
        if(ballWasAirborne)
        {
            targetPosition = position;
        }
        else
        {
            targetPosition = CalculateInterceptPosition(ball.position, ballRb.velocity, transform.position, transform.forward);
        }
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
