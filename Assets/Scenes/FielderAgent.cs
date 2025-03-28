using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;

public class FielderAgent : Agent
{
    Vector3 actualPos, actualRot;
    public Transform ball;
    private NavMeshAgent agent;
    private bool isActive = false; // Move only when assigned
    private Rigidbody ballRb;
    private float ballStoppedThreshold = 0.4f; // Adjust this if needed

    public override void Initialize()
    {
        actualPos = transform.position;
        actualRot = transform.rotation.eulerAngles;
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false; // Start disabled
    }

    private void FixedUpdate()
    {
        if(isActive)
        {
            Debug.Log("active called" + gameObject.name);
            RequestDecision();
            RequestAction();
        }
    }

    //public override void CollectObservations(VectorSensor sensor)
    //{
    //    if (!isActive) return;

    //    // Observe ball position, velocity, and fielder position
    //    sensor.AddObservation(ball.position);
    //    sensor.AddObservation(ballRb.velocity);
    //    sensor.AddObservation(transform.position);
    //}

    //public override void CollectObservations(VectorSensor sensor)
    //{
    //    if (ball == null) return;  // Ensure the ball reference is set

    //    Vector3 ballPosition = ball.position;  // Ball world position
    //    Vector3 fielderPosition = transform.position;  // Fielder's position
    //    Vector3 directionToBall = (ballPosition - fielderPosition).normalized;  // Direction towards ball

    //    // Add observations (make sure the number matches your config.yaml)
    //    sensor.AddObservation(fielderPosition.x);
    //    sensor.AddObservation(fielderPosition.z);
    //    sensor.AddObservation(ballPosition.x);
    //    sensor.AddObservation(ballPosition.z);
    //    sensor.AddObservation(directionToBall.x);
    //    sensor.AddObservation(directionToBall.z);
    //}

    public override void CollectObservations(VectorSensor sensor)
    {
        if (ball == null)
        {
            sensor.AddObservation(Vector3.zero); // Placeholder if ball is missing
            sensor.AddObservation(Vector3.zero);
            return;
        }

        // 1-3: Relative position of ball to fielder (normalized)
        Vector3 relativeBallPos = ball.position - transform.position;
        sensor.AddObservation(relativeBallPos.normalized);

        // 4-5: Fielder’s movement direction (normalized)
        Vector3 agentVelocity = agent.velocity.normalized;
        sensor.AddObservation(agentVelocity);

        // 6: Distance to ball (normalized)
        float distanceToBall = Vector3.Distance(transform.position, ball.position);
        sensor.AddObservation(distanceToBall / 50f); // Normalize (assuming max 50 units)
    }



    //public override void OnActionReceived(ActionBuffers actions)
    //{
    //    Debug.Log("called  " + gameObject.name);
    //    Debug.Log($"Received Actions: {actions.ContinuousActions[0]}, {actions.ContinuousActions[1]}");

    //    float moveX = actions.ContinuousActions[0];
    //    float moveZ = actions.ContinuousActions[1];

    //    if (Mathf.Abs(moveX) < 0.01f && Mathf.Abs(moveZ) < 0.01f)
    //    {
    //        Debug.Log("Movement too small, skipping...");
    //        return;
    //    }

    //    Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized;
    //    float moveSpeed = 5f;  // Reduced speed to prevent overshooting

    //    Vector3 targetPosition = transform.position + moveDirection * moveSpeed;

    //    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
    //    {
    //        agent.SetDestination(hit.position);
    //        Debug.Log($"Moving to: {hit.position}");
    //    }
    //    else
    //    {
    //        Debug.Log("NavMesh SamplePosition failed.");
    //    }
    //    CheckIfBallIsDead();
    //}

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("called " + gameObject.name);

        if (ball == null) return;

        // Ensure the ball is on a valid NavMesh area before moving
        if (NavMesh.SamplePosition(ball.position, out NavMeshHit ballHit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(ballHit.position);  // Move directly to the ball
            Debug.Log($"Moving towards: {ballHit.position}");
        }
        else
        {
            Debug.Log("NavMesh SamplePosition failed for ball.");
        }
        //if(ball.GetComponent<BallHit>().stopTriggered)
        //{
        //    SetReward(1f); // Reward for stopping the ball
        //    EndEpisode();
        //}
        CheckIfBallIsDead();
    }



    // Allow Field Manager to activate AI when needed
    public void ActivateFielder(Transform newBall)
    {
        ball = newBall;
        ballRb = ball.GetComponent<Rigidbody>();
        isActive = true;
        agent.enabled = true;
        RequestDecision();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("some called enter " + other.gameObject.name);
        if (other.CompareTag("Ball") && isActive)
        {
            SetReward(1f); // Reward for stopping the ball
            Gameplay.instance.deliveryDead = true;
            EndEpisode();
        }
    }

    private void CheckIfBallIsDead()
    {
        // If the ball is moving very slowly, consider it "dead"
        if (Gameplay.instance.deliveryDead)
        {
            SetReward(-0.01f);
            EndEpisode();
        }
    }

    private bool IsBallOutOfBounds()
    {
        // Define a boundary based on your game environment
        float boundaryX = 50f;
        float boundaryZ = 50f;

        return Mathf.Abs(ball.position.x) > boundaryX || Mathf.Abs(ball.position.z) > boundaryZ;
    }

    public void Reset()
    {
        agent.Stop();
        agent.ResetPath();
        isActive = false;
        transform.position = actualPos;
        transform.rotation = Quaternion.Euler(actualRot);

    }
}
