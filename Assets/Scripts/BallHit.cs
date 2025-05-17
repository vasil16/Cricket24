using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;
    public bool secondTouch , groundShot, keeperReceive, fielderReached, boundary, stopTriggered;
    public GameObject fieldedPlayer, shootMarker;
    public Vector3 pitchPoint, ballCatchPoint;
    [SerializeField] AudioSource soundFx;
    [SerializeField] AudioClip wicketFx, shotFx;
    [SerializeField] Fielder keeper;

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
                Gameplay.instance.deliveryDead = true;
                break;

            case "pitch":
                if (secondTouch)
                {
                    groundShot = true;
                    rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.04f, rb.velocity.z);
                }
                else
                {
                    pitchPoint = collision.contacts[0].point;
                    StartCoroutine(checkTrajectory());
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
                shootMarker.transform.position = PredictFallPosition(transform.position, FieldManager.hitVelocity, -4.44f);
                FieldManager.StartCheckField.Invoke(transform.position);
                break;

            case "Ground":
                groundShot = true;
                break;

            case "boundary":
                stopTriggered = true;
                boundary = true;
                Gameplay.instance.deliveryDead = true;
                break;
        }
    }

    public bool keeperExit;

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag is "keeper")
        {
            keeperExit = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag is "keeper" || (other.gameObject.tag is "rayTest" && other.transform.parent.tag is "keeper"))
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
                //transform.position = other.transform.position;
                //stopTriggered = true;
                //rb.isKinematic = true;
            }
        }

        else if(other.gameObject.tag is "fielder" or "DeepFielder")
        {
            if (secondTouch)
            {
                if(!fielderReached)
                {
                    //rb.isKinematic = true;
                    fieldedPlayer = other.transform.parent.gameObject;
                    fielderReached = true;
                }
            }
        }
        else if (other.gameObject.tag is "stop")
        {
            if (stopTriggered) return;
            if(!secondTouch)
            {
                Gameplay.instance.deliveryDead = true;
            }
            rb.isKinematic = true;
            transform.SetParent(other.transform, true);
            transform.position = other.transform.position;
            stopTriggered = true;
            //rb.isKinematic = false;
        }
        else if(other.gameObject.name is "overHead")
        {
            Vector3 contactPoint = transform.position;
            CheckLegalDelivery(contactPoint);
            //Gameplay.instance.legalDelivery = false;
        }
    }

    Vector3 PredictFallPosition(Vector3 startPos, Vector3 velocity, float groundY, float timeStep = 0.02f)
    {
        Vector3 pos = startPos;
        Vector3 vel = velocity;
        float gravity = Physics.gravity.y;

        while (pos.y > groundY)
        {
            vel.y += gravity * timeStep;   // apply gravity
            pos += vel * timeStep;         // update position

            // Early exit if we pass ground level
            if (pos.y <= groundY)
            {
                // Interpolate to get exact ground hit point
                float overshoot = groundY - pos.y;
                float totalDisplacement = vel.y * timeStep;
                float factor = overshoot / totalDisplacement;
                Vector3 lastPos = pos - vel * timeStep;
                return Vector3.Lerp(lastPos, pos, factor);
            }
        }

        return pos; // fallback
    }

    IEnumerator checkTrajectory()
    {
        Debug.Log("startcheck");
        yield return new WaitForSeconds(0.5f);
        Vector3 lodgePosition = transform.position;
        Ray ray = new Ray(pitchPoint, lodgePosition - pitchPoint);

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);

        yield return new WaitUntil(() =>ballPassed(transform));

        if (hits.Length > 0 && !secondTouch)
        {
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("rayTest") && hit.collider.transform.parent.gameObject.CompareTag("keeper"))
                {
                    Debug.Log("yess");
                    //shootMarker.transform.position = new Vector3(-82.23507f, hit.point.y, hit.point.z);
                    ballCatchPoint = new Vector3(-92.3f, hit.point.y, hit.point.z);
                    keeper.GetComponent<Fielder>().enabled = true;
                    keeper.KeeperRecieve(ballCatchPoint, this.transform);
                    break; // stop if you found keeper
                }
            }
        }

    }

    bool ballPassed(Transform ballT)
    {
        if (ballT.position.x < -31.99f || ballT.GetComponent<BallHit>().secondTouch)
            return true;
        return false;
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
        keeperExit = false;
    }
}
