using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{

    public static CameraLookAt instance;
    Camera camera;
    public GameObject ball;
    public Quaternion defRotation;
    public bool readyToDeliver;
    [SerializeField] float distanceThreshold, defFOV, currentDist;

    private void OnEnable()
    {
        instance = this;
        camera = GetComponent<Camera>();
        defRotation = camera.transform.localRotation;
    }

    void Start()
    {
        defFOV = camera.fieldOfView;
    }

    bool goDown;
    float dampFact = 0;

    // Update is called once per frame
    void Update()
    {
        if(ball == null)
        {
            //Debug.Log("ball Null");
            if(MainGame.camIndex!=3)
            {
                camera.transform.rotation = defRotation;
                camera.fieldOfView = defFOV;
            }
            return;
        }
        currentDist = Vector3.Distance(transform.position, ball.transform.position);

        if (MainGame.camIndex==1)
        {

            //if (Vector3.Distance(transform.position, ball.transform.position) < 200)
            if(ball.GetComponent<BallHit>().secondTouch)
            {
                //camera.fieldOfView += ball.GetComponent<Rigidbody>().velocity.magnitude * .2f * Time.deltaTime;
                if(ball.transform.position.x< -27.03f)
                {
                    camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 6, ref dampFact, 0.6f);
                }
                else
                {
                    if(camera.fieldOfView<10.3f)
                    {
                        camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 10.3f, ref dampFact, 1f);
                    }
                    else
                    {
                        transform.LookAt(FieldManager.bestFielder.transform);
                        camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 9.1f, ref dampFact, 1f);
                    }
                }
            }
            //else
            if (ball.transform.position.y > 80)
            {
                camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 20, ref dampFact, 1f);
                goDown = true;
            }
            if (goDown)
            {
                if (ball.transform.position.y < 60)
                {
                    camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, defFOV, ref dampFact, 1.4f);
                    if (Gameplay.instance.deliveryDead)
                    {
                        goDown = false;
                        camera.fieldOfView = defFOV;
                    }
                }
            }

            if (Vector3.Distance(transform.position, ball.transform.position) > distanceThreshold && !ball.GetComponent<BallHit>().secondTouch && !Gameplay.instance.deliveryDead)
            {
                ////if(camera.fieldOfView>7.5f)
                ////{
                //    camera.fieldOfView -= .8f * Time.deltaTime;
                ////}
                camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, 7f/2, ref dampFact, 0.3f);
                camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, Quaternion.Euler(6.064f, -90, 0), 0.3f * Time.deltaTime);
            }
            else
            {                
                LookAt();
            }
        }

        else if(MainGame.camIndex == 3)
        {
            transform.parent.LookAt(ball.transform);
            //LookAt();
        }


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
