using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;

    private Transform cameraTransform;
    private Vector3 originalPosition;
    [SerializeField] CinemachineVirtualCamera followCam;

    public static CameraShake instance;

    void Awake()
    {
        cameraTransform = GetComponent<Transform>();
        instance = this;
    }

    public void Shake()
    {
        originalPosition = cameraTransform.localPosition;
        InvokeRepeating("DoShake", 0, 0.01f);
        Invoke("StopShake", shakeDuration);
    }

    void DoShake()
    {
        float offsetX = Random.value * shakeMagnitude * 2 - shakeMagnitude;
        float offsetY = Random.value * shakeMagnitude * 2 - shakeMagnitude;
        cameraTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
    }

    void StopShake()
    {
        CancelInvoke("DoShake");
        cameraTransform.localPosition = originalPosition;
    }

    public void Restart()
    {
        SceneManager.UnloadSceneAsync("Ciri 2");
        SceneManager.LoadScene("Ciri 2");
    }

    public void followBall(GameObject ball)
    {
        StartCoroutine(FollowRoutine(ball));
    }

    IEnumerator FollowRoutine(GameObject followBall)
    {
        if (followBall == null)
        {
            Debug.Log("null ball");
        }
        else if(followCam == null)
        {
            Debug.Log("null cam");
        }
        else
        {
            followCam.LookAt = followBall.transform;
            yield return new WaitForSeconds(1f);
            followCam.LookAt = null;
        }
    }
}
