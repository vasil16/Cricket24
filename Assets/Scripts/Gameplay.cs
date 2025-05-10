using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Burst.Intrinsics;

public class Gameplay : MonoBehaviour
{
    public static Gameplay instance;

    [SerializeField] GameObject lostPanel, pauseBtn, mark, bails, ball, groundBounds, bat, bowler, overPanel;
    [SerializeField] RectTransform dragRect;
    [SerializeField] Vector3[] pitchPoints;
    [SerializeField] int balls, overs, wickets, randPitch;
    [SerializeField] Vector3 ballLaunchPos, bound1, bound2, bound3, bound4, ballOriginPoint, ballScale;
    [SerializeField] float speedMult, pitchXOffset, ballStopPoint;
    [SerializeField] public Transform batCenter, currentBall, bb, bowlerPalm, center;
    [SerializeField] Animator machineAnim;
    [SerializeField] List<float> launchSpeeds;
    [SerializeField] Bat batter;
    [SerializeField] CameraLookAt broadcastCamComp;

    public float miRand, maRand, randomAngle;
    [SerializeField] float[] xArr = { -0.37f, -2.05f };

    public CameraLookAt[] activeCams;
    public Bounds stadiumBounds;

    private int ballsLaunched = 0, run;

    private Rigidbody rb;
    public Camera sideCam;

    public bool isGameOver = false;
    public bool readyToBowl = true;
    public bool deliveryDead, opp, legalDelivery;

    private void Awake()
    {
        instance = this;
        //bat.GetComponent<Rigidbody>().centerOfMass = batCenter.localPosition;
    }

    public float scc;

    void Start()
    {
        //ShiftEnd();
        StartCoroutine(LaunchBallsWithDelay());
        //Application.targetFrameRate = 60;
        //activeCams = FindObjectsOfType<CameraLookAt>();
        stadiumBounds = groundBounds.GetComponent<Renderer>().bounds;
        bowlerPalm = ball.transform.parent;
        ballOriginPoint = ball.transform.localPosition;
        ballScale = ball.transform.localScale;
    }

    IEnumerator LaunchBallsWithDelay()
    {
        batter.batterAnim.SetTrigger("ToWait");
        while (overs < 5 && !isGameOver)
        {
            yield return new WaitForSeconds(0.2f);

            deliveryDead = false;

            randPitch = Random.Range(0, 20);

            //Vector3 ballPitchPoint = pitchPoints[randPitch];


            ball.transform.SetParent(bowlerPalm,true);

            ball.transform.localScale = ballScale;

            Vector3 ballPitchPoint = GetRandomPointWithinBounds();

            ball.transform.localPosition = ballOriginPoint;

            ball.transform.rotation = Quaternion.Euler(-90, 0, 0);

            Vector3 direction = (ballPitchPoint - ballLaunchPos).normalized;

            float speed = 80 * 0.25f;

            Vector3 force = direction * speed;

            //mark.transform.position = ballPitchPoint;

            yield return new WaitForSeconds(1f);

            batter.batterAnim.SetTrigger("ToStance");

            yield return new WaitForSeconds(5f);

            broadcastCamComp.startingRunUp = true;

            Bowl.instance.anim.Play("bowl");

            //mark.transform.position = PredictLandingPosition(ball.transform.position, force * 10);

            mark.transform.position = ballPitchPoint;
            rb = ball.GetComponent<Rigidbody>();
            yield return new WaitUntil(() => Bowl.instance.ready);
            ball.transform.SetParent(null,true);
            ball.transform.position = ballLaunchPos;
            rb.isKinematic = true;
            broadcastCamComp.readyToDeliver = false;
            currentBall = ball.transform;
            rb.isKinematic = false;
            ball.SetActive(true);
            

            Bowl.instance.ready = false;
            rb.WakeUp();
            rb.AddTorque(Vector3.forward * -10);
            rb.AddForce(force, ForceMode.Impulse);

            //LaunchBallToPitchPoint(ballLaunchPos, ballPitchPoint);

            foreach (CameraLookAt cam in activeCams)
            {
                if(!cam.enabled)
                    cam.enabled = true;
                cam.ball = ball;                
            }

            yield return new WaitForSeconds(.7f);
            yield return new WaitUntil(() => ballPassed(ball.transform));


            broadcastCamComp.startingRunUp = false;                

            StartCoroutine(CheckBallDirection());

            yield return new WaitUntil(() => deliveryDead);

            yield return new WaitForSeconds(1);

            bowler.GetComponent<Animator>().SetBool("DeliveryComplete", true);
            yield return new WaitForSeconds(2f);

            if(!legalDelivery)
            {
                if(ball.GetComponent<BallHit>().secondTouch)
                {
                    legalDelivery = true;
                }
            }

            if(legalDelivery)
            {
                ballsLaunched++;
            }


            if (ballsLaunched > 0 && ballsLaunched % 6 == 0)
            {
                overs++;
                ballsLaunched = 0;
                //ShiftEnd();
            }

            UpdateScoreBoard(ball.GetComponent<BallHit>());            
            
            sideCam.depth = -2;
            sideCam.enabled = false;            
            yield return new WaitForSeconds(1);
            FieldManager.ResetFielder.Invoke();
            foreach (CameraLookAt cam in activeCams)
            {
                cam.ball = null;
                cam.CamReset();
            }
            yield return null;

            batter.batterAnim.ResetTrigger("ToStance");
        }
    }

    public void SetBatter()
    {
        batter.batterAnim.Play("trigger");
    }

    private void LaunchBallToPitchPoint(Vector3 ballLaunchPos, Vector3 ballPitchPoint)
    {
        // Gravity constant (you can adjust this based on your scene's gravity)
        float landDistance = Mathf.Abs(ballLaunchPos.x - ballPitchPoint.x);

        float gravity = Physics.gravity.y;

        // Distance from the launch position to the target pitch point
        float distance = Vector3.Distance(ballLaunchPos, ballPitchPoint);

        // The desired time to reach the target (this is an estimate, adjust as needed)
        float timeToReachTarget = 0.58f; // Change this based on how fast you want the ball to travel

        Debug.Log("speeed" + timeToReachTarget);

        // Calculate the initial velocity required to reach the target point
        float verticalSpeed = (ballPitchPoint.y - ballLaunchPos.y - 0.5f * gravity * Mathf.Pow(timeToReachTarget, 2)) / timeToReachTarget;

        // Calculate the horizontal velocity (ignoring gravity in horizontal direction)
        Vector3 horizontalDirection = (ballPitchPoint - ballLaunchPos);
        horizontalDirection.y = 0; // Ignore vertical direction for horizontal velocity calculation
        Vector3 horizontalVelocity = horizontalDirection.normalized * (distance / timeToReachTarget);

        // Combine vertical and horizontal components to get the final velocity
        Vector3 initialVelocity = horizontalVelocity;
        initialVelocity.y = verticalSpeed;

        // Apply the velocity to the ball's Rigidbody
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.velocity = initialVelocity; // Directly set the velocity to simulate the launch

        // Optional: If you want to add torque for spin, you can add it here
        rb.AddTorque(Vector3.forward * -10);
    }

    void ShiftEnd()
    {
        opp = Random.Range(0, 2) == 0;
        if(opp)
        {
            ballLaunchPos.z = 2.35f;
            bowler.transform.position =  new Vector3(bowler.transform.position.x, bowler.transform.position.y, 2.466186f);
        }
        else
        {
            ballLaunchPos.z = -1.64f;
            bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -2.999954f);            
        }
    }

    Vector3 GenerateRandomPointOnPlane()
    {
        float randomX = Random.Range(-45.8f, 22.4f);
        float randomZ = Random.Range(-4f, .8f);
        return new Vector3(randomX, -4.427082f, randomZ);
    }

    Vector3 GetRandomPointWithinBounds()
    {
        float minX = Mathf.Min(bound1.x, bound2.x, bound3.x, bound4.x);
        float maxX = Mathf.Max(bound1.x, bound2.x, bound3.x, bound4.x);

        float minZ = Mathf.Min(bound1.z, bound2.z, bound3.z, bound4.z);
        float maxZ = Mathf.Max(bound1.z, bound2.z, bound3.z, bound4.z);

        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);

        randomX = Mathf.Clamp(randomX, minX, maxX);
        randomZ = Mathf.Clamp(randomZ, minZ, maxZ);

        return new Vector3(randomX, -4.427082f, randomZ);
    }

    IEnumerator CheckBallDirection()
    {
        yield return null;
        FieldManager.StartCheckField.Invoke(currentBall.transform.position);
    }


    bool ballPassed(Transform ballT)
    {
        if (ballT.position.x < -31.99f || ballT.GetComponent<BallHit>().secondTouch)
            return true;
        return false;
    }

    void UpdateScoreBoard(BallHit ball)
    {
        Vector3 ballfinal = ball.transform.position;
        if (ball.secondTouch)
        {
            switch (ball.lastHit)
            {
                case "Ground":
                    if(ball.boundary)
                    {
                        run = 4;
                    }
                    break;
                case "boundary":
                    if (ball.groundShot)
                        run = 4;
                    else
                        run = 6;
                    break;
                case "gallery":
                    run = 6;
                    break;

                default:
                    if (stadiumBounds.Contains(ballfinal))
                        run = 0;
                    else
                        run = 6;
                    break;
            }
        }
        else
        {
            run = legalDelivery ? 0 : 1;
        }
        string detail = legalDelivery ? run+"" : "wd";
        Scorer.instance.UpdateScore(run, wickets, overs, ballsLaunched, detail);
        StartCoroutine(ResetBall(ball.gameObject));
    }    

    IEnumerator ResetBall(GameObject ball)
    {
        transform.position = ballLaunchPos;
        ball.GetComponent<BallHit>().Reset();
        yield return new WaitForSeconds(1);
    }

    #region Old
    //void LaunchBall(float launchSpeed)
    //{
    //    float cc = xArr[Random.Range(0, 1)];
    //    //GameObject newBall = Instantiate(Ball, new Vector3(42.7f, 1.61f, cc), Quaternion.Euler(-90, 0, 0));
    //    GameObject newBall = Instantiate(Ball, ballLaunchPos, Quaternion.Euler(-90, 0, 0));
    //    currentBall = newBall.transform;
    //    newBall.GetComponent<Rigidbody>().isKinematic = false;
    //    newBall.SetActive(true);
    //    if (CameraLookAt.instance != null)
    //    {
    //        CameraLookAt.instance.ball = newBall;
    //    }
    //    rb = newBall.GetComponent<Rigidbody>();

    //    if (rb != null && bails != null)
    //    {
    //        Vector3 initialPosition = newBall.transform.position;

    //        randPitch = Random.Range(0, 20);
    //        mark.transform.position = pitchPoints[randPitch];
    //        Vector3 direction = (pitchPoints[randPitch] - initialPosition).normalized;

    //        randomAngle = Random.Range(miRand, maRand);
    //        Vector3 axis = Vector3.down;
    //        Quaternion rotation = Quaternion.AngleAxis(randomAngle, axis);

    //        Vector3 newDirection = rotation * direction;

    //        Vector3 acDir = new Vector3(newDirection.x, newDirection.y, 0);

    //        acDir = new Vector3(acDir.x, acDir.y + randomAngle, acDir.z);


    //        rb.WakeUp();

    //        //rb.AddForce(direction * (launchSpeed * speedMult), ForceMode.Impulse);

    //        rb.AddForce(acDir * (launchSpeed * speedMult), ForceMode.Impulse);

    //        Destroy(newBall, 2f);

    //        //StartCoroutine(SetOFf(newBall));
    //    }
    //    else
    //    {
    //        Debug.LogError("Rigidbody component or Bails reference not found!");
    //    }
    //}
    #endregion

    public void Out()
    {
        isGameOver = true;
        lostPanel.SetActive(true);
        pauseBtn.SetActive(false);        
        //crowdFx.Play();
        //VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Failure);
    }

    IEnumerator SetOFf(GameObject gg)
    {
        yield return new WaitForSeconds(2f);
        gg.GetComponent<Rigidbody>().isKinematic = true;
        gg.SetActive(false);
    }    

    public void PauseFn()
    {
        Time.timeScale = 0;
        gameObject.GetComponent<AudioSource>().Pause();
    }

    public void ResumeFn()
    {
        Time.timeScale = 1;
        gameObject.GetComponent<AudioSource>().Play();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
