using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Pusher : MonoBehaviour
{
    public static Pusher instance;

    [SerializeField] GameObject lostPanel, pauseBtn, mark, bails, Ball, newBall, instBall;
    [SerializeField] RectTransform dragRect;
    [SerializeField] Transform activeCamTrans;
    [SerializeField] Vector3[] pitchPoints, ppT;
    [SerializeField] int balls, overs, wickets, randPitch;
    [SerializeField] Vector3 ballLaunchPos;
    [SerializeField] float speedMult;
    [SerializeField] public Transform batTrans, currentBall;
    [SerializeField] Animator machineAnim;
    [SerializeField] Text overText;
    public CameraLookAt[] activeCams;
    
    public List<float> launchSpeeds, launchHeights;
    public int numberOfBallsToLaunch = 5;

    private int ballsLaunched = 0;
    private Rigidbody rb;
    public Camera sideCam;

    bool isPaused;

    public bool isGameOver = false;

    private void Awake()
    {
        instance = this;
    }

    public float scc;

    void Start()
    {
        StartCoroutine(LaunchBallsWithDelay());
        Application.targetFrameRate = 60;
        activeCamTrans = GameObject.FindObjectOfType<Camera>().transform;
        activeCams = GameObject.FindObjectsOfType<CameraLookAt>();
    }

    private void Update()
    {
        
    }

    //private void Update()
    //{
    //    if (Input.touchCount > 0)
    //    {
    //        touch = Input.GetTouch(0);
    //        if (RectTransformUtility.RectangleContainsScreenPoint(dragRect, touch.position))
    //        {
    //            if (touch.phase == TouchPhase.Moved)
    //            {
    //                if (touch.deltaPosition.x > 0)
    //                    activeCamTrans.Rotate(Vector3.up);
    //                else
    //                    activeCamTrans.Rotate(Vector3.up * -1);
    //            }
    //        }
    //    }
    //}


    IEnumerator LaunchBallsWithDelay()
    {
        yield return new WaitForSeconds(1);
        while (overs < 5 && !isGameOver)
        {
            if (isPaused)
            {
                yield return null;
            }
            else
            {
                machineAnim.SetTrigger("Restart");
                yield return new WaitForSeconds(0.2f);
                //LaunchBall(launchSpeeds[Random.Range(0, launchSpeeds.Count)]);


                randPitch = Random.Range(0, 20);
                mark.transform.position = pitchPoints[randPitch];
                yield return new WaitForSeconds(1f);
                float cc = xArr[Random.Range(1, 2)];
                ballLaunchPos.z = cc;
                if (overs == 0 && ballsLaunched == 0)
                {
                    newBall = Instantiate(Ball, ballLaunchPos, Quaternion.Euler(-90, 0, 0));
                }
                else
                {
                    newBall = instBall;                    
                    newBall.transform.rotation = Quaternion.Euler(Vector3.left * -90);
                }
                currentBall = newBall.transform;
                newBall.GetComponent<Rigidbody>().isKinematic = false;
                newBall.SetActive(true);

                rb = newBall.GetComponent<Rigidbody>();
                Vector3 initialPosition = newBall.transform.position;
                Vector3 targetPos = pitchPoints[randPitch];
                Vector3 direction = (targetPos - initialPosition).normalized;

                {
                    //float time = 0;
                    //float duration = 2;

                    //while (time <= duration)
                    //{
                    //    time += Time.deltaTime;
                    //    newBall.transform.position = Vector3.Lerp(initialPosition, targetPos, time / duration);
                    //    yield return null;
                    //}
                }

                rb.WakeUp();
                rb.AddTorque(Vector3.forward * -20);
                rb.AddForce(direction * (launchSpeeds[Random.Range(0, launchSpeeds.Count)] * speedMult), ForceMode.Impulse);

                yield return new WaitForSeconds(.3f);

                //if (CameraLookAt.instance != null && MainGame.camIndex == 3)
                //{
                //    CameraLookAt.instance.ball = newBall;
                //}

                foreach(CameraLookAt cam in activeCams)
                {
                    if(MainGame.camIndex == 3)
                        cam.ball = newBall;
                }

                //Destroy(newBall, 4f);                
                ballsLaunched++;
                if(ballsLaunched%6==0)
                {
                    overs++;                    
                    ballsLaunched = 0;
                }

                //if(newBall.GetComponent<BallHit>().secondTouch)
                //{
                //    yield return new WaitForSeconds(7);
                //}
                //else
                //{
                //    yield return new WaitForSeconds(2);
                //}
                yield return new WaitForSeconds(7);
                sideCam.depth = -2;
                sideCam.enabled = false;
                StartCoroutine(ResetBall(newBall));
                overText.text = overs + "." + ballsLaunched;
                yield return new WaitForSeconds(1);
            }
            yield return null;
        }
    }

    void UpdateScoreBoard()
    {

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

    public void Out()
    {
        isGameOver = true;
        lostPanel.SetActive(true);
        pauseBtn.SetActive(false);
        //wicketFx.Play();
        //crowdFx.Play();
        VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Failure);
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
