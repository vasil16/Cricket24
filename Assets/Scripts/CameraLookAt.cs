using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{

    public static CameraLookAt instance;
    Camera camera;
    public GameObject ball;
    public Quaternion defRotation;
    public bool readyToDeliver, startingRunUp;
    [SerializeField] float distanceThreshold, defFOV, currentDist, adjustedSensorX;
    public float refWidth = 2280, activeScreenWidth, refSensorSize;
    [SerializeField] Vector2 activeCamSize;

    private void OnEnable()
    {
        instance = this;
        if (TryGetComponent<Camera>(out camera))
        {
            camera = GetComponent<Camera>();
        }
        defRotation = transform.rotation;
    }

    void Start()
    {
        if(camera)
        {
            defFOV = camera.fieldOfView;
            activeCamSize = camera.sensorSize;
            activeScreenWidth = Screen.width;
            adjustedSensorX = activeCamSize.x * Screen.width / refWidth;
            camera.sensorSize  = new Vector2((activeCamSize.x * Screen.width) / refWidth,activeCamSize.y) ;
        }
    }

    bool goDown;
    float dampFact = 0;

    void Update()
    {
        if(ball == null)
        {
            if(MainGame.instance.camIndex==1 && startingRunUp)
            {
                CamRunUpAnim();
            }
            return;
        }
        else
        {
            //CamReset();
        }
        currentDist = Vector3.Distance(transform.position, ball.transform.position);

        if (MainGame.instance.camIndex ==1)
        {

            //if (Vector3.Distance(transform.position, ball.transform.position) < 200)
            if (ball.GetComponent<BallHit>().secondTouch)
            {
                //camera.fieldOfView += ball.GetComponent<Rigidbody>().velocity.magnitude * .2f * Time.deltaTime;
                if (ball.transform.position.x < -27.03f)
                {
                    camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 6, ref dampFact, 0.6f);
                }
                else
                {
                    if (camera.fieldOfView < 10.3f)
                    {
                        camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 10.3f, ref dampFact, 1f);
                    }
                    else
                    {
                        camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 9.1f, ref dampFact, 1f);
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
                camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 7.5f / 2, ref dampFact, 0.3f);
                camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, Quaternion.Euler(6.064f, -90, 0), 0.3f * Time.deltaTime);
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
        camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, camera.fieldOfView-0.7f, ref dampFact, 1.5f);
        transform.rotation = Quaternion.Euler(Mathf.Lerp(transform.rotation.eulerAngles.x, 6.4f,Time.deltaTime * 0.3f), -90, 0);
    }

    public void CamReset()
    {
        transform.rotation = defRotation;
        if (!camera) return;
        camera.fieldOfView = defFOV;
    }

    public void LookAt()
    {
        camera.transform.LookAt(ball.transform);        
    }

    public bool Ready()
    {
        return readyToDeliver;
    }
}
