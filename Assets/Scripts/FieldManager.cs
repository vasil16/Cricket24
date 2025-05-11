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
    public Transform marker, keeper, stumps;
    [SerializeField] MeshRenderer ignoreBounds;
    public bool ballWasAirBorne;

    private void Start()
    {
        StartCheckField = AssignBestFielders;
        ResetFielder = ResetFielders;
    }

    public void AssignBestFielders(Vector3 ballAt)
    {
        ball = Gameplay.instance.currentBall;
        ballWasAirBorne = ball.GetComponent<BallHit>().groundShot = false;
        Debug.Log("fielder");
        StartCoroutine(AssignFielders(ballAt));             
    }

    IEnumerator KeeperReceive()
    {
        //keeper.GetComponent<Animator>().enabled = true;
        //keeper.GetComponent<Fielder>().enabled = true;
        //keeper.GetComponent<Fielder>().ball = ball;
        //while (ball.GetComponent<BallHit>().keeperReceive == false)
        //{
        //    keeper.transform.position = new Vector3(keeper.transform.position.x, keeper.transform.position.y, Mathf.MoveTowards(keeper.position.z, ball.position.z, 20 * Time.deltaTime));
        //    yield return null;
        //}
        //while (ball.GetComponent<BallHit>().stopTriggered == false)
        //{
        //    if (ball.GetComponent<BallHit>().fielderReached) break;            
        //    keeper.GetComponent<Fielder>().KeeperRecieve();
            yield return null;
        //}
        //Gameplay.instance.deliveryDead = true;        
    }

    IEnumerator AssignFielders(Vector3 ballAt)
    {
        hitBallPos = ballAt;
        hitVelocity = ball.GetComponent<Rigidbody>().velocity;

        int fielderCount = 1;

        if (!ball.GetComponent<BallHit>().groundShot)
        {
            landPos = PredictLandingPosition(ball);
            marker.transform.position = landPos;
            fielderCount = 2;
            Debug.Log("Air ball detected");
        }

        yield return new WaitForSeconds(0.4f);

        Debug.Log("Assigning fielders...");
        List<Fielder> selectedFielders = new List<Fielder>();

        if (ball.GetComponent<BallHit>().groundShot)
        {
            if (ball.GetComponent<Rigidbody>().velocity.magnitude > 88)
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
                    //if (fielder.CompareTag("DeepFielder") && ball.GetComponent<Rigidbody>().velocity.magnitude > 88)
                    //{
                    //    if (!selectedFielders.Contains(fielder))
                    //    {
                    //        selectedFielders.Add(fielder);
                    //        Debug.Log("Deep fielder added: " + fielder.name);
                    //    }
                    //}
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
                fielder.GetComponent<Animator>().enabled = true;
                fielder.startedRun = true;
                fielder.Initiate(landPos,ball);
            }
        }
        bestFielders = selectedFielders;
        if (!bestFielders.Contains(fielders[0]))
        {
            StartCoroutine(KeeperRunToRecieve());
        }
    }

    IEnumerator KeeperRunToRecieve()
    {
        keeper.GetComponent<Animator>().enabled = true;
        keeper.GetComponent<Fielder>().enabled = true;
        keeper.GetComponent<Fielder>().ball = ball;
        keeper.GetComponent<Animator>().Play("running");

        while (Vector2.Distance(new Vector2(keeper.transform.position.x, keeper.transform.position.z),new Vector2(stumps.position.x, stumps.position.z))>2f)
        {
            Vector3 moveDirection = (ball.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            keeper.transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            keeper.transform.position = Vector3.MoveTowards(keeper.transform.position, new Vector3(stumps.position.x, keeper.transform.position.y, stumps.position.z), 28 * Time.deltaTime);
            yield return null;
        }
        keeper.GetComponent<Animator>().SetTrigger("StopField");
    }

    private float CalculateFielderScore(Fielder fielder)
    {
        float score = 0;

        Vector3 ballDirection = ball.position - Gameplay.instance.center.position;
        float ballAngle = Mathf.Atan2(ballDirection.z, ballDirection.x) * Mathf.Rad2Deg;


        Vector3 fielderDirection = fielder.transform.position - Gameplay.instance.center.position;
        float fielderAngle = Mathf.Atan2(fielderDirection.z, fielderDirection.x) * Mathf.Rad2Deg;


        float angleDifference = Mathf.Abs(ballAngle - fielderAngle);

        // Direction-based score: Lower angle difference means better positioning
        float directionScore = -angleDifference; // Negative because lower is better

        Debug.Log("ball speed " + ball.GetComponent<Rigidbody>().velocity.magnitude);
        score += directionScore;

        if (ball.GetComponent<Rigidbody>().velocity.magnitude > 70 && ball.GetComponent<BallHit>().groundShot)
        {
            if (fielder.CompareTag("DeepFielder"))
            {
                score += 50;
            }
        }

        fielder.score = score;
        fielder.angleDiff = angleDifference;
        return score;
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

        // Ball initial position (where it started its flight)
        Vector3 ballStartPosition = hitBallPos; // Use the position at the moment of bat contact
        Vector3 ballVelocity = ballRb.velocity;
        float gravity = Mathf.Abs(Physics.gravity.y);

        // Height difference between ball's starting height and ground
        float startHeight = ballStartPosition.y;
        float groundHeight = -4.437081f; // Adjust this based on your ground level
        float heightDifference = startHeight - groundHeight;

        if (heightDifference <= 0)
        {
            Debug.LogWarning("Ball is already on or below the ground level.");
            landingPosition = ballStartPosition;
            landingPosition.y = fielders[3].transform.position.y;
            return landingPosition;
        }

        // Vertical velocity component
        float verticalVelocity = ballVelocity.y;

        // Solve for time of flight using the quadratic equation: y = vt + 0.5at^2
        float a = -0.5f * gravity; // Acceleration due to gravity
        float b = verticalVelocity; // Initial vertical velocity
        float c = heightDifference; // Height to ground

        float discriminant = (b * b) - (4 * a * c);

        if (discriminant < 0)
        {
            Debug.LogError("No valid solution for time of flight. Ball may never reach the ground.");
            return Vector3.zero;
        }

        // Solve for time to land (positive root only)
        float timeToLand = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        if (timeToLand < 0)
        {
            timeToLand = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        }

        if (float.IsNaN(timeToLand) || timeToLand <= 0)
        {
            Debug.LogError("Invalid time to land calculated.");
            return Vector3.zero;
        }

        // Calculate horizontal displacement
        Vector3 horizontalVelocity = new Vector3(ballVelocity.x, 0, ballVelocity.z);
        Vector3 horizontalDisplacement = horizontalVelocity * timeToLand;

        // Final landing position
        landingPosition = ballStartPosition + horizontalDisplacement;
        landingPosition.y = fielders[0].transform.position.y; // Align with fielder's height

        // Debug visuals
        Debug.DrawLine(ballStartPosition, landingPosition, Color.green, 2f);
        marker.position = landingPosition;

        Debug.Log($"Predicted landing position: {landingPosition} (Time to land: {timeToLand}s)");
        return landingPosition;
    }


    public void ResetFielders()
    {
        keeper.GetComponent<Fielder>().KeeperReset();
        foreach (var fielder in bestFielders)
        {
            fielder.Reset();
            fielder.startedRun = false;
            fielder.GetComponent<Animator>().enabled = true;
            //fielder.enabled = false;
        }
        bestFielders.Clear();
    }
}
