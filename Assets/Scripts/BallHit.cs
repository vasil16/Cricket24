using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;

    Vector2 hitVelocity;

    Vector2 touchVelocity;
    float shotAngle;
    public bool secondTouch , groundShot;

    [SerializeField] AudioSource shotFx;

    public string lastHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!Pusher.instance || MainGame.camIndex!=1) return;

        if (!Pusher.instance.stadiumBounds.Contains(transform.position))
        {
            Pusher.instance.deliveryDead = true;
        }

        if (transform.position.x > 80 && transform.position.z < 54)
        {
            Pusher.instance.sideCam.depth = 0;
            Pusher.instance.sideCam.enabled = true;
        }
        else
        {
            Pusher.instance.sideCam.depth = -2;
            Pusher.instance.sideCam.enabled = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        lastHit = collision.gameObject.tag;
        if(collision.gameObject.CompareTag("Wicket"))
        {
            Pusher.instance.Out();
        }
        else if(collision.gameObject.CompareTag("pitch"))
        {
            if (secondTouch)
            {
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y*0.01f, rb.velocity.z);
            }
        }
        else if (collision.gameObject.CompareTag("Bat"))
        {
            Debug.Log("spot " + collision.gameObject.name);
            Pusher.instance.bb.position = collision.GetContact(0).point;
            StartCoroutine(waitAndLook());

            if (secondTouch)
            {
                return;
            }
            secondTouch = true;
            Rigidbody ballRigidbody = gameObject.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                //Vector2 direction = collision.contacts[0].point - transform.position;
                hitVelocity = ballRigidbody.velocity;

                //StartCoroutine(Score());

                if (CameraShake.instance != null)
                {
                    CameraShake.instance.Shake();
                    //CameraShake.instance.followBall(this.gameObject);                   
                }
                shotFx.Play();
                //VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Success);                
            }
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            if (secondTouch)
            {
                groundShot = true;
                //Vector3 vel = gameObject.GetComponent<Rigidbody>().velocity;
                //gameObject.GetComponent<Rigidbody>().velocity = new Vector3(vel.x, vel.y * 1 / 4, vel.z);
            }
        }

        else if (collision.gameObject.CompareTag("boundary"))
        {
            Pusher.instance.deliveryDead = true;
        }

    }

    public void Reset()
    {
        lastHit = "";
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;        
        secondTouch = false;
        groundShot = false;
    }

    IEnumerator waitAndLook()
    {
        yield return new WaitForSeconds(0.2f);
        //if (CameraLookAt.instance != null && MainGame.camIndex == 1)
        //{
        //    CameraLookAt.instance.ball = this.gameObject;
        //}

        foreach (CameraLookAt cam in Pusher.instance.activeCams)
        {
            if (MainGame.camIndex == 1)
                cam.ball = this.gameObject;
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
