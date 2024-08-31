using UnityEngine;

[ExecuteInEditMode]
public class CameraHeightAdjuster : MonoBehaviour
{
    public Camera perspectiveCamera; // Reference to the perspective camera
    public float desiredHeight = 10f; // Desired height in world units
    public float cameraDistance = 20f; // Distance from the camera to the target plane

    void Start()
    {
        AdjustCameraHeight();
    }

    void Update()
    {
        // Ensure the camera height is adjusted even in Edit mode
        AdjustCameraHeight();
    }

    void AdjustCameraHeight()
    {
        // Ensure the camera reference is set
        if (perspectiveCamera == null)
        {
            perspectiveCamera = GetComponent<Camera>();
        }

        // Get the current aspect ratio
        float aspectRatio = (float)Screen.width / (float)Screen.height;

        // Calculate the required field of view (FOV) for the desired height
        float fov = 2f * Mathf.Atan(desiredHeight / (2f * cameraDistance)) * Mathf.Rad2Deg;

        // Set the camera's field of view
        perspectiveCamera.fieldOfView = fov;

        // Adjust the camera position to maintain the desired distance
        Vector3 cameraPosition = perspectiveCamera.transform.position;
        cameraPosition.z = -cameraDistance; // Assuming the camera is looking down the Z-axis
        perspectiveCamera.transform.position = cameraPosition;
    }

    void OnValidate()
    {
        // Adjust the camera height in the editor when values are changed
        AdjustCameraHeight();
    }
}
