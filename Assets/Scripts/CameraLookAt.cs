using UnityEngine;

public class CameraLookAt : MonoBehaviour
{

    public GameObject ball;
    public Vector3 defRotation;
    public bool readyToDeliver, startingRunUp;
    public float refWidth = 2280, activeScreenWidth, refSensorSize;
    float dampFact = 0f;
    [SerializeField] float distanceThreshold, defFOV, currentDist, adjustedSensorX;
    [SerializeField] Vector2 activeCamSize;

    Camera cam;

    private void OnValidate()
    {
        defRotation = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(defRotation);
        if (TryGetComponent<Camera>(out cam))
        {

        }
    }

    void Start()
    {
        if (cam)
        {
            defFOV = cam.fieldOfView;
            activeCamSize = cam.sensorSize;
            activeScreenWidth = Screen.width;

            adjustedSensorX = activeCamSize.x * (Screen.width / refWidth);

            cam.sensorSize = new Vector2(adjustedSensorX, activeCamSize.y);
        }
    }


    void Update()
    {
        //if (Gameplay.instance && Gameplay.instance.isGameOver) this.enabled=false;              
        if (MainGame.instance.camIndex == 1)
        {
            if (startingRunUp)
            {
                CamRunUpAnim();
            }
            if (readyToDeliver)
            {
                CamZoomIn();
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
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 6f, ref dampFact, 0.7f);
                    }
                    else
                    {
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 6.5f, ref dampFact, 0.2f);
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
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.rotation.eulerAngles.x, 6.3f, Time.deltaTime * 1f), -90, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 5f, ref dampFact, 1.7f);
    }

    public void CamZoomIn()
    {
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.rotation.eulerAngles.x, 5.68f, Time.deltaTime * 2f), -90, 0);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 4f, ref dampFact, .3f);
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
