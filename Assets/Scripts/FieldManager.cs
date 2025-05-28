using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        bestFielders.Clear();
        ball = Gameplay.instance.currentBall;
        ballWasAirBorne = ball.GetComponent<BallHit>().groundShot = false;
        Debug.Log("fielder");
        //StartCoroutine(AssignFielders(ballAt));
        StartCoroutine(DelayAndCheck(ballAt));

    }

    IEnumerator DelayAndCheck(Vector3 ballAt)
    {
        yield return new WaitForSeconds(0.2f);
        Vector2 ballPos2D = new Vector2(ball.position.x, ball.position.z);
        Vector2 ballAt2D = new Vector2(ballAt.x, ballAt.z);
        Vector2 dir2D = (ballPos2D - ballAt2D).normalized;

        // Convert to Vector3 flat ray
        Vector3 flatDirection = new Vector3(dir2D.x, 0, dir2D.y);
        Ray ballPath = new Ray(new Vector3(ballAt.x, 0.2f, ballAt.z), flatDirection);

        RaycastHit[] hits = Physics.RaycastAll(ballPath, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);

        Debug.DrawRay(ballPath.origin, ballPath.direction, Color.green, 10);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("rayTest"))
            {
                Debug.Log("Added fielder  " + hit.collider.transform.parent.gameObject.name);
                Fielder fielder = hit.collider.transform.parent.GetComponent<Fielder>();
                fielder.enabled = true;
                fielder.targetPosition = hit.point;
                bestFielders.Add(fielder);
            }
            else
            {
                Debug.Log("sum else");
            }
        }
        foreach (var fielder in bestFielders)
        {
            if (!fielder.startedRun)
            {                
                fielder.startedRun = true;
                fielder.Initiate(ballAt, ball);
            }
        }
        if (!bestFielders.Contains(fielders[0]))
        {
            StartCoroutine(KeeperRunToRecieve());
        }
        else
        {
            if(bestFielders.Count>=3)
            {
                bestFielders.Remove(fielders[0]);
            }
        }
    }

    IEnumerator KeeperRunToRecieve()
    {
        keeper.GetComponent<Fielder>().enabled = true;
        keeper.GetComponent<Fielder>().ball = ball;
        keeper.GetComponent<FielderIK>().PlayAnimation(keeper.GetComponent<Fielder>().runningClip);
        Vector3 moveDirection;
        Quaternion lookRotation;

        while (Vector2.Distance(new Vector2(keeper.transform.position.x, keeper.transform.position.z),new Vector2(stumps.position.x, stumps.position.z))>2f)
        {
            moveDirection = (ball.position - keeper.transform.position).normalized;
            lookRotation = Quaternion.LookRotation(moveDirection);
            lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
            keeper.transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 100f);
            keeper.transform.position = Vector3.MoveTowards(keeper.transform.position, new Vector3(stumps.position.x, keeper.transform.position.y, stumps.position.z), 28 * Time.deltaTime);
            yield return null;
        }
        moveDirection = (ball.position - keeper.transform.position).normalized;
        lookRotation = Quaternion.LookRotation(moveDirection);
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, lookRotation.eulerAngles.z);
        keeper.GetComponent<FielderIK>().PlayAnimation(keeper.GetComponent<Fielder>().idleClip);
        keeper.transform.rotation = lookRotation;

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
