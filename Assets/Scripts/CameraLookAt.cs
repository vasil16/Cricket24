using UnityEngine;

public class CameraLookAt : MonoBehaviour
{

    public GameObject ball;
    public Vector3 defRotation;
    public bool readyToDeliver, startingRunUp;
    public float refWidth = 2280, activeScreenWidth, refSensorSize;
    bool goDown;
    float dampFact = 0;
    [SerializeField] float distanceThreshold, defFOV, currentDist, adjustedSensorX;
    [SerializeField] Vector2 activeCamSize;

    Camera cam;

    private void OnEnable()
    {
        transform.localRotation = Quaternion.Euler(defRotation);
        if (TryGetComponent<Camera>(out cam))
        {
            cam = GetComponent<Camera>();
        }
    }

    void Start()
    {
        if(cam)
        {
            defFOV = cam.fieldOfView;
            activeCamSize = cam.sensorSize;
            activeScreenWidth = Screen.width;
            adjustedSensorX = activeCamSize.x * Screen.width / refWidth;
            cam.sensorSize  = new Vector2((activeCamSize.x * Screen.width) / refWidth,activeCamSize.y) ;
        }
    }
    

    void Update()
    {
        if (Gameplay.instance && Gameplay.instance.isGameOver) this.enabled=false;
        if (ball == null)
        {
            if(MainGame.instance.camIndex==1 && startingRunUp)
            {
                CamRunUpAnim();
            }
            return;
        }

        currentDist = Vector3.Distance(transform.position, ball.transform.position);

        if (MainGame.instance.camIndex ==1)
        {
            if (ball.GetComponent<BallHit>().secondTouch)
            {
                //camera.fieldOfView += ball.GetComponent<Rigidbody>().velocity.magnitude * .2f * Time.deltaTime;
                if (ball.transform.position.x < -27.03f)
                {
                    cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 6, ref dampFact, 1f);
                }
                else
                {
                    if (cam.fieldOfView < 10.3f)
                    {
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 10.3f, ref dampFact, 1f);
                    }
                    else
                    {
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 9.1f, ref dampFact, 1f);
                    }
                }
            }
            //else
            //if (ball.transform.position.y > 80)
            //{
            //    camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 20, ref dampFact, 1f);
            //    goDown = true;
            //}
            //if (goDown)
            //{
            //    if (ball.transform.position.y < 60)
            //    {
            //        camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, defFOV, ref dampFact, 1.4f);
            //        if (Gameplay.instance.deliveryDead)
            //        {
            //            goDown = false;
            //            camera.fieldOfView = defFOV;
            //        }
            //    }
            //}

            if (Vector3.Distance(transform.position, ball.transform.position) > distanceThreshold && !ball.GetComponent<BallHit>().secondTouch && !Gameplay.instance.deliveryDead)
            {
                ////if(camera.fieldOfView>7.5f)
                ////{
                //    camera.fieldOfView -= .8f * Time.deltaTime;
                ////}
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 7.5f / 2, ref dampFact, 0.3f);
                cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, Quaternion.Euler(6.064f, -90, 0), 0.3f * Time.deltaTime);
            }
            else
            {                
                LookAt();
            }
        }

        else if(MainGame.instance.camIndex == 3)
        {
            transform.LookAt(ball.transform);
            
        }
        else
        {
            LookAt();
        }
    }

    void CamRunUpAnim()
    {
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, 3.651515f, ref dampFact, 1.5f);
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.rotation.eulerAngles.x, 6.1f,Time.deltaTime * 0.3f), -90, 0);
    }

    public void CamReset()
    {
        transform.localRotation = Quaternion.Euler(defRotation);
        if (!cam) return;
        cam.fieldOfView = defFOV;
        //this.enabled = false;
    }

    public void LookAt()
    {        
        cam.transform.LookAt(ball.transform);        
    }
}
