using UnityEngine;
using UnityEngine.AI;

public class FielderAI : MonoBehaviour
{
    [Header("Fielding Parameters")]
    public Transform ballTransform;       // Reference to the ball
    public float pickUpRadius = 1.5f;     // Distance at which the fielder "picks up" the ball
    public float predictionTime = 1.0f;  // Time ahead to predict ball landing spot
    public float reactionDelay = 0.2f;   // Delay before the fielder reacts

    private NavMeshAgent agent;          // NavMeshAgent for movement
    private bool isFielding = false;     // Is the fielder currently active?

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (isFielding)
        {
            FieldTheBall();
        }
    }

    /// <summary>
    /// Starts the fielder's action toward the ball.
    /// </summary>
    public void StartFielding()
    {
        isFielding = true;
        Invoke(nameof(ReactToBall), reactionDelay); // Introduce reaction delay for realism
    }

    /// <summary>
    /// Stops the fielder's fielding action.
    /// </summary>
    public void StopFielding()
    {
        isFielding = false;
        agent.isStopped = true;
    }

    /// <summary>
    /// Predicts the ball's landing position and moves toward it.
    /// </summary>
    private void FieldTheBall()
    {
        if (!ballTransform) return;

        // Predict the landing position of the ball
        Vector3 predictedPosition = PredictBallLanding();

        // Move the fielder toward the predicted position
        if (Vector3.Distance(transform.position, predictedPosition) > pickUpRadius)
        {
            agent.SetDestination(predictedPosition);
        }
        else
        {
            // Ball is within pickup radius, stop movement
            agent.isStopped = true;
            PickUpBall();
        }
    }

    /// <summary>
    /// Reacts to the ball's movement after a delay.
    /// </summary>
    private void ReactToBall()
    {
        if (!ballTransform) return;
        agent.isStopped = false;
    }

    /// <summary>
    /// Simulates the ball pickup logic.
    /// </summary>
    private void PickUpBall()
    {
        Debug.Log($"{gameObject.name} picked up the ball!");
        StopFielding();
        // Additional logic to "return" the ball can go here.
    }

    /// <summary>
    /// Predicts the landing position of the ball based on its velocity.
    /// </summary>
    /// <returns>Predicted landing position</returns>
    private Vector3 PredictBallLanding()
    {
        Rigidbody ballRigidbody = ballTransform.GetComponent<Rigidbody>();
        if (!ballRigidbody) return ballTransform.position;

        // Predict future position based on velocity
        Vector3 currentPosition = ballTransform.position;
        Vector3 velocity = ballRigidbody.velocity;

        // Use basic physics to estimate landing spot
        Vector3 predictedPosition = currentPosition + velocity * predictionTime;
        predictedPosition.y = 0; // Ensure prediction stays on the ground (y-axis = 0)

        return predictedPosition;
    }
}
