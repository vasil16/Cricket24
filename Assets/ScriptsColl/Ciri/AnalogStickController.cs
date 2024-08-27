using UnityEngine;
using UnityEngine.EventSystems;

public class AnalogStickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform baseRect;
    private RectTransform thumbRect;
    private Vector2 inputVector;

    private AxisEventData axisEventData;

    private void Start()
    {
        baseRect = transform.parent.GetComponent<RectTransform>();
        thumbRect = GetComponent<RectTransform>();
        thumbRect.anchoredPosition = Vector2.zero; // Set the initial position of the thumbstick to the center
    }

    public virtual void OnPointerDown(PointerEventData ped)
    {
        //OnDrag(ped);
    }

    public virtual void OnPointerUp(PointerEventData ped)
    {
        inputVector = Vector2.zero;
        thumbRect.anchoredPosition = Vector2.zero;
    }

    public virtual void OnDrag(PointerEventData ped)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, ped.position, ped.pressEventCamera, out pos))
        {
            pos.x = (pos.x / baseRect.sizeDelta.x);
            pos.y = (pos.y / baseRect.sizeDelta.y);

            inputVector = new Vector2(pos.x * 2 + 1, pos.y * 2 - 1);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Move the thumbstick within the base (outer circle)
            float radius = baseRect.sizeDelta.x / 3; // Adjust this radius as needed
            thumbRect.anchoredPosition = inputVector * radius;
        }
    }


    public float Horizontal() => inputVector.x;
    public float Vertical() => inputVector.y;
}
