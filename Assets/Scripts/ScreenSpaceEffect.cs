using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenSpaceEffect : MonoBehaviour
{
    public float blankingWidth = 0.2f; // Percentage of the screen to blank (left and right)
    public float blankingHeight = 0.0f; // Percentage of the screen to blank (top and bottom)

    private Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    void OnPreCull()
    {
        ApplyBlankingEffect();
    }

    void ApplyBlankingEffect()
    {
        // Calculate the horizontal and vertical scale to apply based on blanking width and height
        float leftBlank = blankingWidth / 2.0f;
        float rightBlank = 1.0f - blankingWidth / 2.0f;
        float topBlank = blankingHeight / 2.0f;
        float bottomBlank = 1.0f - blankingHeight / 2.0f;

        // Modify the projection matrix to create a letterbox effect
        Matrix4x4 matrix = _camera.projectionMatrix;

        // Apply scaling to the matrix to create the blanked effect
        matrix.m00 *= rightBlank;  // Horizontal scaling (for blanking left-right)
        matrix.m11 *= bottomBlank; // Vertical scaling (for blanking top-bottom)

        // Apply the modified projection matrix to the camera
        _camera.projectionMatrix = matrix;
    }
}
