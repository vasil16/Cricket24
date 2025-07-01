using UnityEngine;

public class CameraLookAt : MonoBehaviour
{

    public GameObject ball;
    public Vector3 defRotation;
    public bool readyToDeliver, startingRunUp;
    public float refWidth = 2280, activeScreenWidth, refSensorSize;
    float dampFact = 0f;
    [SerializeField] float distanceThreshold, defFOV, currentDist, adjustedSensorX, runUpTargetFov, deliverTargetFov;
    [SerializeField] Vector2 activeCamSize;

    Camera cam;

    private void OnEnable()
    {
        defRotation = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(defRotation);
        if (TryGetComponent<Camera>(out cam))
        {
            cam = GetComponent<Camera>();
        }
    }

    void Start()
    {
        cover = false;
        if (cam)
        {
            defFOV = cam.fieldOfView;
            activeCamSize = cam.sensorSize;

            // Use Screen.height and camera aspect ratio instead of Screen.width
            float screenAspect = (float)Screen.width / (float)Screen.height;
            float virtualScreenWidth = screenAspect * Screen.height;

            // Use virtualScreenWidth instead of Screen.width directly
            adjustedSensorX = activeCamSize.x / (virtualScreenWidth / refWidth);

            cam.sensorSize = new Vector2(adjustedSensorX, activeCamSize.y);
        }
    }

    public bool cover;

    void Update()
    {
        //if (Gameplay.instance && Gameplay.instance.isGameOver) this.enabled=false;              
        if (MainGame.instance.camIndex == 1)
        {
            if (startingRunUp)
            {
                CamRunUpAnim();
            }
            else if (readyToDeliver)
            {
                CamZoomIn();
            }
            if (BallHit.cover)
            {
                cam.transform.LookAt(ball.transform,Vector3.up);
            }

        }

        if (ball)
        {
            currentDist = Vector3.Distance(transform.position, ball.transform.position);

            if (MainGame.instance.camIndex == 1)
            {
                if (ball.GetComponent<BallHit>().secondTouch)
                {
                    if (Vector3.Distance(transform.position, ball.transform.position) > distanceThreshold)
                    {
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 7f, ref dampFact, 0.7f);
                    }
                    else if (Vector3.Distance(transform.position, ball.transform.position) < 160)
                    {
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 16f, ref dampFact, 0.2f);
                    }
                    else
                    {
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 8f, ref dampFact, 0.2f);
                    }
                    LookAt();
                }
            }

            else if (MainGame.instance.camIndex == 3)
            {
                transform.LookAt(ball.transform);

            }
            else
            {
                LookAt();
            }
        }
    }

    public void CamRunUpAnim()
    {        
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.rotation.eulerAngles.x, 8.5f, Time.deltaTime * 1f), -90, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, runUpTargetFov, ref dampFact, 1.7f);
    }

    public void CamZoomIn()
    {
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.rotation.eulerAngles.x, 6.3f, Time.deltaTime * 2f), -90, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, deliverTargetFov, ref dampFact, .3f);
    }

    public void CamReset()
    {
        transform.localRotation = Quaternion.Euler(defRotation);
        if (!cam) return;
        cam.fieldOfView = defFOV;
    }

    public void LookAt()
    {        
        cam.transform.LookAt(ball.transform);        
    }
}
