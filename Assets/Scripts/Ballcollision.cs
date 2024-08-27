using UnityEngine;
using System.Collections;

public class Ballcollision : MonoBehaviour
{
    Rigidbody rb;

    Vector2 hitVelocity;

    Vector2 touchVelocity;
    bool hitBat;

    private void Update()
    {
        touchVelocity = gameObject.GetComponent<Rigidbody2D>().velocity;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bat"))
        {
            hitBat = true;
            Rigidbody2D ballRigidbody = gameObject.GetComponent<Rigidbody2D>();
            if (ballRigidbody != null)
            {
                Vector2 direction = collision.contacts[0].point - (Vector2) transform.position;
                ballRigidbody.AddForce(direction * -500.0f, ForceMode2D.Impulse);
                hitVelocity = ballRigidbody.velocity;
                if (CameraShake.instance != null)
                {
                    CameraShake.instance.Shake();
                }
                VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.HeavyImpact);
            }
        }
        else
        if (collision.gameObject.CompareTag("Ground"))
        {
            Rigidbody2D ballRigidbody = gameObject.GetComponent<Rigidbody2D>();
            if (ballRigidbody != null)
            {
                Vector2 direction = collision.contacts[0].point - (Vector2) transform.position;
                if (hitVelocity == Vector2.zero)
                {
                    ballRigidbody.AddForce(touchVelocity * new Vector2(0.01f, -0.2f), ForceMode2D.Impulse);
                }
                else
                {
                    ballRigidbody.AddForce(hitVelocity * new Vector2(0.01f, -0.5f), ForceMode2D.Impulse);
                }
            }
        }
    }
}
