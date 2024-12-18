using UnityEngine;
using System.Collections;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;
    public bool secondTouch , groundShot;

    [SerializeField] AudioSource soundFx;
    [SerializeField] AudioClip wicketFx, shotFx;

    public string lastHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!Gameplay.instance) return;

        if (!Gameplay.instance.stadiumBounds.Contains(transform.position))
        {
            Gameplay.instance.deliveryDead = true;
        }

        if (!secondTouch) return;
        if (transform.position.x > 80 && transform.position.z < 24)
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

        switch (collision.gameObject.tag)
        {
            case "Wicket":
                soundFx.PlayOneShot(wicketFx);
                Gameplay.instance.Out();
                break;

            case "pitch":
                if (secondTouch)
                {
                    groundShot = true;
                    rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.01f, rb.velocity.z);
                }
                break;

            case "Bat":
                Debug.Log("spot " + collision.gameObject.name);
                Gameplay.instance.bb.position = collision.GetContact(0).point;
                //StartCoroutine(waitAndLook());

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
                        // CameraShake.instance.followBall(this.gameObject);
                    }

                    soundFx.PlayOneShot(shotFx);
                    // VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Success);
                }
                break;

            case "Ground":
                if (secondTouch)
                {
                    groundShot = true;
                    // Vector3 vel = gameObject.GetComponent<Rigidbody>().velocity;
                    // gameObject.GetComponent<Rigidbody>().velocity = new Vector3(vel.x, vel.y * 1 / 4, vel.z);
                }
                break;

            case "boundary":
                Gameplay.instance.deliveryDead = true;
                break;

            default:
                // Handle any other tags if necessary
                break;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        Vector3 contactPoint = transform.position;
        CheckLegalDelivery(contactPoint);
        //Gameplay.instance.legalDelivery = false;
    }

    void CheckLegalDelivery(Vector3 enterPos)
    {
        if(enterPos.z is <= -4.71f or >=1.8f || enterPos.y > 2.96f)
        {
            Debug.Log("wideball");
            Gameplay.instance.legalDelivery = false;
        }
        else
        {
            Debug.Log("goodball");
            Gameplay.instance.legalDelivery = true;
        }
    }

    public void Reset()
    {
        lastHit = "";
        GetComponent<Rigidbody>().isKinematic = true;    
        secondTouch = false;
        groundShot = false;
    }

    //IEnumerator waitAndLook()
    //{
    //    yield return new WaitForSeconds(0.2f);
    //    foreach (CameraLookAt cam in Gameplay.instance.activeCams)
    //    {
    //        if (MainGame.instance.camIndex == 1)
    //            cam.ball = this.gameObject;
    //    }
    //}
}
