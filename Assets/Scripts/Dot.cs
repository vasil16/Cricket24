using UnityEngine;

public class Dot : MonoBehaviour
{
    [Header("Dot Settings")]
    public GameObject dotPrefab;      // Tiny cylinder or sphere prefab
    public int numberOfDots = 80;     // Number of dots along the circle
    public float dotScale = 0.3f;     // Diameter of each dot in meters
    public float dotHeight = 0.01f;   // Height/thickness of the dot

    [Header("Circle Settings")]
    public float radius = 27.43f;     // 30-yard circle radius
    public float yOffset = 0.02f;     // Height above ground

    void Start()
    {
        if (dotPrefab == null)
        {
            Debug.LogError("Assign a dot prefab!");
            return;
        }

        GenerateCircle();
    }

    public void GenerateCircle()
    {
        // Clear existing children (optional)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < numberOfDots; i++)
        {
            float angle = i * 360f / numberOfDots;
            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float z = Mathf.Sin(rad) * radius;

            GameObject dot = Instantiate(dotPrefab, transform);
            dot.transform.localPosition = new Vector3(x, yOffset, z);
            dot.transform.localRotation = Quaternion.identity;

            dot.transform.localScale = new Vector3(dotScale, dotHeight, dotScale);
        }
    }
}
