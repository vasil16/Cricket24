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

    public static Vector3 hitBallPos, hitVelocity;

    Vector3 landPos;
    public float score;
    public Transform marker, keeper;
    [SerializeField] MeshRenderer ignoreBounds;

    private void Start()
    {
        StartCheckField = AssignBestFielders;
        ResetFielder = ResetFielders;
    }

    public void AssignBestFielders(Vector3 ballAt)
    {
        ball = Gameplay.instance.currentBall;
        if(ball.GetComponent<BallHit>().secondTouch)
        {
            Debug.Log("fielder");
            StartCoroutine(AssignFielders(ballAt));
        }
        else
        {
            Debug.Log("keepr");
            StartCoroutine(KeeperReceive());
        }
    }

    IEnumerator KeeperReceive()
    {
        keeper.GetComponent<Fielder>().enabled = true;
        keeper.GetComponent<Fielder>().ball = ball;
        while (ball.GetComponent<BallHit>().keeperReceive==false)
        {
            keeper.transform.position = new Vector3(keeper.transform.position.x, keeper.transform.position.y, Mathf.MoveTowards(keeper.position.z, ball.position.z, 20*Time.deltaTime));
            yield return null;
        }
        ball.GetComponent<Rigidbody>().isKinematic = true;
        Gameplay.instance.deliveryDead = true;
        yield return new WaitForSeconds(4);
        keeper.GetComponent<Fielder>().Reset();
    }

    IEnumerator AssignFielders(Vector3 ballAt)
    {
        
        hitBallPos = ballAt;
        hitVelocity = ball.GetComponent<Rigidbody>().velocity;

        int fielderCount = 1;

        if (!ball.GetComponent<BallHit>().groundShot)
        {
            landPos = PredictLandingPosition(ball);
            fielderCount = 2;
            Debug.Log("Air ball detected");
        }

        yield return new WaitForSeconds(0.4f);

        Debug.Log("Assigning fielders...");
        List<Fielder> selectedFielders = new List<Fielder>();

        if (ball.GetComponent<BallHit>().groundShot)
        {
            landPos = BallStopPos(ball.GetComponent<Rigidbody>());
            if (ball.GetComponent<Rigidbody>().velocity.magnitude > 68)
            {
                fielderCount = 3;
            }
            else
            {
                fielderCount = 2;
            }
        }        

        marker.transform.position = landPos;

        List<(Fielder fielder, float score)> scoredFielders = new List<(Fielder, float)>();

        foreach (var fielder in fielders)
        {
            float fielderScore = CalculateFielderScore(fielder);
            scoredFielders.Add((fielder, fielderScore));
        }

        scoredFielders.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (var (fielder, _) in scoredFielders)
        {
            if (selectedFielders.Count < fielderCount)
            {
                if (ball.GetComponent<BallHit>().groundShot)
                {
                    if (fielder.CompareTag("DeepFielder") && ball.GetComponent<Rigidbody>().velocity.magnitude > 50)
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

        foreach (var fielder in selectedFielders)
        {
            if (!fielder.startedRun)
            {
                fielder.enabled = true;
                fielder.startedRun = true;
                fielder.Initiate(landPos, ball);
            }
        }

        bestFielders = selectedFielders;
    }

    private float CalculateFielderScore(Fielder fielder)
    {
        float score = 0;

        Vector3 ballDirection = ball.position - Gameplay.instance.center.position;
        float ballAngle = Mathf.Atan2(ballDirection.z, ballDirection.x) * Mathf.Rad2Deg;


        Vector3 fielderDirection = fielder.transform.position - Gameplay.instance.center.position;
        float fielderAngle = Mathf.Atan2(fielderDirection.z, fielderDirection.x) * Mathf.Rad2Deg;


        float angleDifference = Mathf.Abs(ballAngle-fielderAngle);

        // Direction-based score: Lower angle difference means better positioning
        float directionScore = -angleDifference; // Negative because lower is better

        Debug.Log("ball speed " + ball.GetComponent<Rigidbody>().velocity.magnitude);
        score += directionScore;
        
        if (ball.GetComponent<Rigidbody>().velocity.magnitude > 70 && ball.GetComponent<BallHit>().groundShot)
        {
            if (fielder.CompareTag("DeepFielder"))
            {
                // Boost score for deep fielders if the shot is fast
                score += 50;
            }
        }

        //if (!ball.GetComponent<BallHit>().groundShot && !ignoreBounds.bounds.Contains(landPos))
        //{
        //    Vector3 toLanding = landPos - fielder.transform.position;
        //    float distanceToLanding = toLanding.magnitude;
        //    score -= distanceToLanding;
        //}
        
        fielder.score = score;
        fielder.angleDiff = angleDifference;
        return score;
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

        Debug.DrawLine(Gameplay.instance.center.position, landingPosition, Color.green);
        marker.position = landingPosition;
        return landingPosition;
    }

    public void ResetFielders()
    {
        foreach (var fielder in bestFielders)
        {
            fielder.Reset();
            fielder.startedRun = false;
            //fielder.enabled = false;
        }
        bestFielders.Clear();
    }
}
