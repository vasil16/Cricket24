using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public static Action<Vector3, Vector3> StartCheckField;
    public static Action ResetFielder;
    [SerializeField] private List<GameObject> fielders;
    [SerializeField] private float runSpeed;
    [SerializeField] private float fieldingRange = 10f;
    [SerializeField] private float deepFielderBoost = 0.8f; // Boost score for optimal deep fielders
    //[SerializeField] private float circledistance;
    private Coroutine moverCoroutine;

    private Vector3 initialFielderPosition;
    [SerializeField] GameObject bestFielder, marker;

    private void Start()
    {
        StartCheckField = CheckAndInitiateFielder;
        ResetFielder = ResetField;
    }

    void CheckAndInitiateFielder(Vector3 ballAt, Vector3 at)
    {
        Debug.Log("Checking fielders...");

        Vector3 ballDirection = Pusher.instance.currentBall.GetComponent<Rigidbody>().velocity.normalized; // Accurate direction based on velocity
        GameObject ball = Pusher.instance.currentBall.gameObject;
        float bestScore = -1f;
        bestFielder = null;

        foreach (var fielder in fielders)
        {
            Vector3 toFielder = (fielder.transform.position - ballAt);
            float distance = toFielder.magnitude;
            toFielder.Normalize();

            // Check direction accuracy - positive dot value means the fielder is closer to the ball's direction
            float dotProduct = Vector3.Dot(toFielder, ballDirection);

            // Set a base score
            float score = dotProduct;

            // Prioritize fielders in the right general direction, discourage fielders positioned behind
            if (dotProduct < 0)
            {
                score -= 1f; // or some larger penalty for being in the opposite direction
            }
            else
            {
                // Distance penalty
                score -= distance / fieldingRange;

                // Boost deep fielders who are in the correct direction
                if (distance > 110f)
                {
                    score += deepFielderBoost;
                }
            }

            // Track the best fielder
            if (score > bestScore)
            {
                bestScore = score;
                bestFielder = fielder;
            }
        }


        if (bestFielder != null)
        {
            //if(!ball.GetComponent<BallHit>().groundShot)
            //{
            //    Vector3 landingPos = PredictLandingPosition(ball.GetComponent<Rigidbody>(), 20f, 20);
            //    landingPos.y = bestFielder.transform.position.y;
            //    marker.transform.position = landingPos;
            //    initialFielderPosition = bestFielder.transform.position;
            //    moverCoroutine = StartCoroutine(RunToLanding(bestFielder, Pusher.instance.currentBall.transform, landingPos));
            //}
            //else
            {
                Vector3 landingPos = fallPos(ballAt,ball.transform);
                landingPos.y = bestFielder.transform.position.y;
                marker.transform.position = landingPos;
                initialFielderPosition = bestFielder.transform.position;
                moverCoroutine = StartCoroutine(RunToBall(bestFielder, Pusher.instance.currentBall.transform, ballAt));
                Debug.Log($"Selected Fielder: {bestFielder.name} with score {bestScore}");
            }
        }

        else
        {
            Debug.Log("No suitable fielder found within range.");
        }
    }

    IEnumerator RunToLanding(GameObject fielder, Transform ball, Vector3 predictedSpot)
    {
        Debug.Log("Starting fielder movement...");
        Vector3 predictedPosition;

        predictedPosition = predictedSpot;

        while (Vector2.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z), new Vector2(predictedSpot.x, predictedSpot.z)) > 1f)
        {
            if(Vector2.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z), new Vector2(ball.position.x, ball.position.z)) < 2f)
            {
                break;
            }            

            fielder.transform.position = Vector3.MoveTowards(fielder.transform.position, predictedPosition, runSpeed * Time.deltaTime);

            yield return null;
        }

        Debug.Log("Fielder reached the target position!");

        if (!ball.GetComponent<BallHit>().groundShot)
        {
            Pusher.instance.Out();
        }
        else
        {
            fielder.transform.position = initialFielderPosition;
            Pusher.instance.deliveryDead = true;
        }

        ball.GetComponent<Rigidbody>().isKinematic = true;
    }

    IEnumerator RunToBall(GameObject fielder, Transform ball, Vector3 ballAt)
    {

        Debug.Log("Starting fielder movement...");
        

        while (Vector2.Distance(new Vector2(fielder.transform.position.x, fielder.transform.position.z), new Vector2(ball.position.x, ball.position.z)) > 2f ||
            (!ball.GetComponent<BallHit>().groundShot && ball.position.y > 0.22f))
        {
            Vector3 ballDirection = ball.transform.position - ballAt;
            Vector3 predictedPosition;
            if (ball.GetComponent<BallHit>().groundShot)
            {
                predictedPosition = new Vector3(ball.position.x, fielder.transform.position.y, ball.position.z);
            }
            else
            {
                predictedPosition = ball.position + ballDirection * 2f;
                predictedPosition.y = fielder.transform.position.y;
            }

            fielder.transform.position = Vector3.MoveTowards(fielder.transform.position, predictedPosition, runSpeed * Time.deltaTime);

            yield return null;
        }

        Debug.Log("Fielder reached the target position!");

        if (!ball.GetComponent<BallHit>().groundShot)
        {
            Pusher.instance.Out();
        }
        else
        {
            fielder.transform.position = initialFielderPosition;
            Pusher.instance.deliveryDead = true;
        }

        ball.GetComponent<Rigidbody>().isKinematic = true;
    }

    Vector3 fallPos(Vector3 ballAt, Transform ball)
    {
        return ball.position - ballAt.normalized * 3;
    }

    Vector3 PredictLandingPosition(Rigidbody ballRb, float timeToSimulate, int steps)
    {
        Vector3 initialPosition = ballRb.position; // Current position of the ball
        Vector3 velocity = ballRb.velocity; // Current velocity of the ball
        Vector3 gravity = Physics.gravity; // Gravity vector

        // Simulate the trajectory
        Vector3 predictedPosition = initialPosition;

        for (int i = 0; i < steps; i++)
        {
            // Update position based on current velocity
            predictedPosition += velocity * (timeToSimulate / steps);

            // Update velocity based on gravity
            velocity += gravity * (timeToSimulate / steps);

            // Check if predicted position is below ground level (assuming ground is at y = 0)
            if (predictedPosition.y <= -4.437081f || !Pusher.instance.stadiumBounds.Contains(predictedPosition))
            {
                predictedPosition.y = -4.437081f; // Ground level
                break; // Exit the loop if we've hit the ground
            }
        }

        return predictedPosition;
    }


    void ResetField()
    {
        if (moverCoroutine != null)
            StopCoroutine(moverCoroutine);

        if (bestFielder != null)
        {
            bestFielder.transform.position = initialFielderPosition;
            Debug.Log("Fielder position reset.");
        }
    }
}
