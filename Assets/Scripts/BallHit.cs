using UnityEngine;
using System.Collections;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;
    public bool secondTouch , groundShot;

    [SerializeField] AudioSource shotFx;

    public string lastHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!Gameplay.instance || MainGame.camIndex!=1) return;

        if (!Gameplay.instance.stadiumBounds.Contains(transform.position))
        {
            Gameplay.instance.deliveryDead = true;
        }

        if (transform.position.x > 80 && transform.position.z < 54)
        {
            Gameplay.instance.sideCam.depth = 0;
            Gameplay.instance.sideCam.enabled = true;
        }
        else
        {
            Gameplay.instance.sideCam.depth = -2;
            Gameplay.instance.sideCam.enabled = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        lastHit = collision.gameObject.tag;
        if(collision.gameObject.CompareTag("Wicket"))
        {
            Gameplay.instance.Out();
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
            Gameplay.instance.bb.position = collision.GetContact(0).point;
            StartCoroutine(waitAndLook());

            if (secondTouch)
            {
                return;
            }
            secondTouch = true;
            Rigidbody ballRigidbody = gameObject.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
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
            Gameplay.instance.deliveryDead = true;
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
        foreach (CameraLookAt cam in Gameplay.instance.activeCams)
        {
            if (MainGame.camIndex == 1)
                cam.ball = this.gameObject;
        }
    }
}
