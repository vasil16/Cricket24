using UnityEngine;

public class Wick : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] GameObject pauseBtn;

    [SerializeField] AudioSource wicketFx;
    [SerializeField] AudioSource crowdFx;
    Vector3 ogPos;

    private HingeJoint joint;

    public float breakForce = 10000f;
    public float breakTorque = 10000f;

    private Rigidbody rb;

    public GameObject groundObject;

    private Rigidbody groundRb;

    private void Awake()
    {
        ogPos = gameObject.transform.position;

        if (groundObject != null)
        {
            groundRb = groundObject.GetComponent<Rigidbody>();
            if (groundRb == null)
            {
                groundRb = groundObject.AddComponent<Rigidbody>();
                groundRb.isKinematic = true;
            }
        }
        else
        {
            Debug.LogError("Ground object is not assigned.");
        }
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Ball"))
    //    {
            
    //    }
    //}

    public void ResetPosition()
    {
        gameObject.transform.position = ogPos;
        rb.velocity = Vector3.zero; // Reset velocity
        rb.angularVelocity = Vector3.zero; // Reset angular velocity
    }
}
