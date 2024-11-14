using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Pusher : MonoBehaviour
{
    public static Pusher instance;

    [SerializeField] GameObject lostPanel, pauseBtn, mark, bails, Ball, newBall, instBall, groundBounds, bat, bowler;
    [SerializeField] AudioSource wicketFx;
    [SerializeField] RectTransform dragRect;
    [SerializeField] Vector3[] pitchPoints;
    [SerializeField] int balls, overs, wickets, randPitch;
    [SerializeField] Vector3 ballLaunchPos;
    [SerializeField] float speedMult, pitchXOffset, ballStopPoint;
    [SerializeField] public Transform batCenter, currentBall, bb;
    [SerializeField] Animator machineAnim;
    [SerializeField] Text overText;
    public CameraLookAt[] activeCams;
    public Bounds stadiumBounds;

    public List<float> launchSpeeds, launchHeights;
    public int numberOfBallsToLaunch = 5;

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
        Gizmos.DrawLine(instBall.transform.position, helperdir);
    }

    private void Awake()
    {
        instance = this;
        //bat.GetComponent<Rigidbody>().centerOfMass = batCenter.localPosition;
    }

    public float scc;

    void Start()
    {
        ShiftEnd();
        StartCoroutine(LaunchBallsWithDelay());
        Application.targetFrameRate = 60;
        activeCams = FindObjectsOfType<CameraLookAt>();
        stadiumBounds = groundBounds.GetComponent<Renderer>().bounds;
    }

    Vector3 helperdir;

    [SerializeField] Bat batter;

    IEnumerator LaunchBallsWithDelay()
    {
        while (overs < 5 && !isGameOver)
        {

            yield return new WaitForSeconds(0.2f);

            deliveryDead = false;

            randPitch = Random.Range(0, 20);

            Vector3 targetPos = pitchPoints[randPitch];

            mark.transform.position = new Vector3(targetPos.x + pitchXOffset, targetPos.y, targetPos.z);


            newBall = instBall;
            newBall.transform.rotation = Quaternion.Euler(-90, 0, 0);

            newBall.SetActive(false);
            newBall.transform.position = ballLaunchPos;

            Vector3 initialPosition = newBall.transform.position;
            Vector3 direction = (targetPos - initialPosition).normalized;
            Vector3 pitchPoint = direction * (launchSpeeds[Random.Range(0, launchSpeeds.Count)] * speedMult);
            helperdir = pitchPoint;

            yield return new WaitForSeconds(1f);

            Bowl.instance.anim.Play("bowl");

            //batter.batterAnim.Play("hitGround");

            yield return new WaitUntil(() => Bowl.instance.ready);

            CameraLookAt.instance.readyToDeliver = false;
            currentBall = newBall.transform;
            newBall.GetComponent<Rigidbody>().isKinematic = false;
            newBall.SetActive(true);

            rb = newBall.GetComponent<Rigidbody>();

            Bowl.instance.ready = false;

            rb.WakeUp();
            rb.AddTorque(Vector3.forward * -10);
            rb.AddForce(pitchPoint, ForceMode.Impulse);

            foreach (CameraLookAt cam in activeCams)
            {
                //if (MainGame.camIndex == 3)
                    cam.ball = newBall;
            }

            yield return new WaitForSeconds(.7f);
            yield return new WaitUntil(() => ballPassed(newBall.transform));

            if(newBall.GetComponent<BallHit>().secondTouch)
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
                ShiftEnd();
            }

            yield return new WaitUntil(() => deliveryDead);
            UpdateScoreBoard(newBall.GetComponent<BallHit>());
            yield return new WaitForSeconds(2f);

            FieldManager.ResetFielder.Invoke();
            
            sideCam.depth = -2;
            sideCam.enabled = false;
            StartCoroutine(ResetBall(newBall));
            overText.text = $"{overs}.{ballsLaunched}";
            yield return new WaitForSeconds(1);

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
        // Generate random x and z coordinates within the defined range
        float randomX = Random.Range(-45.8f, 22.4f);
        float randomZ = Random.Range(-4f, .8f);

        // Set y to 0 for a flat plane
        return new Vector3(randomX, -4.427082f, randomZ);
    }

    IEnumerator CheckBallDirection()
    {
        Vector3 firstPos = newBall.transform.position;
        yield return new WaitForSeconds(0.2f);
        Vector3 ballDirection = (newBall.transform.position - firstPos).normalized;
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
        newBall.SetActive(false);
        newBall.transform.position = ballLaunchPos;
        ball.GetComponent<BallHit>().Reset();
        foreach (CameraLookAt cam in activeCams)
        {
            cam.ball = null;
        }
        yield return new WaitForSeconds(1);
        instBall = ball;
    }

    #region Old
    void LaunchBall(float launchSpeed)
    {
        float cc = xArr[Random.Range(0, 1)];
        //GameObject newBall = Instantiate(Ball, new Vector3(42.7f, 1.61f, cc), Quaternion.Euler(-90, 0, 0));
        GameObject newBall = Instantiate(Ball, ballLaunchPos, Quaternion.Euler(-90, 0, 0));
        currentBall = newBall.transform;
        newBall.GetComponent<Rigidbody>().isKinematic = false;
        newBall.SetActive(true);
        if (CameraLookAt.instance != null)
        {
            CameraLookAt.instance.ball = newBall;
        }
        rb = newBall.GetComponent<Rigidbody>();

        if (rb != null && bails != null)
        {
            Vector3 initialPosition = newBall.transform.position;

            randPitch = Random.Range(0, 20);
            mark.transform.position = pitchPoints[randPitch];
            Vector3 direction = (pitchPoints[randPitch] - initialPosition).normalized;

            randomAngle = Random.Range(miRand, maRand);
            Vector3 axis = Vector3.down;
            Quaternion rotation = Quaternion.AngleAxis(randomAngle, axis);

            Vector3 newDirection = rotation * direction;

            Vector3 acDir = new Vector3(newDirection.x, newDirection.y, 0);

            acDir = new Vector3(acDir.x, acDir.y + randomAngle, acDir.z);


            rb.WakeUp();

            //rb.AddForce(direction * (launchSpeed * speedMult), ForceMode.Impulse);

            rb.AddForce(acDir * (launchSpeed * speedMult), ForceMode.Impulse);

            Destroy(newBall, 2f);

            //StartCoroutine(SetOFf(newBall));
        }
        else
        {
            Debug.LogError("Rigidbody component or Bails reference not found!");
        }
    }
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
