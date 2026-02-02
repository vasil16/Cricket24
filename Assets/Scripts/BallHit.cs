using UnityEngine;
using System.Collections;

public class BallHit : MonoBehaviour
{
    Rigidbody rb;
    public bool secondTouch, groundShot, keeperReceive, fielderReached, boundary, stopTriggered;
    public GameObject fieldedPlayer, shootMarker;
    public Vector3 pitchPoint, ballCatchPoint, shotPoint, shotForce;
    [SerializeField] AudioSource soundFx;
    [SerializeField] AudioClip wicketFx, shotFx;
    [SerializeField] public Fielder keeper;

    public string lastHit;

    public GameObject stopper;

    public bool keeperExit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    //private void Update()
    //{
    //    if (!Gameplay.instance) return;


    //    if (!secondTouch) return;
    //    if (!Gameplay.instance.stadiumBounds.Contains(transform.position))
    //    {
    //        Gameplay.instance.deliveryDead = true;
    //    }
    //    if (transform.position.x > 80 && transform.position.z < 24)
    //    {
    //        Gameplay.instance.sideCam.depth = 0;
    //        Gameplay.instance.sideCam.enabled = true;
    //    }
    //    else
    //    {
    //        Gameplay.instance.sideCam.depth = -2;
    //        Gameplay.instance.sideCam.enabled = false;
    //    }
    //}

    void OnCollisionEnter(Collision collision)
    {
        lastHit = collision.gameObject.tag;

        switch (collision.gameObject.tag)
        {
            case "Wicket":
                if (fielderReached || Gameplay.instance.deliveryDead) return;
                soundFx.PlayOneShot(wicketFx);
                Debug.Log("Stump crash");
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
                    StartCoroutine(SimulateBallTrajectory(transform.position, rb.velocity));
                }
                break;

            case "Bat":
                if (secondTouch)
                {
                    break;
                }
                shotPoint = collision.contacts[0].point;
                shotForce = rb.velocity;
                FieldManager.hitBallPos = transform.position;
                FieldManager.hitVelocity = rb.velocity;
                Debug.Log("spot " + collision.gameObject.name);
                //Gameplay.instance.bb.position = collision.GetContact(0).point;
                Gameplay.instance.broadcastCamComp.readyToDeliver = false;
                //StartCoroutine(waitAndLook());                
                secondTouch = true;
                soundFx.PlayOneShot(shotFx);
                shootMarker.transform.position = PredictFallPosition(transform.position, FieldManager.hitVelocity, -4.44f);
                FieldManager.StartCheckField.Invoke(shotPoint);
                break;

            case "Ground":
                if (secondTouch && !groundShot)
                {
                    groundShot = true;
                    rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.04f, rb.velocity.z);
                }
                break;

            case "boundary":
                Debug.Log("Stopped by " + collision.gameObject.name);
                stopTriggered = true;
                boundary = true;
                Gameplay.instance.deliveryDead = true;
                //FieldManager.StopField?.Invoke();
                break;
        }
    }

    

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag is "keeper")
        {
            keeperExit = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("keeper") || (other.gameObject.CompareTag("rayTest") && other.transform.parent.CompareTag("keeper")))
        {
            keeperReceive = true;
            if (secondTouch)
            {
                fieldedPlayer = other.transform.parent.gameObject;
                fielderReached = true;
                return;
            }            
        }

        if (other.gameObject.tag is "fielder" or "DeepFielder")
        {
            if (secondTouch)
            {                
                fieldedPlayer = other.transform.parent.gameObject;
                fielderReached = true;                
            }
        }

        if (other.gameObject.CompareTag("stop"))
        {
            if (stopTriggered) return;
            stopTriggered = true;
            rb.isKinematic = true;
            transform.SetParent(other.transform, true);
            transform.position = other.transform.position;
            stopper = other.gameObject;
            stopper.GetComponent<BallStopper>().fieldScript.DropBall(stopper.CompareTag("keeper")?true:false);
            //Debug.Log("stopped by " + stopper.name);
        }

        if (other.gameObject.CompareTag("camTrigger"))
        {
            //Debug.Log("os");
            Vector3 contactPoint = transform.position;
            CheckLegalDelivery(contactPoint);
            cover = true;
        }
    }
    public bool cover = false;

    Vector3 PredictFallPosition(Vector3 startPos, Vector3 velocity, float groundY, float timeStep = 0.02f)
    {
        Vector3 pos = startPos;
        Vector3 vel = velocity;
        float gravity = Physics.gravity.y;

        while (pos.y > groundY)
        {
            vel.y += gravity * timeStep;  
            pos += vel * timeStep;        

            if (pos.y <= groundY)
            {
                float overshoot = groundY - pos.y;
                float totalDisplacement = vel.y * timeStep;
                float factor = overshoot / totalDisplacement;
                Vector3 lastPos = pos - vel * timeStep;
                return Vector3.Lerp(lastPos, pos, factor);
            }
        }

        return pos; // fallback
    }

    [SerializeField] LayerMask keeperLayer;


    //IEnumerator SimulateBallTrajectory(Vector3 startPosition, Vector3 initialVelocity)
    //{
    //    float timestep = 0.005f;
    //    float maxTime = 2f;
    //    float ballRadius = 0.12f;
    //    int stepsPerFrame = 5;

    //    Vector3 currentPosition = startPosition;
    //    Vector3 velocity = initialVelocity;

    //    for (float t = 0f; t < maxTime; t += timestep)
    //    {
    //        for (int i = 0; i < stepsPerFrame; i++)
    //        {
    //            Vector3 nextPosition = currentPosition + velocity * timestep + 0.5f * Physics.gravity * timestep * timestep;
    //            Vector3 direction = nextPosition - currentPosition;

    //            Debug.DrawRay(currentPosition, direction, Color.red, 2f);

    //            if (Physics.SphereCast(currentPosition, ballRadius, direction.normalized, out RaycastHit hit, direction.magnitude, keeperLayer, QueryTriggerInteraction.Collide))
    //            {
    //                if (hit.collider.CompareTag("keeper") || (hit.collider.CompareTag("rayTest") && hit.collider.transform.parent.CompareTag("keeper")))
    //                {
    //                    Debug.Log("Keeper will catch ball at: " + hit.point);
    //                    Vector3 fixedCatchPoint = hit.point;
    //                    fixedCatchPoint.x = -91.12f;
    //                    ballCatchPoint = fixedCatchPoint;
    //                    shootMarker.transform.position = ballCatchPoint;
    //                    yield return new WaitUntil(() => cover);
    //                    keeper.GetComponent<Fielder>().enabled = true;
    //                    keeper.KeeperRecieve(ballCatchPoint, this.transform);
    //                    yield break;
    //                }
    //            }

    //            velocity += Physics.gravity * timestep;
    //            currentPosition = nextPosition;
    //        }

    //        yield return null;
    //    }
    //}

    IEnumerator SimulateBallTrajectory(Vector3 startPosition, Vector3 initialVelocity)
    {
        // 1. Match the project's exact physics step for 100% accuracy
        float timestep = Time.fixedDeltaTime;
        float maxTime = 2f;
        float ballRadius = 0.12f;

        // Get the drag from the actual Rigidbody component
        float drag = rb.drag;

        Vector3 currentPosition = startPosition;
        Vector3 velocity = initialVelocity;

        int totalSteps = Mathf.CeilToInt(maxTime / timestep);

        for (int s = 0; s < totalSteps; s++)
        {
            // 2. Physics Step Calculation (Unity Style)
            // First, apply gravity
            velocity += Physics.gravity * timestep;

            // Second, apply Drag (This is what usually causes the "higher" error)
            // Unity uses: velocity *= 1.0 / (1.0 + drag * timestep)
            velocity /= (1f + drag * timestep);

            // Third, calculate the movement
            Vector3 moveDelta = velocity * timestep;
            Vector3 nextPosition = currentPosition + moveDelta;

            // Visualization
            Debug.DrawLine(currentPosition, nextPosition, Color.red, 2f);

            // 3. Collision Detection
            if (Physics.SphereCast(currentPosition, ballRadius, moveDelta.normalized, out RaycastHit hit, moveDelta.magnitude, keeperLayer, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.CompareTag("keeper") || (hit.collider.CompareTag("rayTest") && hit.collider.transform.parent.CompareTag("keeper")))
                {
                    Vector3 fixedCatchPoint = hit.point;
                    fixedCatchPoint.z = 155f;
                    ballCatchPoint = fixedCatchPoint;

                    shootMarker.transform.position = ballCatchPoint;

                    //yield return new WaitUntil(() => Vector2.Distance(new Vector2(keeper.transform.position.x, keeper.transform.position.z), new Vector2(transform.position.x, transform.position.z)) <21);
                    Debug.Log("check done for distance");
                    keeper.GetComponent<Fielder>().enabled = true;
                    keeper.KeeperRecieve(ballCatchPoint, this.transform);
                    yield break;
                }
            }

            currentPosition = nextPosition;

            // This ensures we don't calculate everything in one frame (unless you want to)
            // For a path preview, you might want to run this in a single frame's loop.
            // For a real-time sync, we yield every few steps.
            if (s % 5 == 0) yield return null;
        }
    }

    void CheckLegalDelivery(Vector3 enterPos)
    {
        if(enterPos.z is <= -4.71f or >=1.8f || enterPos.y > 2.96f)
        {
            //Debug.Log("wideball");
            //Gameplay.instance.legalDelivery = false;
        }
        else
        {
            //Debug.Log("goodball");
            //Gameplay.instance.legalDelivery = true;
        }
    }

    public void Reset()
    {
        lastHit = "";
        cover = false;
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
