 using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Bat : MonoBehaviour
{
    Touch touch;
    [SerializeField] Animator swingAnim;
    [SerializeField] AnimationClip animationClip;
    public Vector3 touchPos;
    public float pressPoint;


    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
        {
            //if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            if (Utils.IsPointerOverUIObject(Input.mousePosition)) return;
            else
            {
                touchPos = Input.mousePosition;
                pressPoint = touchPos.x / Screen.width; // Normalize the x-coordinate to a value between 0 and 1
                StartCoroutine(anim());
            }
        }
    }

    IEnumerator anim()
    {
        //swingAnim.gameObject.GetComponent<Animator>().enabled = true;
        if (pressPoint > 0.5f) // Check if the tap is on the right portion of the screen
        {
            swingAnim.Play("shot");
        }
        else
        {
            swingAnim.Play("pull");
        }
        yield return new WaitForSeconds(1f);
        //swingAnim.enabled = false;
        //swingAnim.gameObject.GetComponent<Animator>().enabled = false;
    }
}