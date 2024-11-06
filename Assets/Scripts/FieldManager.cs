using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public static Action<Vector3, Vector3> StartCheckField;
    public static Action ResetFielder;

    [SerializeField] private List<GameObject> fielders;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float fieldingRange = 1.5f;
    [SerializeField] private float speedThreshold = 1.5f;
    private Coroutine fielderCoroutine;

    private Vector3 initialFielderPosition;
    [SerializeField] private GameObject bestFielder, marker;

    private Transform ball;
    private bool boostDeep;

    public float ballAngle, fielderAngle;

    private void Start()
    {
        StartCheckField = CheckAndInitiateFielder;
        ResetFielder = ResetField;
    }

    void CheckAndInitiateFielder(Vector3 ballAt, Vector3 direction)
    {
        StartCoroutine(StartCheckingField(ballAt));
    }

    IEnumerator StartCheckingField(Vector3 ballAt)
    {
        yield return new WaitForSeconds(.3f);

        ball = Pusher.instance.currentBall;
        Rigidbody ballRigidbody = ball.GetComponent<Rigidbody>();

        bool isAerialShot = ballAt.y < ball.transform.position.y;

        int steps = 50;  // A high number of steps for smoother prediction

        // Predict the landing position if it's an aerial shot
        Vector3 landingPosition = isAerialShot ? PredictLandingPosition(ballRigidbody, steps) : Vector3.zero;

        // Set marker position to the predicted landing position
        marker.transform.position = landingPosition;

        // Score for the fielder interception
        float bestScore = float.MinValue;
        bestFielder = null;

        foreach (var fielder in fielders)
        {
            bool isDeep = fielder.CompareTag("DeepFielder");

            // Calculate angles between ball and fielder
            Vector3 ballDir = ball.position - ballAt;
            ballAngle = Mathf.Atan2(ballDir.z, ballDir.x) * Mathf.Rad2Deg;

            Vector3 fielderDir = fielder.transform.position - Pusher.instance.batCenter.position;
            fielderAngle = Mathf.Atan2(fielderDir.z, fielderDir.x) * Mathf.Rad2Deg;

            // Calculate direction boost for fielder positioning
            float directionBoost = -1 * Mathf.Abs(fielderAngle - ballAngle);

            // Calculate distance to landing position
            Vector3 toLandingPosition = landingPosition - fielder.transform.position;
            float distanceToLanding = toLandingPosition.magnitude;

            

            // Calculate time required for the fielder to intercept the ball
            float timeToIntercept = distanceToLanding / runSpeed;

            // Calculate the score for each fielder based on distance, direction, and position
            float score = 0f;

            boostDeep = ballRigidbody.velocity.magnitude > speedThreshold;

            if (boostDeep && isDeep)
            {
                score += 2;  // Deep fielder advantage
            }

            score += directionBoost;

            if (score > bestScore)
            {
                bestScore = score;
                bestFielder = fielder;
            }
        }

        // If the best fielder is found, execute the appropriate action
        if (bestFielder != null)
        {
            initialFielderPosition = bestFielder.transform.position;
            if (isAerialShot)
            {
                HandleAerialShot(landingPosition);
            }
            else
            {
                HandleGroundedShot(ballAt);
            }
        }
        else
        {
            Debug.Log("No fielder able to reach the target position in time.");
        }
    }


    void HandleGroundedShot(Vector3 ballAt)
    {
        Vector3 ballDirection = (ball.position - ballAt).normalized;
        Vector3 fielderToBall = ball.position - bestFielder.transform.position;
        float angleToBall = Vector3.Angle(ballDirection, fielderToBall);

        if (angleToBall < 10f && fielderToBall.magnitude < fieldingRange)
        {
            Debug.Log("Grounded ball coming straight at fielder. Fielder stops the ball.");
            StopBall();
        }
        else
        {
            Vector3 interceptPoint = FindBestIntersectionPoint(ball.GetComponent<Rigidbody>(), bestFielder.transform.position);
            fielderCoroutine = StartCoroutine(MoveToTargetWithGroundCheck(bestFielder, interceptPoint));
        }
    }

    void HandleAerialShot(Vector3 landingPosition)
    {
        if (Vector3.Distance(new Vector2(landingPosition.x, landingPosition.z), new Vector2(bestFielder.transform.position.x, bestFielder.transform.position.z)) < fieldingRange)
        {
            Debug.Log("Aerial ball landing close to fielder. Fielder stays to catch.");
            StartCoroutine(WaitForCatch());
        }
        else
        {
            Debug.Log("Aerial ball. Fielder moves to landing position.");
            fielderCoroutine = StartCoroutine(MoveToTargetWithGroundCheck(bestFielder, landingPosition));
        }
    }

    IEnumerator WaitForCatch()
    {
        yield return new WaitUntil(() => ball.position.y <= bestFielder.transform.position.y);
        Debug.Log("Caught the ball!");
        Pusher.instance.Out();
    }

    void StopBall()
    {
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        ballRb.isKinematic = true;
        Pusher.instance.deliveryDead = true;
    }

    Vector3 FindBestIntersectionPoint(Rigidbody ballRb, Vector3 fielderPos)
    {
        Vector3 ballDir = ballRb.velocity.normalized;
        Vector3 relativePos = fielderPos - ball.position;
        float distanceAlongPath = Vector3.Dot(relativePos, ballDir);
        Vector3 interceptPoint = ball.position + ballDir * distanceAlongPath;
        return new Vector3(interceptPoint.x, fielderY, interceptPoint.z);
    }

    IEnumerator MoveToTargetWithGroundCheck(GameObject fielder, Vector3 targetPosition)
    {
        while (Vector3.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z),new Vector2(ball.transform.position.x, ball.transform.position.z)) > fieldingRange)
        {
            // Check if ball has hit the ground
            if (ball.GetComponent<BallHit>().groundShot)
            {
                Debug.Log("Ball hit the ground, adjusting fielder movement.");
                targetPosition = AdjustForGroundedBall(ball.position, fielder.transform.position);
            }

            fielder.transform.position = Vector3.MoveTowards(fielder.transform.position, targetPosition, runSpeed * Time.deltaTime);
            yield return null;
        }
        Debug.Log("Fielder reached the target position.");
        if (!ball.GetComponent<BallHit>().groundShot)
        {
            while(ball.transform.position.y>1.5f)
            {
                fielder.transform.position = Vector3.MoveTowards(fielder.transform.position, new Vector3(ball.transform.position.x, fielder.transform.position.y, ball.transform.position.z), runSpeed * Time.deltaTime);
                yield return null;
            }
            yield return null;
            Pusher.instance.Out();
        }
        ball.GetComponent<Rigidbody>().isKinematic = true;
        Pusher.instance.deliveryDead = true;
    }

    Vector3 AdjustForGroundedBall(Vector3 ballPosition, Vector3 fielderPosition)
    {
        Vector3 newDirection = (ballPosition - fielderPosition).normalized;

        Vector3 adjustedPos = ballPosition + newDirection * fieldingRange; // Move closer but maintain some distance

        return new Vector3(adjustedPos.x, fielderY, adjustedPos.z);
    }

    [SerializeField] float fielderY = 1.12f;

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
            if (position.y <= -4.221f || !Pusher.instance.stadiumBounds.Contains(position))
            {
                position.y = Mathf.Max(position.y, -4.221f);  // Ensure it doesn't go below the ground level
                break;
            }
        }

        return new Vector3(position.x, fielderY, position.z);  // Only return x and z coordinates, with a fixed y (fielderY)
    }


    void ResetField()
    {
        if (fielderCoroutine != null)
        {
            StopCoroutine(fielderCoroutine);
        }

        if (bestFielder != null)
        {
            bestFielder.transform.position = initialFielderPosition;
            Debug.Log("Fielder position reset.");
        }
    }
}
