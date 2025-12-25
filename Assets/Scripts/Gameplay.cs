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
    public CameraLookAt broadcastCamComp;


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

    //public List<Vector3> deliveryPoints = new List<Vector3>()
    //{
    //    // --- YORKERS (Targeting the Base of Stumps) ---
    //    new Vector3(-30.5f, -4.42f, -0.36f), // Perfect Middle Yorker
    //    new Vector3(-30.3f, -4.42f, 0.40f),  // Off-Stump Yorker
    //    new Vector3(-30.6f, -4.42f, -1.10f), // Leg-Stump Yorker
    //    new Vector3(-30.0f, -4.42f, 3.50f),  // Wide Yorker (Inside Wide Line)
    //    new Vector3(-29.8f, -4.42f, 5.50f),  // Extreme Wide Yorker
    //    new Vector3(-30.8f, -4.42f, -0.70f), // Cramp Yorker (Aimed at toes)

    //    // --- SLOT BALLS (High Boundary Risk - Half Volleys) ---
    //    new Vector3(-28.5f, -4.42f, -0.36f), // Classic Middle Slot
    //    new Vector3(-27.5f, -4.42f, 1.00f),  // Off-Drive Half-Volley
    //    new Vector3(-28.0f, -4.42f, -1.50f), // Leg-Side Clip
    //    new Vector3(-27.0f, -4.42f, 2.80f),  // Wide Drive Bait
    //    new Vector3(-27.8f, -4.42f, 0.60f),  // Overpitched Off-Stump
    //    new Vector3(-29.0f, -4.42f, -0.10f), // The "Floaty" Half-Volley

    //    // --- GOOD LENGTH (The "Corridor of Uncertainty") ---
    //    new Vector3(-26.0f, -4.42f, -0.10f), // Top of Off
    //    new Vector3(-25.5f, -4.42f, 0.80f),  // 4th Stump Channel
    //    new Vector3(-25.2f, -4.42f, 1.60f),  // 5th Stump (Test match line)
    //    new Vector3(-25.8f, -4.42f, -0.36f), // Straight Good Length
    //    new Vector3(-26.5f, -4.42f, -0.90f), // Tight into the Pads
    //    new Vector3(-24.8f, -4.42f, 2.50f),  // Wide Slanting Angle
    //    new Vector3(-24.5f, -4.42f, 0.40f),  // Deep Good Length (Dry spell line)

    //    // --- BACK OF LENGTH (Defensive / Heavy Ball) ---
    //    new Vector3(-23.0f, -4.42f, -0.50f), // Heavy Ball (Rib-cage height)
    //    new Vector3(-22.5f, -4.42f, 1.20f),  // Back of Length Outside Off
    //    new Vector3(-23.5f, -4.42f, -1.00f), // Short of Length Body Cramp
    //    new Vector3(-22.0f, -4.42f, 0.00f),  // Defensive Middle

    //    // --- FULL TOSSES (Targets in the air at the Crease) ---
    //    new Vector3(-31.08f, -4.10f, -0.36f), // Low Full Toss Middle
    //    new Vector3(-31.08f, -3.50f, 1.20f),  // Waist High Off-Side (Risky)
    //    new Vector3(-31.08f, -3.80f, 4.80f),  // Wide Full Toss (T20 Death ball)
    //    new Vector3(-31.08f, -4.00f, -1.80f), // Leg Side Full Toss

    //    // --- SHORT PITCH (Occasional Bouncers) ---
    //    new Vector3(-20.0f, -4.42f, -0.36f), // Standard Short Ball
    //    new Vector3(-19.5f, -4.42f, 2.50f),  // Wide Bouncer
    //    new Vector3(-18.5f, -4.42f, 0.20f),  // Chest Height Bouncer
    //    new Vector3(-17.5f, -4.42f, -0.20f)  // Throat Ball
    //};

    public List<Vector3> deliveryPoints = new List<Vector3>()
    {
        // --- YORKERS (Base of Stumps) ---
        new Vector3(-30.2f, -4.42f, -0.36f), // Middle Stump Yorker
        new Vector3(-30.0f, -4.42f, 0.60f),  // Off-Stump Yorker
        new Vector3(-29.8f, -4.42f, 3.50f),  // Wide Yorker (T20 Style)

        // --- SLOT BALLS (In the "Arc") ---
        new Vector3(-28.5f, -4.42f, -0.36f), // Middle Slot
        new Vector3(-28.0f, -4.42f, 1.00f),  // Off Slot
        new Vector3(-28.2f, -4.42f, -1.20f), // Leg-ish Slot

        // --- GOOD LENGTH (Standard) ---
        new Vector3(-25.5f, -4.42f, 0.00f),  // Top of Off
        new Vector3(-25.0f, -4.42f, 0.80f),  // 4th Stump
        new Vector3(-26.0f, -4.42f, -0.80f), // At the Body

        // --- FULL TOSS / BEAMER TARGETS (Y is higher here) ---
        new Vector3(-31.08f, -3.80f, 0.00f), // Low Full Toss
        new Vector3(-31.08f, -3.20f, 1.50f)  // Wide Full Toss
    };

    public float scc;

    void Start()
    {
        ballerTrueScale = bowler.transform.localScale;
        ShiftEnd();
        //activeCams = FindObjectsOfType<CameraLookAt>();
        stadiumBounds = groundBounds.GetComponent<Renderer>().bounds;
        bowlerPalm = ball.transform.parent;
        ballOriginPoint = ball.transform.localPosition;
        ballScale = ball.transform.localScale;
        StartCoroutine(LaunchBallsWithDelay());
    }

    [SerializeField] bool randomBound;

    IEnumerator LaunchBallsWithDelay()
    {
        while (overs < 5 && !isGameOver)
        {
            yield return new WaitForSeconds(0.2f);

            deliveryDead = false;

            ball.transform.SetParent(bowlerPalm);

            ball.transform.localScale = ballScale;

            //Vector3 ballPitchPoint = randomBound == true? GetRandomPointWithinBounds() : pitchPoints[ballDeliverType].points[Random.Range(0, 10)];

            Vector3 ballPitchPoint = randomBound == true ? shareRand() : pitchPoints[ballDeliverType].points[Random.Range(0, 10)];
            

            ball.transform.localPosition = ballOriginPoint;

            ball.transform.rotation = Quaternion.Euler(-90, 0, 0);

            Vector3 direction = (ballPitchPoint - ballLaunchPos).normalized;

            float speed = 146.8f;

            Vector3 force = direction * speed;

            ball.GetComponent<BallHit>().stopTriggered = false;

            batter.batterAnim.SetTrigger("ToStance");

            yield return new WaitForSeconds(7f);            

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

            //mark.transform.position = ballPitchPoint;

            mark.transform.position = BowlingEngine.instance.DecidePoint(false);

            yield return new WaitForSeconds(1f);
            broadcastCamComp.startingRunUp = true;
            rb = ball.GetComponent<Rigidbody>();
            yield return new WaitUntil(() => readyToBowl);
            broadcastCamComp.startingRunUp = false;
            broadcastCamComp.readyToDeliver = true;
            ball.transform.SetParent(null,true);
            ball.transform.position = ballLaunchPos;
            rb.isKinematic = true;
            currentBall = ball.transform;
            rb.isKinematic = false;
            ball.SetActive(true);
            

            readyToBowl = false;

            //----------------------------------
            {
                //Vector3 toTarget = ballPitchPoint - ballLaunchPos;
                //Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
                //float y = toTarget.y; // This should be negative if the target is below
                //float xz = toTargetXZ.magnitude;
                //float gravity = Mathf.Abs(Physics.gravity.y);
                //float speedSquared = speed * speed;

                //float discriminant = speedSquared * speedSquared - gravity * (gravity * xz * xz + 2 * y * speedSquared);

                ////Debug.Log($"y = {y}, xz = {xz}, speed = {speed}, discriminant = {discriminant}");

                //if (discriminant < 0f)
                //{
                //    yield break; // Exit early
                //}

                //float discRoot = Mathf.Sqrt(discriminant);

                //float angle = Mathf.Atan2(speedSquared - discRoot, gravity * xz);

                //Vector3 velocity = toTargetXZ.normalized * Mathf.Cos(angle) * speed;
                //velocity.y = Mathf.Sin(angle) * speed;

                //rb.velocity = Vector3.zero;
                //rb.angularVelocity = Vector3.zero;
                //rb.WakeUp();
                //rb.AddForce(velocity, ForceMode.VelocityChange);
                //rb.AddTorque(Vector3.forward * -10f, ForceMode.Impulse);
                //BowlingEngine.instance.ReleaseBall(false);
                BowlingEngine.instance.Release();
            }

            broadcastCamComp.ball = ball;

            foreach (CameraLookAt cam in activeCams)
            {
                if(!cam.enabled)
                    cam.enabled = true;
                cam.ball = ball;                
            }

            yield return new WaitForSeconds(.7f);                       

            yield return new WaitUntil(() => deliveryDead);

            ball.GetComponent<BallHit>().cover = false;

            broadcastCamComp.readyToDeliver = false;

            bowler.GetComponent<Animator>().SetBool("DeliveryComplete", true);

            yield return new WaitForSeconds(2);

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
            FieldManager.ResetFielder.Invoke();
            foreach (CameraLookAt cam in activeCams)
            {
                cam.ball = null;
                cam.CamReset();
            }

            yield return new WaitForSeconds(.5f);
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

    Vector3 shareRand()
    {
        int kd = Random.Range(0, deliveryPoints.Count);
        return deliveryPoints[kd];
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

    Vector3 GetRandomCricketDeliveryPoint()
    {
        float groundY = -4.43f;

        // X: Length from batter (~ -6 = yorker, ~ -30 = short ball)
        float minX = -30f;
        float maxX = -6f;

        // Z: Line (off to leg side)
        float minZ = -1.8f; // wide outside off
        float maxZ = 1.0f;  // deep leg side (rare but happens)

        // Random point within realistic cricket pitch zone
        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);

        return new Vector3(x, groundY, z);
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

    public bool loftOn;
    public UnityEngine.UI.Image loftBtn;

    public void Loft()
    {
        loftOn = loftOn ? false : true;
        loftBtn.color = loftOn ? Color.blue : Color.white;
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
