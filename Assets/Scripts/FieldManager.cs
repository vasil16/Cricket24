using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public List<Fielder> fielders;
    public Transform ball;
    public float fieldingRange = 1.5f;

    public static Fielder bestFielder;

    public static Action<Vector3> StartCheckField;
    public static Action  ResetFielder;

    public float score;

    public Fielder assignedFielder;

    public Transform marker;

    private void Start()
    {
        StartCheckField = AssignBestFielder;
        ResetFielder = ResetField;
    }

    public void AssignBestFielder(Vector3 ballAt)
    {
        StartCoroutine(Assign(ballAt));
    }

    Vector3 landPos;

    IEnumerator Assign(Vector3 ballAt)
    {
        yield return new WaitForSeconds(0.4f);
        Debug.Log("called");
        ball = Pusher.instance.currentBall;
        float bestScore = -1000;
        Fielder selectedFielder = null;

        if (!ball.GetComponent<BallHit>().groundShot)
            landPos =  marker.position = PredictLandingPosition(ball);

        foreach (var fielder in fielders)
        {
            score = 0;

            bool isDeep = fielder.CompareTag("DeepFielder");

            if (!ball.GetComponent<BallHit>().groundShot)
            {
                Vector3 toLanding = landPos - fielder.transform.position;
                float distanceToLanding = toLanding.magnitude;
                score -= distanceToLanding;
            }

            else
            {
                if(isDeep && ball.GetComponent<Rigidbody>().velocity.magnitude>60)
                {
                    score += 5;
                }
            }

            Vector3 toBall = ball.position - fielder.transform.position;
            float distanceToBall = toBall.magnitude;

            


            Vector3 ballDir = ball.position - ballAt;
            float ballAngle = Mathf.Atan2(ballDir.z, ballDir.x) * Mathf.Rad2Deg;

            Vector3 fielderDir = fielder.transform.position - Pusher.instance.batCenter.position;
            float fielderAngle = Mathf.Atan2(fielderDir.z, fielderDir.x) * Mathf.Rad2Deg;


            float directionBoost = -1 * Mathf.Abs(fielderAngle - ballAngle);

            //if (distanceToBall <= fieldingRange)
            {
                //score = fieldingRange - distanceToBall;

                score += directionBoost;
                if (score > bestScore)
                {
                    bestScore = score;
                    selectedFielder = fielder;
                }
                selectedFielder.score = score;
            }
        }

        if (selectedFielder != null && selectedFielder != bestFielder)
        {
            bestFielder = selectedFielder;
            if(!ball.GetComponent<BallHit>().groundShot)
            {
                bestFielder.StartField(landPos, ball);
            }
            else
                bestFielder.StartField(ball.position, ball);
            bestFielder.enabled = true;
            bestFielder.active = true;
            assignedFielder = bestFielder;
        }

        else
        {
            Debug.Log("no fielder");
        }
    }

    //private Vector3 PredictLandingPosition(Transform ball)
    //{
    //    BallHit ballComp = ball.GetComponent<BallHit>();
    //    Rigidbody ballRb = ball.GetComponent<Rigidbody>();
    //    Vector3 landingPosition = Vector3.zero;

    //    if (ballRb == null)
    //    {
    //        Debug.LogError("Ball Rigidbody is missing.");
    //        return landingPosition;
    //    }

    //    Vector3 ballVelocity = ballRb.velocity;
    //    float gravity = Mathf.Abs(Physics.gravity.y);

    //    // Airborne shot prediction using projectile motion
    //    float verticalVelocity = ballVelocity.y;
    //    float discriminant = verticalVelocity * verticalVelocity + 2 * gravity * ball.position.y;

    //    // Check if discriminant is non-negative
    //    if (discriminant < 0)
    //    {
    //        Debug.LogError("Invalid discriminant for airborne shot calculation.");
    //        return landingPosition; // Return zero vector if calculation fails
    //    }

    //    float timeToLand = (verticalVelocity + Mathf.Sqrt(discriminant)) / gravity;
    //    if (float.IsNaN(timeToLand) || timeToLand < 0) timeToLand = 0;

    //    // Calculate horizontal displacement over the flight time
    //    Vector3 horizontalVelocity = new Vector3(ballVelocity.x, 0, ballVelocity.z);
    //    Vector3 horizontalDisplacement = horizontalVelocity * timeToLand;

    //    landingPosition = ball.position + horizontalDisplacement;


    //    landingPosition.y = fielders[0].transform.position.y;  // Adjust y position to be level with fielders

    //    if (float.IsNaN(landingPosition.x) || float.IsNaN(landingPosition.z))
    //    {
    //        Debug.LogError("Landing position has invalid coordinates.");
    //        return Vector3.zero; // Return a default value if invalid
    //    }

    //    return landingPosition;
    //}

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

        // Calculate height difference to ground (fielder's height)
        float heightDifference = ball.position.y - fielders[0].transform.position.y;

        // Using quadratic equation from projectile motion:
        // y = y0 + v0y*t - (1/2)g*t^2
        // Where y is final height (fielder's height), y0 is initial height,
        // v0y is initial vertical velocity, g is gravity, and t is time

        float verticalVelocity = ballVelocity.y;

        // Calculate quadratic formula components
        // at^2 + bt + c = 0
        float a = -0.5f * gravity;
        float b = verticalVelocity;
        float c = heightDifference;

        float discriminant = (b * b) - (4 * a * c);

        // Debug values
        Debug.Log($"Ball Position: {ball.position}, Velocity: {ballVelocity}");
        Debug.Log($"Height Difference: {heightDifference}, Vertical Velocity: {verticalVelocity}");
        Debug.Log($"Discriminant: {discriminant}");

        if (discriminant < 0)
        {
            Debug.LogWarning($"Invalid discriminant ({discriminant}) for airborne shot calculation.");
            // If discriminant is negative, use simple linear prediction
            float simpleTime = heightDifference / Mathf.Abs(verticalVelocity);
            if (!float.IsNaN(simpleTime) && simpleTime > 0)
            {
                Vector3 horVelocity = new Vector3(ballVelocity.x, 0, ballVelocity.z);
                landingPosition = ball.position + horVelocity * simpleTime;
                landingPosition.y = fielders[0].transform.position.y;
                return landingPosition;
            }
            return Vector3.zero;
        }

        // Get the positive time solution (we want future, not past)
        float timeToLand = (-b + Mathf.Sqrt(discriminant)) / (2 * a);

        // If first solution gives negative time, try the other solution
        if (timeToLand < 0)
        {
            timeToLand = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        }

        Debug.Log($"Time to Land: {timeToLand}");

        if (float.IsNaN(timeToLand) || timeToLand < 0)
        {
            Debug.LogWarning($"Invalid time to land: {timeToLand}");
            return Vector3.zero;
        }

        // Calculate horizontal displacement
        Vector3 horizontalVelocity = new Vector3(ballVelocity.x, 0, ballVelocity.z);
        Vector3 horizontalDisplacement = horizontalVelocity * timeToLand;

        // Calculate final position
        landingPosition = ball.position + horizontalDisplacement;
        landingPosition.y = fielders[0].transform.position.y;

        // Validate final position
        if (float.IsNaN(landingPosition.x) || float.IsNaN(landingPosition.z))
        {
            Debug.LogError($"Invalid landing position calculated: {landingPosition}");
            return Vector3.zero;
        }

        // Optional: Visualize the prediction
        Debug.DrawLine(ball.position, landingPosition, Color.red, 0.1f);

        return landingPosition;
    }

    public void ResetField()
    {
        if(bestFielder)
        {
            bestFielder.Reset();
            bestFielder.active = false;
            bestFielder.enabled = false;
            bestFielder = null;
        }
    }
}
