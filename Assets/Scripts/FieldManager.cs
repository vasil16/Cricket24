using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public List<Fielder> fielders;
    public Transform ball;
    public float fieldingRange = 1.5f;

    public List<Fielder> bestFielders = new List<Fielder>();
    public static Action<Vector3> StartCheckField;
    public static Action ResetFielder;

    public float score;
    public Transform marker;

    private void Start()
    {
        StartCheckField = AssignBestFielders;
        ResetFielder = ResetFielders;
    }

    public void AssignBestFielders(Vector3 ballAt)
    {
        StartCoroutine(AssignFielders(ballAt));
    }

    Vector3 landPos;


    IEnumerator AssignFielders(Vector3 ballAt)
    {
        yield return new WaitForSeconds(0.4f);

        int fielderCount = 0;

        Debug.Log("Assigning fielders...");
        ball = Gameplay.instance.currentBall;
        List<Fielder> selectedFielders = new List<Fielder>();

        if (ball.GetComponent<BallHit>().groundShot)
        {
            landPos = BallStopPos(ball.GetComponent<Rigidbody>());
            if (ball.GetComponent<Rigidbody>().velocity.magnitude > 38)
            {
                fielderCount = 3;
            }
            else
            {
                fielderCount = 2;
            }
        }
        else
        {
            landPos = PredictLandingPosition(ball);
            fielderCount = 2;
            Debug.Log("Air ball detected");
        }

        marker.transform.position = landPos;

        // Score fielders and find the best three
        List<(Fielder fielder, float score)> scoredFielders = new List<(Fielder, float)>();

        foreach (var fielder in fielders)
        {
            float fielderScore = CalculateFielderScore(fielder);
            scoredFielders.Add((fielder, fielderScore));
        }

        // Sort fielders by score (descending order)
        scoredFielders.Sort((a, b) => b.score.CompareTo(a.score));

        // Select the top three fielders
        foreach (var (fielder, _) in scoredFielders)
        {
            if (ball.GetComponent<BallHit>().groundShot)
            {
                landPos = BallIntersectionPoint(ball.GetComponent<Rigidbody>(), fielder.transform);
            }
            if (selectedFielders.Count < fielderCount)
            {
                if (ball.GetComponent<BallHit>().groundShot)
                {
                    if (fielder.CompareTag("DeepFielder") && ball.GetComponent<Rigidbody>().velocity.magnitude > 38)
                    {
                        if (!selectedFielders.Contains(fielder))
                        {
                            selectedFielders.Add(fielder);
                            Debug.Log("Deep fielder added: " + fielder.name);
                        }
                    }
                }

                if (!selectedFielders.Contains(fielder))
                {
                    selectedFielders.Add(fielder);
                    Debug.Log("Fielder added: " + fielder.name);
                }
            }
        }

        // Assign tasks to the selected fielders
        foreach (var fielder in selectedFielders)
        {
            if (!fielder.startedRun)
            {
                fielder.StartField(landPos, ball);
                fielder.enabled = true;
                fielder.startedRun = true;
            }
        }

        bestFielders = selectedFielders;
    }

    private float CalculateFielderScore(Fielder fielder)
    {
        float score = 0;
        // Calculate ball trajectory angle
        Vector3 ballDirection = ball.position - Gameplay.instance.center.position;
        float ballAngle = Mathf.Atan2(ballDirection.z, ballDirection.x) * Mathf.Rad2Deg;

        // Calculate fielder direction angle
        Vector3 fielderDirection = fielder.transform.position - Gameplay.instance.center.position;
        float fielderAngle = Mathf.Atan2(fielderDirection.z, fielderDirection.x) * Mathf.Rad2Deg;

        // Calculate angle difference
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(ballAngle, fielderAngle));

        // Direction-based score: Lower angle difference means better positioning
        float directionScore = -angleDifference; // Negative because lower is better

        // Add additional scoring based on shot type
        //if (ball.GetComponent<BallHit>().groundShot)
        {
            if (fielder.CompareTag("DeepFielder") && ball.GetComponent<Rigidbody>().velocity.magnitude > 38)
            {
                // Boost score for deep fielders if the shot is fast
                score += 1000;
            }
        }
        //else
        //{
        //    // Air ball: Calculate distance to predicted landing position
        //    Vector3 toLanding = landPos - fielder.transform.position;
        //    float distanceToLanding = toLanding.magnitude;
        //    score = 10000;
        //    score -= distanceToLanding; // Closer fielders score higher
        //}
        score += directionScore;
        return score;
    }

    private Vector3 BallIntersectionPoint(Rigidbody ballRb, Transform fielder)
    {
        Vector3 ballPosition = ballRb.transform.position;
        Vector3 ballVelocity = ballRb.velocity;
        float fielderSpeed = 19.7f; // Adjust based on the fielder's speed
        Vector3 fielderPosition = fielder.position;

        float timeStep = 0.1f; // Simulation time step
        float maxTime = 5.0f;  // Maximum simulation time
        Vector3 gravity = Physics.gravity;

        Vector3 closestPoint = ballPosition; // Default to current ball position
        float closestDistance = float.MaxValue;

        for (float t = 0; t <= maxTime; t += timeStep)
        {
            // Simulate ball's position at time t
            Vector3 ballFuturePosition = ballPosition + ballVelocity * t + 0.5f * gravity * t * t;

            // Simulate fielder's position at time t (assuming they run directly toward the ball)
            Vector3 directionToBall = (ballFuturePosition - fielderPosition).normalized;
            Vector3 fielderFuturePosition = fielderPosition + directionToBall * fielderSpeed * t;

            // Calculate distance between ball and fielder at time t
            float distance = Vector2.Distance(
    new Vector2(ballFuturePosition.x, ballFuturePosition.z),
    new Vector2(fielderFuturePosition.x, fielderFuturePosition.z)
);


            // Check if this is the closest point so far
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = ballFuturePosition;
            }

            // Stop if the fielder reaches the ball
            if (distance < 0.5f) // Threshold for "reached the ball"
            {
                break;
            }
        }

        closestPoint.y = fielder.position.y; // Keep the intersection point at the fielder's height
        return closestPoint;
    }


    private Vector3 BallStopPos(Rigidbody ballRb)
    {
        Vector3 initialPosition = ballRb.transform.position;
        Vector3 initialVelocity = ballRb.velocity;
        float timeToStop = -initialVelocity.magnitude / Physics.gravity.y;

        Vector3 finalPosition = initialPosition + (initialVelocity * timeToStop) + (0.5f * Physics.gravity * timeToStop * timeToStop);
        finalPosition.y = fielders[0].transform.position.y;

        return finalPosition;
    }

    private Vector3 PredictLandingPosition(Transform ball)
    {
        BallHit ballComp = ball.GetComponent<BallHit>();
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        Vector3 landingPosition = Vector3.zero;

        if (ballRb == null)
        {
            Debug.LogError("Ball Rigidbody is missing.");
            return landingPosition;
        }

        Vector3 ballVelocity = ballRb.velocity;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float heightDifference = ball.position.y - (-4.437081f);

        if (heightDifference <= 0)
        {
            landingPosition = ball.position;
            landingPosition.y = fielders[0].transform.position.y;
            return landingPosition;
        }

        float verticalVelocity = ballVelocity.y;

        float a = -0.5f * gravity;
        float b = verticalVelocity;
        float c = heightDifference;

        float discriminant = (b * b) - (4 * a * c);

        if (discriminant < 0)
        {
            float timeToGround = heightDifference / Mathf.Abs(verticalVelocity);
            if (!float.IsNaN(timeToGround) && timeToGround > 0)
            {
                Vector3 horVelocity = new Vector3(ballVelocity.x, 0, ballVelocity.z);
                landingPosition = ball.position + horVelocity * timeToGround;
                landingPosition.y = fielders[0].transform.position.y;
                return landingPosition;
            }
            return Vector3.zero;
        }

        float timeToLand = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        if (timeToLand < 0)
        {
            timeToLand = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        }

        if (float.IsNaN(timeToLand) || timeToLand <= 0)
        {
            return Vector3.zero;
        }

        Vector3 horizontalVelocity = new Vector3(ballVelocity.x, 0, ballVelocity.z);
        Vector3 horizontalDisplacement = horizontalVelocity * timeToLand;

        landingPosition = ball.position + horizontalDisplacement;
        landingPosition.y = fielders[0].transform.position.y;

        Debug.DrawLine(ball.position, landingPosition, Color.green);
        marker.position = landingPosition;
        return landingPosition;
    }

    public void ResetFielders()
    {
        foreach (var fielder in bestFielders)
        {
            fielder.Reset();
            fielder.startedRun = false;
            fielder.enabled = false;
        }
        bestFielders.Clear();
    }
}
