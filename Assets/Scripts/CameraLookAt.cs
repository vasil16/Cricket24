using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
    public GameObject ball;
    public Vector3 defRotation;
    public bool readyToDeliver, startingRunUp;
    public float refWidth = 2280, activeScreenWidth, refSensorSize;

    // -- SMOOTHING VARIABLES --
    [Header("Broadcast Settings")]
    [SerializeField] float lookDamping = 5f;        // Lower = smoother/slower tracking
    [SerializeField] float bounceDamping = 0.3f;      // Higher = ignores bouncing more
    private float currentYVelocity;                   // Internal ref for Y smoothing
    private float stabilizedY;                        // The smoothed Y position
    private bool isTrackingInitialized = false;       // To snap to position on first frame

    float dampFact = 0f; // Used for FOV smoothing
    [SerializeField] float distanceThreshold, defFOV, currentDist, adjustedSensorX, runUpTargetFov, deliverTargetFov;
    [SerializeField] Vector2 activeCamSize;

    Camera cam;

    public bool cover;
    public float runUpTargetRotation, deliverTargetRotaion;

    private void OnEnable()
    {
        defRotation = transform.eulerAngles;
        if (TryGetComponent<Camera>(out cam))
        {
            cam = GetComponent<Camera>();
        }
    }

    void Start()
    {
        transform.rotation = Quaternion.Euler(defRotation);
        cover = false;
        if (cam)
        {
            defFOV = cam.fieldOfView;
            activeCamSize = cam.sensorSize;

            float screenAspect = (float)Screen.width / (float)Screen.height;
            float virtualScreenWidth = screenAspect * Screen.height;
            adjustedSensorX = activeCamSize.x / (virtualScreenWidth / refWidth);
            cam.sensorSize = new Vector2(adjustedSensorX, activeCamSize.y);
        }
    }

    void LateUpdate()
    {
        // 1. Draw Camera Logic (Unchanged)
        if (this.gameObject.name == "draw1")
        {
            if (startingRunUp)
            {
                Debug.Log("drawr");
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 3.8f, ref dampFact, 1f);
            }
            return;
        }

        // 2. Main Game Camera Logic
        if (MainGame.instance.camIndex == 1)
        {
            if (startingRunUp)
            {
                CamRunUpAnim();
                return; // Exit to prevent fighting with ball tracking
            }
            else if (readyToDeliver)
            {
                CamZoomIn();
                return; // Exit to prevent fighting with ball tracking
            }

            // -- BROADCAST TRACKING LOGIC --
            if (ball)
            {
                Debug.Log("yes cover");
                var ballHit = ball.GetComponent<BallHit>();
                currentDist = Vector3.Distance(transform.position, ball.transform.position);

                // A. Second Touch (Standard Play)
                if (ballHit.secondTouch)
                {
                    float targetFovVal = 8f;
                    float fovSmoothTime = 0.2f;

                    if (currentDist > distanceThreshold)
                    {
                        targetFovVal = 7f; fovSmoothTime = 0.7f;
                    }
                    else if (currentDist < 160)
                    {
                        targetFovVal = 16f; fovSmoothTime = 0.2f;
                    }

                    cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFovVal, ref dampFact, fovSmoothTime);

                    // USE THE NEW SMOOTH LOOK
                    BroadcastLookAt(ball.transform.position);
                }
                // B. Cover Drive (Special Action)
                else if (ballHit.cover)
                {
                    Debug.Log("cover (Play & Miss / Leave)");

                    // FOV Logic
                    cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, .7f, ref dampFact, .5f);

                    // --- FAST TRACKING FIX ---
                    // Speed: 20f (Very fast, almost instant but still smooth)
                    // Damp: 0.05f (Very sensitive to Y movement, so we see the ball dip to the keeper)
                    BroadcastLookAt(ball.transform.position, 20f, 0.05f);
                }
                else
                {
                    Debug.Log("cs");
                    // Fallback if neither condition is met but ball exists
                    BroadcastLookAt(ball.transform.position);
                }
            }
        }
        // 3. Other Camera Indices
        else if (MainGame.instance.camIndex == 3 && ball)
        {
            // Keep instant tracking for Cam 3 if desired, or switch to BroadcastLookAt
            transform.LookAt(ball.transform);
        }
        else if (ball)
        {
            // Default fallback
            transform.LookAt(ball.transform);
        }
    }

    // --- THE MAGIC SAUCE: SMOOTH BROADCAST LOOK ---
    // Updated function with optional override parameters
    // defaultSpeed = -1 means "use the global variable"
    void BroadcastLookAt(Vector3 targetPos, float overrideSpeed = -1f, float overrideBounceDamp = -1f)
    {
        // 1. Determine which settings to use (Default vs Override)
        float currentLookSpeed = (overrideSpeed > 0) ? overrideSpeed : lookDamping;
        float currentBounceDamp = (overrideBounceDamp >= 0) ? overrideBounceDamp : bounceDamping;

        // Initialize tracking if needed
        if (!isTrackingInitialized)
        {
            stabilizedY = targetPos.y;
            isTrackingInitialized = true;
        }

        // 2. Stabilize Y (Use lower damping for keeper action to track height accurately)
        stabilizedY = Mathf.SmoothDamp(stabilizedY, targetPos.y, ref currentYVelocity, currentBounceDamp);

        Vector3 stabilizedTarget = new Vector3(targetPos.x, stabilizedY, targetPos.z);
        Vector3 direction = stabilizedTarget - transform.position;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);

            // 3. Apply Rotation with the chosen speed
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * currentLookSpeed);
        }
    }

    public void CamRunUpAnim()
    {
        isTrackingInitialized = false; // Reset tracking so it snaps correctly next time
        transform.localRotation = Quaternion.Euler(Mathf.Lerp(transform.eulerAngles.x, runUpTargetRotation, Time.deltaTime * .61f), 0, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, runUpTargetFov, ref dampFact, 3f);
    }

    public void CamZoomIn()
    {
        isTrackingInitialized = false;
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.eulerAngles.x, deliverTargetRotaion, Time.deltaTime * 1.8f), 0, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, deliverTargetFov, ref dampFact, .25f);
    }

    public void CamReset()
    {
        startingRunUp = false;
        readyToDeliver = false;
        ball = null;
        isTrackingInitialized = false; // Important: Reset smoothing
        transform.eulerAngles = defRotation;
        if (!cam) return;
        cam.fieldOfView = defFOV;
    }

    // Legacy function kept just in case, but unused in CamIndex 1 now
    public void LookAt()
    {
        if (ball) cam.transform.LookAt(ball.transform);
    }
}