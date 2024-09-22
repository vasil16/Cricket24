using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandle : MonoBehaviour
{

    Touch touch;
    Vector3 startPoint, endPoint;

    void Start()
    {

    }

    public bool cut;
    public float diss;

    // Update is called once per frame
    void Update()
    {
        if(true)
        {
            if (Input.touchCount > 0 && !cut)
            {
                foreach(Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        startPoint = touch.position;
                    }
                    if (touch.phase == TouchPhase.Ended)
                    {
                        endPoint = touch.position;
                        diss = Vector3.Distance(endPoint, startPoint);
                        if (Vector3.Distance(endPoint,startPoint)<=30)
                        {
                            Debug.Log("no swipe for "+diss);
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
            Debug.Log("Swipe in Slice 1 (Right)");
        }
        else if (angle >= 45 && angle < 90)
        {
            Debug.Log("Swipe in Slice 2 (Top-Right)");
        }
        else if (angle >= 90 && angle < 135)
        {
            Debug.Log("Swipe in Slice 3 (Top)");
        }
        else if (angle >= 135 && angle < 180)
        {
            Debug.Log("Swipe in Slice 4 (Top-Left)");
        }
        else if (angle >= 180 && angle < 225)
        {
            Debug.Log("Swipe in Slice 5 (Left)");
        }
        else if (angle >= 225 && angle < 270)
        {
            Debug.Log("Swipe in Slice 6 (Bottom-Left)");
        }
        else if (angle >= 270 && angle < 315)
        {
            Debug.Log("Swipe in Slice 7 (Bottom)");
        }
        else if (angle >= 315 && angle < 360)
        {
            Debug.Log("Swipe in Slice 8 (Bottom-Right)");
        }
    }
}
