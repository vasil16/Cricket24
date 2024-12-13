using UnityEngine;
using UnityEngine.EventSystems;

public class JoyStickControl : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    private float outerRadius;
    private RectTransform innerCircle;
    Vector2 inputDirection;
    [SerializeField] Animator batterAnim;
    Vector2 shotDirection;

    void Start()
    {
        // Assuming the inner circle is the first child of this GameObject
        innerCircle = transform.GetChild(0).GetComponent<RectTransform>(); // Get the RectTransform of the first child (inner circle)

        // Outer radius is half of the width of the joystick
        outerRadius = GetComponent<RectTransform>().sizeDelta.x / 2;
    }

    // This method is called during the drag event.
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPosition = eventData.position; // Get the touch/mouse position from the event data

        // Calculate the shot direction by subtracting the joystick center from the touch position
        shotDirection = touchPosition - (Vector2)innerCircle.position;

        // Clamp the shot direction within the outer radius
        if (shotDirection.magnitude > outerRadius)
        {
            shotDirection = shotDirection.normalized * outerRadius;
        }

        // Update the position of the inner circle based on the shot direction
        innerCircle.anchoredPosition = shotDirection;

        // Normalize the shot direction and store it as the input direction
        inputDirection = shotDirection / outerRadius;
    }

    // Reset the joystick to its original position
    public void Reset()
    {
        innerCircle.anchoredPosition = Vector2.zero;
        shotDirection = Vector2.zero;
        inputDirection = Vector2.zero;
    }

    // Call this method to check the shot direction and play the corresponding animation
    public void JoystickControl()
    {
        CheckDragAndPlay(shotDirection, "standard");
    }

    // Check the shot direction and play the appropriate animation based on the angle
    void CheckDragAndPlay(Vector2 direction, string shotType)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (angle < 0) angle += 360;

        // Play different animations based on the shot angle
        if (angle >= 0 && angle < 45)
        {
            batterAnim.Play("pull");
        }
        else if (angle >= 45 && angle < 90)
        {
            batterAnim.Play("flick");
        }
        else if (angle >= 90 && angle < 135)
        {
            batterAnim.Play("block");
        }
        else if (angle >= 135 && angle < 180)
        {
            batterAnim.Play("cut");
        }
        else if (angle >= 180 && angle < 225)
        {
            batterAnim.Play("offDrive");
        }
        else if (angle >= 225 && angle < 270)
        {
            batterAnim.Play("straightDrive");
        }
        else if (angle >= 270 && angle < 315)
        {
            batterAnim.Play("shot");
        }
        else if (angle >= 315 && angle < 360)
        {
            batterAnim.Play("legGlance");
        }
        Reset();
    }

    // This method is needed to implement IPointerDownHandler but isn't being used in this case
    public void OnPointerDown(PointerEventData eventData)
    {
        // You can handle pointer down events here if needed, or leave it empty
    }
}

