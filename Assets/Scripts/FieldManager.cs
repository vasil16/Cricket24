using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public static Action<Vector3, Vector3> StartCheckField;
    public static Action ResetFielder;

    [SerializeField] private List<GameObject> fielders;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float fieldingRange = 1.5f;
    [SerializeField] private float deepFielderBoost = 0.8f; // Boost score for optimal deep fielders
    private Coroutine fielderCoroutine;

    private Vector3 initialFielderPosition;
    [SerializeField] private GameObject bestFielder, marker;

    Transform ball;
    public float ballAngle, fielderAngle;

    private void Start()
    {
        StartCheckField = CheckAndInitiateFielder;
        ResetFielder = ResetField;
    }


    void CheckAndInitiateFielder(Vector3 ballAt, Vector3 direction)
    {
        ball = Pusher.instance.currentBall;
        Debug.Log("Initiating fielder selection...");

        Rigidbody ballRigidbody = ball.GetComponent<Rigidbody>();

        // Predict ball landing position and time it will take to reach it
        Vector3 landingPosition = PredictLandingPosition(ballRigidbody,10f, 30);
        float timeToReachLanding = EstimateTimeToReach(ballRigidbody, landingPosition);
        

        float bestScore = float.MinValue;
        bestFielder = null;

        foreach (var fielder in fielders)
        {
            float fielderReachTime = 20/ Vector2.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z), new Vector2(landingPosition.x, landingPosition.z));

            Vector3 ballDir = ball.position - ballAt;
            ballAngle = Mathf.Atan2(ballDir.z, ballDir.x) * Mathf.Rad2Deg;

            Vector3 fielderDire = fielder.transform.position - Pusher.instance.batCenter.position;
            fielderAngle = Mathf.Atan2(fielderDire.z, fielderDire.x) * Mathf.Rad2Deg;

            float directionBoost = -1 * Mathf.Abs(fielderAngle - ballAngle);

            Vector3 toLandingPosition = landingPosition - fielder.transform.position;
            float distanceToLanding = toLandingPosition.magnitude;

            // Calculate estimated time for fielder to reach the landing position
            float timeToIntercept = distanceToLanding / runSpeed;

            bool canReachInTime = timeToIntercept < timeToReachLanding;

            // Initial scoring, prioritizing inner fielders
            float score = 0f;
            if (distanceToLanding <= 100f && canReachInTime) // Inner fielder preferred
            {
                score = 1.5f / timeToIntercept; // Higher score for quicker reach
            }
            else if (distanceToLanding > 100f && canReachInTime) // Deep fielder as fallback
            {
                score = 1.0f / timeToIntercept + deepFielderBoost;
            }
            score += directionBoost;
            score += fielderReachTime;
            // Update the best fielder based on score
            if (score > bestScore)
            {
                bestScore = score;
                bestFielder = fielder;
            }
        }

        if (bestFielder != null)
        {
            initialFielderPosition = bestFielder.transform.position;
            landingPosition.y = bestFielder.transform.position.y; // Keep target at fielder's ground level
            marker.transform.position = landingPosition;

            fielderCoroutine = StartCoroutine(MoveToTarget(bestFielder, ball.gameObject, landingPosition));
            Debug.Log($"Chosen fielder: {bestFielder.name} with score {bestScore}");
        }
        else
        {
            Debug.Log("No fielder able to reach the target position in time.");
        }
    }

    float EstimateTimeToReach(Rigidbody ball, Vector3 targetPosition)
    {
        Vector3 initialPosition = ball.transform.position;
        Vector3 direction = (targetPosition - initialPosition).normalized;
        float distance = Vector3.Distance(initialPosition, targetPosition);
        float speed = ball.velocity.magnitude;

        return distance / speed; // Approximate time based on current speed and distance
    }

    IEnumerator MoveToTarget(GameObject fielder, GameObject ball, Vector3 predLand)
    {
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        BallHit ballHit;

        while (true)
        {
            Vector3 ballPosition = ball.transform.position;
            Vector3 fielderPosition = fielder.transform.position;

            // Calculate horizontal distance to ignore height difference
            float horizontalDistanceToBall = Vector2.Distance(
                new Vector2(fielderPosition.x, fielderPosition.z),
                new Vector2(ballPosition.x, ballPosition.z)
            );
            ballHit = ball.GetComponent<BallHit>();
            // Adjust the target position to stay slightly ahead of the ball, if necessary
            Vector3 targetPosition = predLand;

            //Vector3 targetPosition = ballPosition - directionToBall * Mathf.Min(horizontalDistanceToBall, 5f);

            targetPosition.y = fielderPosition.y; // Keep fielder's y position constant

            // Move the fielder towards the updated target position
            fielder.transform.position = Vector3.MoveTowards(fielderPosition, targetPosition, runSpeed * Time.deltaTime);

            // Update stop conditions
            bool isCloseEnoughHorizontally = ballHit.groundShot && horizontalDistanceToBall < 2.5f;
            bool airClose = !ballHit.groundShot && isCloseEnoughHorizontally && ballPosition.y <= 1.6f;
            // Stop if close enough or if it's a ground shot and close enough
            if (airClose || isCloseEnoughHorizontally)
            {
                //Debug.Log("horizontal close "+);
                break;
            }

            yield return null;
        }

        // Fielder has reached the ball; handle the "out" or retrieval actions
        Debug.Log("Fielder reached the ball!");

        if (!ballHit.groundShot)
        {
            Pusher.instance.Out();
        }
        else
        {
            Pusher.instance.deliveryDead = true;
        }

        ballRb.isKinematic = true;
    }


    Vector3 PredictLandingPosition(Rigidbody ballRb, float simulationTime, int steps)
    {
        Vector3 initialPosition = ballRb.position;
        Vector3 velocity = ballRb.velocity;
        Vector3 gravity = Physics.gravity;
        Vector3 predictedPosition = initialPosition;

        float stepTime = simulationTime / steps; // Calculate time increment per step

        for (int i = 0; i < steps; i++)
        {
            // Update the predicted position based on velocity
            predictedPosition += velocity * stepTime;

            // Update the velocity considering gravity
            velocity += gravity * stepTime;

            // Check for ground hit or out-of-bounds
            if (predictedPosition.y <= -4.221f || !Pusher.instance.stadiumBounds.Contains(predictedPosition))
            {
                // Adjust position to ground level if it hits the ground
                if (predictedPosition.y <= -4.221f)
                {
                    predictedPosition.y = -4.221f;
                }
                // Log the reason for breaking the loop
                Debug.Log($"Break: Ball {(predictedPosition.y <= -4.221f ? "hit the ground" : "went out of bounds")} at position: {predictedPosition}");
                break; // Exit the loop if the ball hits the ground or goes out of bounds
            }
        }

        return predictedPosition;
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
