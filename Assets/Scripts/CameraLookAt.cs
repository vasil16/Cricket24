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

            // Use Screen.height and camera aspect ratio instead of Screen.width
            float screenAspect = (float)Screen.width / (float)Screen.height;
            float virtualScreenWidth = screenAspect * Screen.height;

            // Use virtualScreenWidth instead of Screen.width directly
            adjustedSensorX = activeCamSize.x / (virtualScreenWidth / refWidth);

            cam.sensorSize = new Vector2(adjustedSensorX, activeCamSize.y);
        }
    }

    public bool cover;

    void LateUpdate()
    {
        if(this.gameObject.name=="draw1")
        {            
            if (startingRunUp)
            {
                Debug.Log("drawr");
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 3.8f, ref dampFact, 1f);
            }
            return;
        }
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
            //if (ball && ball.GetComponent<BallHit>().cover && !ball.GetComponent<BallHit>().secondTouch)
            //{
            //    Debug.Log("cover");
            //    cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, .7f, ref dampFact, .5f);
            //    Vector3 dir = ball.transform.position - transform.position;

            //    // vertical angle only
            //    float targetPitch = Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;

            //    float smoothPitch = Mathf.LerpAngle( transform.eulerAngles.x, targetPitch, Time.deltaTime * 3f );

            //    transform.eulerAngles = new Vector3( smoothPitch, transform.eulerAngles.y, transform.eulerAngles.z );
            //}

            if (ball && ball.GetComponent<BallHit>().cover && !ball.GetComponent<BallHit>().secondTouch)
            {
                Debug.Log("cover"); cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, .7f, ref dampFact, .5f);
                Vector3 direction = (ball.transform.position - transform.position).normalized;
                Vector3 currentEuler = transform.eulerAngles; float targetPitch = Quaternion.LookRotation(direction, Vector3.right).eulerAngles.x;
                float smoothPitch = Mathf.LerpAngle(currentEuler.x, targetPitch, Time.deltaTime * 3);
                transform.eulerAngles = new Vector3(smoothPitch, currentEuler.y, currentEuler.z);
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

    public float runUpTargetRotation, deliverTargetRotaion;

    public void CamRunUpAnim()
    {
        transform.localRotation = Quaternion.Euler(Mathf.Lerp(transform.eulerAngles.x, runUpTargetRotation, Time.deltaTime * .61f), 0, 0);
        //transform.eulerAngles = new Vector3(Mathf.Lerp(transform.eulerAngles.x, runUpTargetRotation, Time.deltaTime * .61f), 0, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, runUpTargetFov, ref dampFact, 3f);
    }

    public void CamZoomIn()
    {
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.eulerAngles.x, deliverTargetRotaion, Time.deltaTime * 1.8f), 0, 0);
        //transform.eulerAngles = new Vector3(Mathf.Lerp(transform.eulerAngles.x, deliverTargetRotaion, Time.deltaTime * 1.8f), 0, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, deliverTargetFov, ref dampFact, .25f);
    }

    public void CamReset()
    {
        startingRunUp = false;
        readyToDeliver = false;
        ball = null;
        transform.eulerAngles = defRotation;
        if (!cam) return;
        cam.fieldOfView = defFOV;
    }

    public void LookAt()
    {        
        cam.transform.LookAt(ball.transform);        
    }
}
