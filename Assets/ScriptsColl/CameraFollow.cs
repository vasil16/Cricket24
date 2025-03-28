using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the sprite's Transform

    void Update()
    {
        if (target != null)
        {
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }
}
