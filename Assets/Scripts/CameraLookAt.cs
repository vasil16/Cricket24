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

    private void OnEnable()
    {
        instance = this;
        camera = GetComponent<Camera>();
        defRotation = camera.transform.rotation;

    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(ball == null)
        {
            //Debug.Log("ball Null");
            camera.transform.rotation = defRotation;
            return;
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
