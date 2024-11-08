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
    [SerializeField] private float slowThreshold = 30f;
    private Coroutine fielderCoroutine;

    private Vector3 initialFielderPosition;
    [SerializeField] private GameObject marker;

    public static GameObject bestFielder;

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
        ball = Pusher.instance.currentBall;
        Rigidbody ballRigidbody = ball.GetComponent<Rigidbody>();
        yield return new WaitForSeconds(.5f);

        //bool isAerialShot = ballAt.y < ball.transform.position.y;

        bool isAerialShot = ballRigidbody.velocity.y<0;

        int steps = 50;

        Vector3 landingPosition = isAerialShot ? PredictLandingPosition(ballRigidbody, steps) : PredictBestTravelPoint(ballRigidbody,30,20);

        marker.transform.position = landingPosition;

        float bestScore = float.MinValue;
        bestFielder = null;

        foreach (var fielder in fielders)
        {
            bool isDeep = fielder.CompareTag("DeepFielder");

            Vector3 ballDir = ball.position - ballAt;
            ballAngle = Mathf.Atan2(ballDir.z, ballDir.x) * Mathf.Rad2Deg;

            Vector3 fielderDir = fielder.transform.position - Pusher.instance.batCenter.position;
            fielderAngle = Mathf.Atan2(fielderDir.z, fielderDir.x) * Mathf.Rad2Deg;

            float directionBoost = -1 * Mathf.Abs(fielderAngle - ballAngle);

            Vector3 toLandingPosition = landingPosition - fielder.transform.position;
            float distanceToLanding = toLandingPosition.magnitude;

            
            float timeToIntercept = distanceToLanding / runSpeed;

            float score = 0f;

            boostDeep = ballRigidbody.velocity.magnitude > speedThreshold;

            if(ballRigidbody.velocity.magnitude<slowThreshold && !isDeep)
            {
                score += 6;
            }

            if (boostDeep && isDeep)
            {
                if(isAerialShot)
                {
                    score += 3;
                }
                score += 4;
            }

            score += directionBoost;

            if (score > bestScore)
            {
                bestScore = score;
                bestFielder = fielder;
            }
        }

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

    Vector3 PredictBestTravelPoint(Rigidbody ballRb, int steps, float forwardDistance = 10f)
    {
        Vector3 position = ballRb.position;
        Vector3 velocity = ballRb.velocity;
        Vector3 gravity = Physics.gravity;
        Vector3 targetPosition = position;

        for (int i = 0; i < steps; i++)
        {
            float deltaTime = Time.fixedDeltaTime;

            // Simulate the position and velocity update
            position += velocity * deltaTime;
            velocity += gravity * deltaTime;

            // Check if it’s going to land soon (for aerial shots)
            if (position.y <= -4.221f || !Pusher.instance.stadiumBounds.Contains(position))
            {
                // Assume we hit the ground or are out of bounds
                targetPosition = new Vector3(position.x, -4.221f, position.z);
                break;
            }

            // If we reach the forwardDistance along the path, break early for grounded shots
            if (Vector3.Distance(ballRb.position, position) >= forwardDistance)
            {
                targetPosition = position;
                break;
            }
        }
        return new Vector3(targetPosition.x, fielderY, targetPosition.z); // Ensure target Y position aligns with fielder’s plane
    }


    void HandleGroundedShot(Vector3 ballAt)
    {
        Vector3 ballDirection = (ball.position - ballAt).normalized;
        Vector3 fielderToBall = ball.position - bestFielder.transform.position;
        float angleToBall = Vector3.Angle(ballDirection, fielderToBall);

        // Check if the ball is coming directly at the fielder and within range
        if (angleToBall < 10f && fielderToBall.magnitude < fieldingRange)
        {
            Debug.Log("Grounded ball coming straight at fielder. Fielder stops the ball.");
            StopBall();
        }
        else
        {
            // Calculate intercept point and immediately set fielder to move
            Vector3 interceptPoint = FindBestIntersectionPoint(ball.GetComponent<Rigidbody>(), bestFielder.transform.position);

            // Start coroutine to move towards target, considering ground check
            fielderCoroutine = StartCoroutine(MoveToTargetWithGroundCheck(bestFielder, interceptPoint));
        }
    }

    void HandleAerialShot(Vector3 landingPosition)
    {
        if (Vector3.Distance(new Vector2(landingPosition.x, landingPosition.z), new Vector2(bestFielder.transform.position.x, bestFielder.transform.position.z)) < 5)
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
        //while (Vector2.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z), new Vector2(ball.transform.position.x, ball.transform.position.z)) > fieldingRange)
        while (Vector2.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) > fieldingRange)
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
            while (ball.transform.position.y > fielderY)
            {
                fielder.transform.position = Vector3.MoveTowards(fielder.transform.position, new Vector3(ball.transform.position.x, fielder.transform.position.y, ball.transform.position.z), runSpeed * Time.deltaTime);
                yield return null;
            }
            yield return null;
            ball.GetComponent<Rigidbody>().isKinematic = true;
            if(!Pusher.instance.deliveryDead)
                Pusher.instance.Out();
        }

        ball.GetComponent<Rigidbody>().isKinematic = true;
        Pusher.instance.deliveryDead = true;
    }

    Vector3 AdjustForGroundedBall(Vector3 ballPosition, Vector3 fielderPosition)
    {
        // Calculate a dynamic intercept point based on the ball's direction and fielding range
        Vector3 directionToBall = (ballPosition - fielderPosition).normalized;
        float distanceAdjustment = Mathf.Max(fieldingRange - 1f, 1f); // Maintain some distance based on fielding range

        Vector3 adjustedPosition = ballPosition - directionToBall * distanceAdjustment;
        return new Vector3(adjustedPosition.x, fielderY, adjustedPosition.z); // Set to fielder's height (fielderY)
    }

    [SerializeField] float fielderY = 0.507754f;

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
