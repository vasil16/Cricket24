using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class Gameplay : MonoBehaviour
{
    public static Gameplay instance;

    [SerializeField] GameObject lostPanel, pauseBtn, mark, bails, ball, groundBounds, bat, bowler, overPanel, keeper, nonStrike;
    [SerializeField] RectTransform dragRect;
    [SerializeField] int balls, overs, wickets;
    [SerializeField] Vector3 ballLaunchPos, bound1, bound2, bound3, bound4, ballOriginPoint, ballScale, ballerTrueScale;
    [SerializeField] float speedMult, pitchXOffset, ballStopPoint;
    [SerializeField] public Transform batCenter, currentBall, bb, bowlerPalm, center;
    [SerializeField] Animator machineAnim;
    [SerializeField] List<float> launchSpeeds;
    [SerializeField] List<PitchPoints> pitchPoints;
    [SerializeField] Bat batter;
    [SerializeField] CameraLookAt broadcastCamComp;

    [SerializeField] Vector3 [] ballDeliverPoint;
    [SerializeField] int ballDeliverType;

    public float miRand, maRand, randomAngle;

    public CameraLookAt[] activeCams;
    public Bounds stadiumBounds;

    private int ballsLaunched = 0, run;

    private Rigidbody rb;
    public Camera sideCam;

    public bool isGameOver = false;
    public bool deliveryDead, opp, legalDelivery, readyToBowl;

    private void Awake()
    {
        instance = this;
        //bat.GetComponent<Rigidbody>().centerOfMass = batCenter.localPosition;
    }

    public float scc;

    void Start()
    {
        ballerTrueScale = bowler.transform.localScale;
        ShiftEnd();
        StartCoroutine(LaunchBallsWithDelay());
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

            ball.transform.SetParent(bowlerPalm,true);

            ball.transform.localScale = ballScale;

            Vector3 ballPitchPoint = GetRandomPointWithinBounds();

            //Vector3 ballPitchPoint = pitchPoints[ballDeliverType].points[Random.Range(0, 10)];

            ball.transform.localPosition = ballOriginPoint;

            ball.transform.rotation = Quaternion.Euler(-90, 0, 0);

            Vector3 direction = (ballPitchPoint - ballLaunchPos).normalized;

            float speed = 142;

            Vector3 force = direction * speed;


            yield return new WaitForSeconds(1f);

            batter.batterAnim.SetTrigger("ToStance");

            yield return new WaitForSeconds(5f);

            broadcastCamComp.startingRunUp = true;

            bowler.GetComponent<Animator>().enabled = true;
            if (ballDeliverType == 0 || ballDeliverType == 2)
            {
                bowler.GetComponent<Animator>().Play("bowl");
            }
            else
            {
                bowler.GetComponent<Animator>().Play("bowlAround");
            }

            //keeper.GetComponent<Animator>().SetTrigger("KeeperSteady");

            mark.transform.position = ballPitchPoint;
            rb = ball.GetComponent<Rigidbody>();
            yield return new WaitUntil(() => readyToBowl);
            ball.transform.SetParent(null,true);
            ball.transform.position = ballLaunchPos;
            rb.isKinematic = true;
            broadcastCamComp.readyToDeliver = false;
            currentBall = ball.transform;
            rb.isKinematic = false;
            ball.SetActive(true);
            

            readyToBowl = false;

            //----------------------------------
            {
                Vector3 toTarget = ballPitchPoint - ballLaunchPos;
                Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
                float y = toTarget.y; // This should be negative if the target is below
                float xz = toTargetXZ.magnitude;
                float gravity = Mathf.Abs(Physics.gravity.y);
                float speedSquared = speed * speed;

                // Proper discriminant formula for projectile motion from height
                float discriminant = speedSquared * speedSquared - gravity * (gravity * xz * xz + 2 * y * speedSquared);

                Debug.Log($"y = {y}, xz = {xz}, speed = {speed}, discriminant = {discriminant}");

                if (discriminant < 0f)
                {
                    Debug.LogWarning("No valid firing solution: speed too low or target too far.");
                    yield break; // Exit early
                }

                float discRoot = Mathf.Sqrt(discriminant);

                // Use lower angle for a flatter arc
                float angle = Mathf.Atan2(speedSquared - discRoot, gravity * xz);

                // Compose launch velocity vector
                Vector3 velocity = toTargetXZ.normalized * Mathf.Cos(angle) * speed;
                velocity.y = Mathf.Sin(angle) * speed;

                // Reset and apply to Rigidbody
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
                rb.AddForce(velocity, ForceMode.VelocityChange);
                rb.AddTorque(Vector3.forward * -10f, ForceMode.Impulse);
            }
            //----------------------------------




            //rb.WakeUp();
            //rb.AddTorque(Vector3.forward * -10);
            //rb.AddForce(force, ForceMode.Impulse);


            foreach (CameraLookAt cam in activeCams)
            {
                if(!cam.enabled)
                    cam.enabled = true;
                cam.ball = ball;                
            }

            yield return new WaitForSeconds(.7f);
            yield return new WaitUntil(() => ballPassed(ball.transform));

            broadcastCamComp.startingRunUp = false;                

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
                ShiftEnd();
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


    void ShiftEnd()
    {
        bowler.GetComponent<Animator>().enabled = false;
        //ballDeliverType = Random.Range(0, 3);
        ballLaunchPos = ballDeliverPoint[ballDeliverType];
        Vector3 nonStrikerPos = nonStrike.transform.position;
        
        switch (ballDeliverType)
        {
            case 0:                
                nonStrike.transform.position = new Vector3(nonStrikerPos.x, nonStrikerPos.y, 4.26f);
                bowler.transform.localScale = ballerTrueScale;
                bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -4f);
                break;

            case 1:
                nonStrike.transform.position = new Vector3(nonStrikerPos.x, nonStrikerPos.y, -3.7f);
                bowler.transform.localScale = ballerTrueScale;
                bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, 4.48f);
                break;

            case 2:
                nonStrike.transform.position = new Vector3(nonStrikerPos.x, nonStrikerPos.y, 4.26f);
                bowler.transform.localScale = new Vector3(ballerTrueScale.x * -1,ballerTrueScale.y, ballerTrueScale.z);
                bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, -4f);
                break;

            case 3:
                nonStrike.transform.position = new Vector3(nonStrikerPos.x, nonStrikerPos.y, -3.7f);
                bowler.transform.localScale = new Vector3(ballerTrueScale.x * -1, ballerTrueScale.y, ballerTrueScale.z);
                bowler.transform.position = new Vector3(bowler.transform.position.x, bowler.transform.position.y, 4.48f);
                break;
        }
        //bowler.GetComponent<Animator>().enabled = true;
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
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}

[System.Serializable]
public class PitchPoints
{
    public Vector3[] points;
}
