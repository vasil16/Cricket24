using UnityEngine;

[ExecuteAlways]
public class MatchCameraToFit : MonoBehaviour
{
    public float targetAspectRatio = 16f / 9f; // Aspect ratio of your reference resolution (1920x1080)
    public float orthoSize = 5f; // For orthographic cameras
    public float fieldOfView; // For perspective cameras

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        MatchCamera();
    }

    private void OnValidate()
    {        
        MatchCamera();
    }

    private void MatchCamera()
    {
        if (cam == null) return;

        fieldOfView = cam.fieldOfView;

        float currentAspectRatio = (float)Screen.width / Screen.height;

        if (cam.orthographic)
        {
            float matchSize = orthoSize;

            if (currentAspectRatio > targetAspectRatio)
            {
                // Pillarbox (add bars on the sides)
                float scale = targetAspectRatio / currentAspectRatio;
                matchSize *= scale;
            }
            else
            {
                // Letterbox (add bars on the top and bottom)
                float scale = currentAspectRatio / targetAspectRatio;
                matchSize /= scale;
            }

            cam.orthographicSize = matchSize;
        }
        else
        {
            if (currentAspectRatio > targetAspectRatio)
            {
                // Pillarbox (wider screens, adjust FOV to maintain horizontal view)
                float scale = targetAspectRatio / currentAspectRatio;
                cam.fieldOfView = Mathf.Atan(Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f) * scale) * 2f * Mathf.Rad2Deg;
            }
            else
            {
                // Letterbox (taller screens, no FOV adjustment needed)
                cam.fieldOfView = fieldOfView;
            }
        }
    }
}
