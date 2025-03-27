using UnityEngine;
using System.Collections;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;
    public bool secondTouch , groundShot, keeperReceive, fielderReached, boundary, stopTriggered;
    public GameObject fieldedPlayer;
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
                if (fielderReached) return;
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
                if (secondTouch)
                {
                    break;
                }
                FieldManager.hitBallPos = transform.position;
                FieldManager.hitVelocity = rb.velocity;
                Debug.Log("spot " + collision.gameObject.name);
                //Gameplay.instance.bb.position = collision.GetContact(0).point;
                //StartCoroutine(waitAndLook());                
                secondTouch = true;
                soundFx.PlayOneShot(shotFx);
                break;

            case "Ground":
                //if (secondTouch)
                //{
                //    groundShot = true;
                //}
                groundShot = true;
                break;

            case "boundary":
                stopTriggered = true;
                boundary = true;
                Gameplay.instance.deliveryDead = true;
                break;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag is "keeper")
        {
            keeperReceive = true;
            if (secondTouch)
            {
                //rb.isKinematic = true;
                fieldedPlayer = other.gameObject;
                fielderReached = true;
                return;
            }
            else
            {
                transform.position = other.transform.position;
                stopTriggered = true;
                rb.isKinematic = true;
            }
            
            keeperReceive = true;
        }
        else if(other.gameObject.tag is "fielder" or "DeepFielder")
        {
            if (secondTouch)
            {
                if(!fielderReached)
                {
                    rb.isKinematic = true;
                    fieldedPlayer = other.gameObject;
                    fielderReached = true;
                }
            }
        }
        else if (other.gameObject.tag is "stop")
        {
            transform.position = other.transform.position;
            stopTriggered = true;
            rb.isKinematic = true;
        }
        else if(other.gameObject.name is "overHead")
        {
            Vector3 contactPoint = transform.position;
            CheckLegalDelivery(contactPoint);
            //Gameplay.instance.legalDelivery = false;
        }
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
        keeperReceive = false;
        fielderReached = false;
        boundary = false;
        stopTriggered = false;
    }
}
