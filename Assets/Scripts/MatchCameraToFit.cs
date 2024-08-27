using UnityEngine;

public class MatchCameraToFit : MonoBehaviour
{
    public float targetAspectRatio = 16f / 9f; // Aspect ratio of your reference resolution (1920x1080)
    public float orthoSize;

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

        float currentAspectRatio = (float)Screen.width / Screen.height;

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
}
