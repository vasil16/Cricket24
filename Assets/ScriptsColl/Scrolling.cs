using UnityEngine;

public class Scrolling : MonoBehaviour
{
    public float scrollSpeed = 1.0f; // Adjust scrolling speed as needed
    public float offset = 20.0f; // Adjust based on the size of your background
    public float startXPosition = -36.0f; // Set the desired starting x-position

    private void Update()
    {
        float newPosition = Mathf.Repeat(Time.time * scrollSpeed, offset);
        transform.position = new Vector2(startXPosition + newPosition, transform.position.y);
    }
}
