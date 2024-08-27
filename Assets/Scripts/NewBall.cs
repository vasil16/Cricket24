using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NewBall : MonoBehaviour
{
    public static NewBall instance;

    public GameObject bails; // Reference to the GameObject named "Bails"
    public GameObject Ball;
    public List<float> launchSpeeds;
    public List<float> launchHeights;// List of launch speeds for individual balls
    public int numberOfBallsToLaunch = 5; // Number of balls to launch

    private int ballsLaunched = 0;
    private Rigidbody2D rb;
    [SerializeField] Animator machineAnim;
    //[SerializeField] AnimationClip machineAnim;

    public bool isGameOver = false;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartCoroutine(LaunchBallsWithDelay());
        Application.targetFrameRate = 60;
        //rb = GetComponent<Rigidbody2D>();
    }

    IEnumerator LaunchBallsWithDelay()
    {        
        while (ballsLaunched < numberOfBallsToLaunch && !isGameOver)
        {
            machineAnim.SetTrigger("Restart");
            yield return new WaitForSeconds(0.2f);
            LaunchBall(launchSpeeds[Random.Range(0, launchSpeeds.Count)]); // Launch the ball with specific speed
            //machineAnim.Play("machineAnim");

            ballsLaunched++;
            yield return new WaitForSeconds(2f); // Wait for 1.2 seconds before next launch
        }
    }

    void LaunchBall(float launchSpeed)
    {
        GameObject newBall = Instantiate(Ball, new Vector3(14.3f, -0.78f, 0f), Quaternion.identity);
        
        rb = newBall.GetComponent<Rigidbody2D>();

        //if (rb != null && bails != null)
        //{
        //    Vector2 direction = (bails.transform.position - newBall.transform.position).normalized;

        //    rb.velocity = (direction) + new Vector2(Random.Range(-1.3f,0),0) * launchSpeed;

        //    Destroy(newBall, 2.9f);
        //}
        if (rb != null && bails != null)
        {
            // Calculate the normalized direction towards the target
            Vector2 direction = (bails.transform.position - newBall.transform.position).normalized;

            // Generate a random angle between -45 and 45 degrees
            float randomAngle = Random.Range(-10, 30f);

            // Apply the random angle to the direction
            Vector2 newDirection = Quaternion.Euler(0, 0, randomAngle) * direction;

            // Set the velocity magnitude to the launch speed
            rb.velocity = newDirection * launchSpeed;

            Destroy(newBall, 2.9f);
        }

        else
        {
            Debug.LogError("Rigidbody2D component or Bails reference not found!");
        }
    }


}