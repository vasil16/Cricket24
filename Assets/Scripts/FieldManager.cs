using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldManager : MonoBehaviour
{
    public List<Fielder> fielders;
    public Transform ball;
    public float fieldingRange = 1.5f;

    public List<Fielder> bestFielders = new List<Fielder>();
    public static Action<Vector3> StartCheckField,startLearn;
    public static Action ResetFielder, resetML;

    public static Vector3 hitBallPos, hitVelocity;

    Vector3 landPos;
    public float score;
    public Transform marker;
    [SerializeField] MeshRenderer ignoreBounds;

    private void Start()
    {
        StartCheckField = AssignBestFielders;
        ResetFielder = ResetFielders;
        startLearn = AssignForML;
        resetML = ResetField;
    }

    public void AssignBestFielders(Vector3 ballAt)
    {
        StartCoroutine(AssignFielders(ballAt));
    }

<<<<<<< Updated upstream

=======
    public void AssignForML(Vector3 ballAt)
    {
        ball = Gameplay.instance.currentBall;
        ballWasAirBorne = ball.GetComponent<BallHit>().groundShot = false;
        {
            Debug.Log("fielder");
            StartCoroutine(AssignFielders(ballAt));
        }
    }

    IEnumerator KeeperReceive()
    {
        keeper.GetComponent<Animator>().enabled = true;
        keeper.GetComponent<Fielder>().enabled = true;
        keeper.GetComponent<Fielder>().ball = ball;
        while (ball.GetComponent<BallHit>().keeperReceive == false)
        {
            keeper.transform.position = new Vector3(keeper.transform.position.x, keeper.transform.position.y, Mathf.MoveTowards(keeper.position.z, ball.position.z, 20 * Time.deltaTime));
            yield return null;
        }
        while (ball.GetComponent<BallHit>().stopTriggered == false)
        {
            if (ball.GetComponent<BallHit>().fielderReached) break;
            keeper.GetComponent<Fielder>().KeeperRecieve();
            yield return null;
        }
        Gameplay.instance.deliveryDead = true;        
    }
>>>>>>> Stashed changes

    IEnumerator AssignFielders(Vector3 ballAt)
    {
        ball = Gameplay.instance.currentBall;
        hitBallPos = ballAt;
        hitVelocity = ball.GetComponent<Rigidbody>().velocity;
        yield return new WaitForSeconds(0.4f);

        int fielderCount = 1;

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
        else
        {
            landPos = PredictLandingPosition(ball);
            fielderCount = 2;
            Debug.Log("Air ball detected");
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

            //FielderAgent bestFielder = fielder.GetComponent<FielderAgent>();  // Your existing selection logic
            //bestFielder.ActivateFielder(ball);
        }

        bestFielders = selectedFielders;
<<<<<<< Updated upstream
=======
        if (!bestFielders.Contains(fielders[0]))
        {
            StartCoroutine(KeeperRunToRecieve());
            Vector3 moveDirection = (ball.position - keeper.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            keeper.transform.rotation = lookRotation;
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
            Vector3 moveDirection = (ball.position - keeper.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            keeper.transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 30f);
            keeper.transform.position = Vector3.MoveTowards(keeper.transform.position, new Vector3(stumps.position.x, keeper.transform.position.y, stumps.position.z), 28 * Time.deltaTime);
            yield return null;
        }
        keeper.GetComponent<Animator>().SetTrigger("StopField");
>>>>>>> Stashed changes
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
        
        if (ball.GetComponent<Rigidbody>().velocity.magnitude > 70)
        {
            if (fielder.CompareTag("DeepFielder"))
            {
                // Boost score for deep fielders if the shot is fast
                //score += (directionScore / 2);
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
        Debug.Log("reeseet called");
        foreach (var fielder in bestFielders)
        {
            fielder.Reset();
            fielder.startedRun = false;
            //fielder.enabled = false;
        }
        bestFielders.Clear();
    }

    public void ResetField()
    {
        foreach (var fielder in bestFielders)
        {
            fielder.gameObject.GetComponent<FielderAgent>().Reset();
        }
        bestFielders.Clear();
    }
}
