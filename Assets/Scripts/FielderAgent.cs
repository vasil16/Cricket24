//using Unity.MLAgents;
//using Unity.MLAgents.Sensors;
//using Unity.MLAgents.Actuators;
//using UnityEngine;

//public class FielderAgent : Agent
//{
//    [SerializeField] private Transform ballTransform;
//    [SerializeField] private float moveSpeed = 5f;

//    public override void OnEpisodeBegin()
//    {
//        // Reset the fielder and ball position for training
//        transform.localPosition = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
//    }

//    public override void CollectObservations(VectorSensor sensor)
//    {
//        // Observing ball and fielder relative position and ball velocity
//        sensor.AddObservation(transform.localPosition); // Fielder's position
//        sensor.AddObservation(ballTransform.localPosition); // Ball's position
//        sensor.AddObservation(ballTransform.GetComponent<Rigidbody>().velocity); // Ball's velocity
//    }

//    public override void OnActionReceived(ActionBuffers actions)
//    {
//        // Get movement actions (e.g., forward, backward, left, right)
//        Vector3 move = new Vector3(actions.ContinuousActions[0], 0, actions.ContinuousActions[1]) * moveSpeed * Time.deltaTime;
//        transform.localPosition += move;

//        // Reward based on the proximity to ball’s landing position
//        float distanceToBall = Vector3.Distance(transform.localPosition, ballTransform.localPosition);
//        AddReward(-distanceToBall); // Negative reward as distance increases
//    }

//    public override void Heuristic(in ActionBuffers actionsOut)
//    {
//        // Optional: control the agent with the keyboard during testing
//    }
//}
