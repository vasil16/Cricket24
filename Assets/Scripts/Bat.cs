using System.Collections;
using UnityEngine;

public class Bat : MonoBehaviour
{
    [SerializeField] Animator swingAnim;
    [SerializeField] AnimationClip animationClip;
    public Vector3 touchPos;
    public float pressPoint;

    Vector3 startPoint, endPoint;
    public float diss;

    void Update()
    {
        //if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
        //{
        //    //if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
        //    if (Utils.IsPointerOverUIObject(Input.mousePosition)) return;
        //    else
        //    {
        //        touchPos = Input.mousePosition;
        //        pressPoint = touchPos.x / Screen.width; // Normalize the x-coordinate to a value between 0 and 1
        //        StartCoroutine(anim());
        //    }
        //}

        if (true)
        {
            if (Input.touchCount > 0 )
            {
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        startPoint = touch.position;
                    }
                    if (touch.phase == TouchPhase.Ended)
                    {
                        endPoint = touch.position;
                        diss = Vector3.Distance(endPoint, startPoint);
                        if (Vector3.Distance(endPoint, startPoint) <= 30)
                        {
                            //Debug.Log("no swipe for " + diss);
                            return;
                        }
                        Vector3 direction = endPoint - startPoint;
                        CheckDirection(direction);
                    }
                }
            }
        }
    }

    void CheckDirection(Vector3 direction)
    {
        // Calculate the angle of the swipe
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Normalize the angle to be between 0 and 360
        if (angle < 0) angle += 360;

        // Divide the screen into 8 pizza slices (each 45 degrees)
        if (angle >= 0 && angle < 45)
        {
            //Debug.Log("Swipe in Slice 1 (Right)");
            swingAnim.Play("pull");
        }
        else if (angle >= 45 && angle < 90)
        {
            //Debug.Log("Swipe in Slice 2 (Top-Right)");
            swingAnim.Play("flick");
        }
        else if (angle >= 90 && angle < 135)
        {
            //Debug.Log("Swipe in Slice 3 (Top)");
            //swingAnim.Play("block");
        }
        else if (angle >= 135 && angle < 180)
        {
            //Debug.Log("Swipe in Slice 4 (Top-Left)");
            swingAnim.Play("cut");
        }
        else if (angle >= 180 && angle < 225)
        {
            //Debug.Log("Swipe in Slice 5 (Left)");
            swingAnim.Play("offDrive");
        }
        else if (angle >= 225 && angle < 270)
        {
            //Debug.Log("Swipe in Slice 6 (Bottom-Left)");
            swingAnim.Play("straightDrive");
        }
        else if (angle >= 270 && angle < 315)
        {
            //Debug.Log("Swipe in Slice 7 (Bottom)");
            swingAnim.Play("shot");
        }
        else if (angle >= 315 && angle < 360)
        {
            //Debug.Log("Swipe in Slice 8 (Bottom-Right)");
            swingAnim.Play("flick");
        }
    }

    IEnumerator anim()
    {
        if (pressPoint > 0.5f) // Check if the tap is on the right portion of the screen
        {
            swingAnim.Play("shot");
        }
        else
        {
            swingAnim.Play("pull");
        }
        yield return new WaitForSeconds(1f);
    }
}