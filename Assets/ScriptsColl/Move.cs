using UnityEngine;

public class Move : MonoBehaviour
{
    public float moveSpeed = 5f; // Adjust the speed of movement
    public float flySpeed = 7f; // Speed when flying
    public float maxFlyDuration = 2.8f; // Maximum duration of flight
    public float cooldownDuration = 1f; // Cooldown duration after extended flight
    public float gravityScale = 1f; // Gravity scale when flying ends

    private Rigidbody2D rb;
    private bool isFlying = false;
    private bool extendedFlight = false;
    private float flyTimer = 0f;
    private float cooldownTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveInput = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        if (moveInput > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!extendedFlight) // Check if cooldown is over before allowing to start flying again
            {
                StartFlying();
            }
        }

        if (isFlying)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                flyTimer += Time.deltaTime;
                if (!extendedFlight && flyTimer > maxFlyDuration)
                {
                    StopFlying();
                }
                rb.velocity = new Vector2(rb.velocity.x, flySpeed);
            }
            else
            {
                StopFlying();
            }
        }

        if (extendedFlight)
        {
            if (cooldownTimer < cooldownDuration)
            {
                cooldownTimer += Time.deltaTime;
            }
            else
            {
                extendedFlight = false;
                flyTimer = 0f;
                cooldownTimer = 0f;
                rb.gravityScale = gravityScale; // Reset gravity scale after cooldown
            }
        }
    }

    void StartFlying()
    {
        isFlying = true;
        flyTimer = 0f;
        cooldownTimer = 0f;
        extendedFlight = false;
        rb.gravityScale = 0f; // Disable gravity while flying
    }

    void StopFlying()
    {
        isFlying = false;
        if (flyTimer > maxFlyDuration)
        {
            extendedFlight = true;
        }
        rb.gravityScale = gravityScale; // Apply gravity when flight stops
    }
}
