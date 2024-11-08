using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{

    public static CameraLookAt instance;
    Camera camera;
    public GameObject ball;
    Quaternion defRotation;
    public bool readyToDeliver;
    [SerializeField] float distanceThreshold, defFOV;

    private void OnEnable()
    {
        instance = this;
        camera = GetComponent<Camera>();
        defRotation = camera.transform.rotation;
    }

    void Start()
    {
        defFOV = camera.fieldOfView;
    }

    // Update is called once per frame
    void Update()
    {
        if(ball == null)
        {
            //Debug.Log("ball Null");
            camera.transform.rotation = defRotation;
            camera.fieldOfView = defFOV;
            return;
        }
        if(Vector3.Distance(transform.position, ball.transform.position)<distanceThreshold)
        {
            camera.fieldOfView += ball.GetComponent<Rigidbody>().velocity.magnitude *  .2f * Time.deltaTime;
        }
        LookAt();
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
