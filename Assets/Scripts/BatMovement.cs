using UnityEngine;
using UnityEngine.UI;

public class BatMovement : MonoBehaviour
{
    [SerializeField] GameObject bat;
    [SerializeField] GameObject hand;
    [SerializeField] GameObject batter;
    [SerializeField] GameObject dPad;
    [SerializeField] Slider rotSlider;

    public float touchSensitivity = 1.5f;


    //[SerializeField] private Rigidbody rigidbody;
    [SerializeField] private FixedJoystick joystick;

    [SerializeField] private float moveSpeed;
    public int controlChoice;

    private void Start()
    {
        rotSlider.onValueChanged.AddListener(SliderControl);
    }

    void SliderControl(float value)
    {
        float rotAngle = value;
        bat.transform.rotation = Quaternion.Euler(0, 0, rotAngle);
    }

    bool isDragging;

    public void UpdateControl()
    {
        switch (controlChoice)
        {
            case 0: dPad.SetActive(false);
                gameObject.GetComponent<Bat>().enabled = true;
                batter.GetComponent<Animator>().enabled = true;
                break;
            case 1 : dPad.SetActive(true);
                batter.GetComponent<Animator>().Play("ssww");
                gameObject.GetComponent<Bat>().enabled = false;
                //batter.GetComponent<Animator>().enabled = false;
                break;
        }
    }

    //void Update()
    //{

    //    // Get the horizontal value from the joystick
    //    float horizontalInput = joystick.Horizontal;

    //    // Set the "HorizontalInput" parameter based on joystick input
    //    animator.SetFloat("HorizontalInput", horizontalInput);

    //    // Play the animation based on the joystick input
    //    if (horizontalInput > 0.1f || horizontalInput < -0.1f)
    //    {
    //        // Play forward or backward based on the sign of horizontal input
    //        animator.SetFloat("HorizontalInput", horizontalInput);

    //        // Check if the animation is near the end (adjust the threshold as needed)
    //        if (Mathf.Abs(horizontalInput) > 0.9f)
    //        {
    //            // Stop the animation
    //            animator.SetFloat("HorizontalInput", 0f);
    //        }
    //    }
    //    else
    //    {
    //        // Stop the animation if the horizontal input is near zero
    //        animator.SetFloat("HorizontalInput", 0f);
    //    }
    //}


    //private void FixedUpdate()
    //{
    //    if (joystick.Direction != Vector2.zero)
    //    {
    //        Quaternion handRotation = hand.transform.rotation;
    //        Vector3 eulerRotation = handRotation.eulerAngles;
    //        Vector2 joystickDirection = joystick.Direction.normalized;
    //        float angle = Mathf.Atan2(joystickDirection.x, -joystickDirection.y) * Mathf.Rad2Deg;
    //        hand.transform.eulerAngles = new Vector3(0f, 0f, angle);
    //        Debug.Log(hand.transform.eulerAngles);
    //    }
    //}

    float joystickThreshold = 0.03f;
    [SerializeField] Animator animator;
    public bool started;
    [SerializeField] GameObject batterCam;

    //private void Update()
    //{
    //    if (started)
    //    {
    //        if (batterCam.activeInHierarchy)
    //        {
    //            if (joystick.Direction != Vector2.zero)
    //            {
    //                float verticalInput = joystick.Direction.y;

    //                // Map the full range of joystick to the entire animation clip
    //                float newNormalizedTime = (verticalInput + 1f) / 2f;

    //                // Apply the normalized time to the animation clip
    //                animator.Play("shot", 0, Mathf.Clamp01(newNormalizedTime));
    //            }
    //            else if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
    //            {
    //                //Stop the animation if the joystick is released and the animation is not complete
    //                animator.Play("shot", 0, 0.2f);
    //            }
    //        }
    //        else
    //        {
    //            if (joystick.Direction != Vector2.zero)
    //            {
    //                float horizontalInput = joystick.Direction.x;

    //                // Map the full range of joystick to the entire animation clip
    //                float newNormalizedTime = (horizontalInput + 1f) / 2f;

    //                // Apply the normalized time to the animation clip
    //                animator.Play("shot", 0, Mathf.Clamp01(newNormalizedTime));
    //            }
    //            else if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
    //            {
    //                //Stop the animation if the joystick is released and the animation is not complete
    //                animator.Play("shot", 0, 0.1f);
    //            }
    //        }
    //    }
    //}
}
