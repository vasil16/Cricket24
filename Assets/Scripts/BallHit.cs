using UnityEngine;
using System.Collections;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;

    Vector2 hitVelocity;

    Vector2 touchVelocity;
    float shotAngle;

    bool secondTouch;

    [SerializeField] AudioSource shotFx;

    private void Update()
    {
        //transform.position = new Vector3(transform.position.x, transform.position.y, -0.37f);
        //transform.position = new Vector3(transform.position.x, transform.position.y, Pusher.instance.batTrans.position.z);
        touchVelocity = gameObject.GetComponent<Rigidbody>().velocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Wicket"))
        {
            Pusher.instance.Out();
        }
        else if (collision.gameObject.CompareTag("Bat"))
        {
            StartCoroutine(waitAndLook());

            if (secondTouch)
            {
                return;
            }
            Rigidbody ballRigidbody = gameObject.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                Vector2 direction = collision.contacts[0].point - transform.position;
                hitVelocity = ballRigidbody.velocity;

                StartCoroutine(Score());

                if (CameraShake.instance != null)
                {
                    CameraShake.instance.Shake();
                    //CameraShake.instance.followBall(this.gameObject);                   
                }
                shotFx.Play();
                VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Success);
                secondTouch = true;
            }
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            if (secondTouch)
            {
                groundShot = true;
                Vector3 vel = gameObject.GetComponent<Rigidbody>().velocity;
                gameObject.GetComponent<Rigidbody>().velocity = new Vector3(vel.x, vel.y * 1 / 4, vel.z);
            }
        }

    }
    bool groundShot;

    IEnumerator waitAndLook()
    {
        yield return new WaitForSeconds(0.2f);
        if (CameraLookAt.instance != null && MainGame.camIndex == 1)
        {
            CameraLookAt.instance.ball = this.gameObject;
        }
    }

    IEnumerator Score()
    {
        yield return new WaitForSeconds(1f);
        if (Pusher.instance.isGameOver) yield break;
        Vector2 lastPos = new Vector2 (gameObject.transform.position.x,gameObject.transform.position.y);
        //Debug.Log(lastPos);
        float shotAngle = Mathf.Atan2(lastPos.y, lastPos.x) * Mathf.Rad2Deg;
        //Debug.Log("angle   " + shotAngle);
        if(transform.position.x < -8.85f)
        {
            Scorer.instance.UpdateScore(-1, groundShot);
        }
        else
            Scorer.instance.UpdateScore(shotAngle,groundShot);
    }

}
