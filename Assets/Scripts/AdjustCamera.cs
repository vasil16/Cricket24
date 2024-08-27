using UnityEngine;

public class AdjustCamera : MonoBehaviour
{
    public float transitionSpeed = 2f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        AdjustOrthographicSize();
    }

    void Update()
    {
        // Optionally, adjust orthographic size continuously in Update for dynamic aspect ratio changes
        // AdjustOrthographicSize();
    }

    void AdjustOrthographicSize()
    {
        float currentAspectRatio = (float)Screen.width / Screen.height;
        float targetAspectRatio = mainCamera.aspect;
        float newOrthoSize = mainCamera.orthographicSize;

        if (currentAspectRatio < targetAspectRatio)
        {
            // Screen is narrower, decrease orthographic size
            newOrthoSize = mainCamera.orthographicSize * (targetAspectRatio / currentAspectRatio);
        }
        else if (currentAspectRatio > targetAspectRatio)
        {
            // Screen is wider, increase orthographic size
            newOrthoSize = mainCamera.orthographicSize / (currentAspectRatio / targetAspectRatio);
        }

        // Smoothly transition to the new orthographic size
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, newOrthoSize, Time.deltaTime * transitionSpeed);
    }
}
