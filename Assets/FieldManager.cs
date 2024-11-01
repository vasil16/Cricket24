using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public static Action<Vector3, Vector3> StartCheckField;
    public static Action ResetFielder;
    [SerializeField] private List<GameObject> fielders;
    [SerializeField] private float runSpeed;
    [SerializeField] private float fieldingRange = 10f;
    private Coroutine moverCoroutine;

    private Vector3 initialFielderPosition;
    private GameObject bestFielder;

    private void Start()
    {
        StartCheckField = CheckAndInitiateFielder;
        ResetFielder = ResetField;
    }

    void CheckAndInitiateFielder(Vector3 ballAt, Vector3 ballDirection)
    {
        Debug.Log("Checking fielders...");
        float bestScore = -1f;
        bestFielder = null;

        foreach (var fielder in fielders)
        {
            Vector3 toFielder = (fielder.transform.position - ballAt).normalized;
            float dotProduct = Vector3.Dot(toFielder, ballDirection.normalized);
            float distance = Vector3.Distance(fielder.transform.position, ballAt);

            // Define if fielder is inside or outside the 30-yard circle
            bool isInsideCircle = distance <= 180f; // Adjust as necessary for Unity's units

            // Calculate score: prioritize inner fielders if direction and distance are similar
            float score = dotProduct - (distance / fieldingRange);
            if (isInsideCircle) score += 0.5f; // Boost inner fielder score for closer fielding

            // Select the fielder with the highest score, considering circle positioning
            if (score > bestScore)
            {
                bestScore = score;
                bestFielder = fielder;
            }
        }

        if (bestFielder != null)
        {
            initialFielderPosition = bestFielder.transform.position;
            moverCoroutine = StartCoroutine(RunToBall(bestFielder, Pusher.instance.currentBall));
            Debug.Log($"Selected Fielder: {bestFielder.name} with score {bestScore}");
        }
        else
        {
            Debug.Log("No suitable fielder found within range.");
        }
    }


    IEnumerator RunToBall(GameObject fielder, Transform ball)
    {
        Debug.Log("Starting fielder movement...");

        // Ensure the ball was hit
        if (!ball.GetComponent<BallHit>().secondTouch)
        {
            Pusher.instance.deliveryDead = true;
            Debug.Log("No ball hit detected.");
            yield break;
        }


        // Continue running until the fielder reaches the ball
        while (Vector2.Distance(
            new Vector2(fielder.transform.position.x, fielder.transform.position.z),
            new Vector2(ball.position.x, ball.position.z)) > 0.1f ||
            (!ball.GetComponent<BallHit>().groundShot && (ball.position.y) > 0.22f))
        {
            Vector3 ballTargetPos = new Vector3(ball.position.x, fielder.transform.position.y, ball.position.z);

            // Move the fielder toward the ball’s current position
            fielder.transform.position = Vector3.MoveTowards(fielder.transform.position, ballTargetPos, runSpeed * Time.deltaTime);

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

        // Stop ball movement
        ball.GetComponent<Rigidbody>().isKinematic = true;
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
