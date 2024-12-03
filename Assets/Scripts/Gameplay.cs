using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Gameplay : MonoBehaviour
{
    public static Gameplay instance;

    [SerializeField] GameObject lostPanel, pauseBtn, mark, bails, ball, groundBounds, bat, bowler;
    [SerializeField] AudioSource wicketFx;
    [SerializeField] RectTransform dragRect;
    [SerializeField] Vector3[] pitchPoints;
    [SerializeField] int balls, overs, wickets, randPitch;
    [SerializeField] Vector3 ballLaunchPos, bound1, bound2, bound3, bound4, ballOriginPoint;
    [SerializeField] float speedMult, pitchXOffset, ballStopPoint;
    [SerializeField] public Transform batCenter, currentBall, bb, bowlerPalm;
    [SerializeField] Animator machineAnim;
    [SerializeField] Text overText;
    [SerializeField] List<float> launchSpeeds;
    [SerializeField] Bat batter;

    public CameraLookAt[] activeCams;
    public Bounds stadiumBounds;

    private int ballsLaunched = 0, run;

    private Rigidbody rb;
    public Camera sideCam;

    bool isPaused;

    public bool isGameOver = false;
    public bool readyToBowl = true;
    public bool deliveryDead;
    public bool opp;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(ball.transform.position, helperdir);
    }

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
        Application.targetFrameRate = 60;
        activeCams = FindObjectsOfType<CameraLookAt>();
        stadiumBounds = groundBounds.GetComponent<Renderer>().bounds;
        bowlerPalm = ball.transform.parent;
        ballOriginPoint = ball.transform.localPosition;
    }

    Vector3 helperdir;


    IEnumerator LaunchBallsWithDelay()
    {
        batter.batterAnim.SetTrigger("ToWait");
        while (overs < 5 && !isGameOver)
        {

            yield return new WaitForSeconds(0.2f);

            deliveryDead = false;

            randPitch = Random.Range(0, 20);

            //Vector3 targetPos = pitchPoints[randPitch];

            Vector3 targetPos = GetRandomPointWithinBounds();

            mark.transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);

            //ball.transform.SetParent(bowlerPalm);

            ball.transform.localPosition = ballOriginPoint;

            ball.transform.rotation = Quaternion.Euler(-90, 0, 0);

            ball.SetActive(false);
            ball.transform.position = ballLaunchPos;

            Vector3 initialPosition = ball.transform.position;
            Vector3 direction = (targetPos - initialPosition).normalized;
            //Vector3 pitchPoint = direction * (launchSpeeds[Random.Range(0, launchSpeeds.Count)] * speedMult);
            Vector3 pitchPoint = direction * (launchSpeeds[0] * speedMult);
            helperdir = pitchPoint;

            foreach (CameraLookAt cam in activeCams)
            {
                if (MainGame.camIndex == 3)
                {
                    cam.transform.rotation = cam.defRotation;
                }                
            }

            batter.batterAnim.SetTrigger("ToStance");

            yield return new WaitForSeconds(3f);

            CameraLookAt.instance.startingRunUp = true;

            Bowl.instance.anim.Play("bowl");


            yield return new WaitUntil(() => Bowl.instance.ready);


            CameraLookAt.instance.readyToDeliver = false;
            currentBall = ball.transform;
            ball.GetComponent<Rigidbody>().isKinematic = false;
            ball.SetActive(true);
            ball.transform.SetParent(null);
            ball.transform.position = ballLaunchPos;
            rb = ball.GetComponent<Rigidbody>();

            Bowl.instance.ready = false;

            rb.WakeUp();
            rb.AddTorque(Vector3.forward * -10);
            rb.AddForce(pitchPoint, ForceMode.Impulse);

            foreach (CameraLookAt cam in activeCams)
            {
                //if (MainGame.camIndex == 3) { }
                //else
                {
                    cam.ball = ball;
                }
            }

            yield return new WaitForSeconds(.7f);
            yield return new WaitUntil(() => ballPassed(ball.transform));

            foreach (CameraLookAt cam in activeCams)
            {
                //if (MainGame.camIndex == 3) { }
                //else
                {
                    cam.startingRunUp = false;
                }
            }


            if (ball.GetComponent<BallHit>().secondTouch)
                StartCoroutine(CheckBallDirection());
            else
            {
                while(currentBall.position.x>ballStopPoint)
                {
                    yield return null;
                }
                yield return null;
                rb.isKinematic = true;
                deliveryDead = true;
            }


            ballsLaunched++;
            if (ballsLaunched % 6 == 0)
            {
                overs++;
                ballsLaunched = 0;
                //ShiftEnd();
            }

            yield return new WaitUntil(() => deliveryDead);
            bowler.GetComponent<Animator>().SetBool("DeliveryComplete", true);
            UpdateScoreBoard(ball.GetComponent<BallHit>());
            yield return new WaitForSeconds(2f);

            FieldManager.ResetFielder.Invoke();
            foreach (CameraLookAt cam in activeCams)
            {
                cam.ball = null;
                cam.CamReset();                
            }
            sideCam.depth = -2;
            sideCam.enabled = false;
            StartCoroutine(ResetBall(ball));
            overText.text = $"{overs}.{ballsLaunched}";
            yield return new WaitForSeconds(1);
            bowler.GetComponent<Animator>().SetBool("DeliveryComplete", false);
            yield return null;
        }
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
        Vector3 firstPos = ball.transform.position;
        yield return new WaitForSeconds(0.2f);
        Vector3 ballDirection = (ball.transform.position - firstPos).normalized;
        FieldManager.StartCheckField.Invoke(currentBall.transform.position);
    }


    bool ballPassed(Transform ballT)
    {
        if (ballT.position.x < -28.8 || ballT.GetComponent<BallHit>().secondTouch)
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
                    if (ballfinal.x < -116 || ballfinal.x > 133 || ballfinal.z > 110 || ballfinal.z < 110)
                    {
                        run += 2;
                    }
                    else
                        run += 1;
                    break;
                case "boundary":
                    if (ball.groundShot)
                        run += 4;
                    else
                        run += 6;
                    break;
                case "gallery":
                    run += 6;
                    break;

                default:
                    if (stadiumBounds.Contains(ballfinal))
                        run += 0;
                    else
                        run += 6;
                    break;
            }
        }
        else
        {
            run += 0;
        }
        Scorer.instance.NewScore(run, wickets);
    }

    public float miRand, maRand, randomAngle;
    [SerializeField] float[] xArr = { -0.37f, -2.05f };

    IEnumerator ResetBall(GameObject ball)
    {
        this.ball.SetActive(false);
        this.ball.transform.position = ballLaunchPos;
        ball.GetComponent<BallHit>().Reset();
        foreach (CameraLookAt cam in activeCams)
        {
            cam.ball = null;
        }
        yield return new WaitForSeconds(1);
        this.ball = ball;
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
        wicketFx.Play();
        //crowdFx.Play();
        //VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Failure);
    }

    IEnumerator SetOFf(GameObject gg)
    {
        yield return new WaitForSeconds(2f);
        gg.GetComponent<Rigidbody>().isKinematic = true;
        gg.SetActive(false);
    }

    [SerializeField] GameObject overPanel;

    public void PauseFn()
    {
        isPaused = true;
        gameObject.GetComponent<AudioSource>().Pause();
    }

    public void ResumeFn()
    {
        isPaused = false;
        gameObject.GetComponent<AudioSource>().Play();
    }

    public void Restart()
    {
        SceneManager.LoadScene("crk");
    }

}
