using System.Collections;
using UnityEngine;

public class Bat : MonoBehaviour
{
    [SerializeField] public Animator batterAnim;
    [SerializeField] RectTransform dragObject;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip batHittingGround;

    Vector3 startPoint, endPoint;
    public float diss;

    private void Awake()
    {
        batterAnim.Play("idle");
    }

    void Update()
    {
        if (true)
        {
            if (Input.touchCount > 0)
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
            batterAnim.Play("pull");
        }
        else if (angle >= 45 && angle < 90)
        {
            //Debug.Log("Swipe in Slice 2 (Top-Right)");
            batterAnim.Play("backFlick");
        }
        else if (angle >= 90 && angle < 135)
        {
            batterAnim.Play("block");
        }
        else if (angle >= 135 && angle < 180)
        {
            //Debug.Log("Swipe in Slice 4 (Top-Left)");
            batterAnim.Play("cut");
        }
        else if (angle >= 180 && angle < 225)
        {
            //Debug.Log("Swipe in Slice 5 (Left)");
            batterAnim.Play("cover");
        }
        else if (angle >= 225 && angle < 270)
        {
            //Debug.Log("Swipe in Slice 6 (Bottom-Left)");
            batterAnim.Play("offDrive");
        }
        else if (angle >= 270 && angle < 315)
        {
            //Debug.Log("Swipe in Slice 7 (Bottom)");
            batterAnim.Play("straightDrive");
        }
        else if (angle >= 315 && angle < 360)
        {
            //Debug.Log("Swipe in Slice 8 (Bottom-Right)");
            batterAnim.Play("flick");
        }
    }

    public void PlayClip()
    {
        Debug.Log("sommee");        
        audioSource.PlayOneShot(batHittingGround);
        
    }
}

/*
 using UnityEngine;
using System.Reflection;

public class SetFirstKeyframe : MonoBehaviour
{
    public AnimationClip clip;

    void Start()
    {
        // Ensure the clip is valid
        if (clip != null)
        {
            // Get all properties of the Transform component (or other components if needed)
            AddKeyframesFromComponent(transform, clip);
        }
    }

    void AddKeyframesFromComponent(Component component, AnimationClip clip)
    {
        // Get all public properties of the component
        PropertyInfo[] properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        // Loop through all properties
        foreach (PropertyInfo property in properties)
        {
            // Check if it's a property that can be animated (i.e., numeric types like float, Vector3, etc.)
            if (property.PropertyType == typeof(Vector3))
            {
                Vector3 value = (Vector3)property.GetValue(component);
                AddKeyframeForVector3(property.Name, value, clip);
            }
            else if (property.PropertyType == typeof(Quaternion))
            {
                Quaternion value = (Quaternion)property.GetValue(component);
                AddKeyframeForQuaternion(property.Name, value, clip);
            }
            else if (property.PropertyType == typeof(float))
            {
                float value = (float)property.GetValue(component);
                AddKeyframeForFloat(property.Name, value, clip);
            }
            // Add more property types as needed (e.g., Color, etc.)
        }
    }

    void AddKeyframeForVector3(string propertyName, Vector3 value, AnimationClip clip)
    {
        clip.SetCurve("", typeof(Transform), propertyName + ".x", new AnimationCurve(new Keyframe(0f, value.x)));
        clip.SetCurve("", typeof(Transform), propertyName + ".y", new AnimationCurve(new Keyframe(0f, value.y)));
        clip.SetCurve("", typeof(Transform), propertyName + ".z", new AnimationCurve(new Keyframe(0f, value.z)));
    }

    void AddKeyframeForQuaternion(string propertyName, Quaternion value, AnimationClip clip)
    {
        // Quaternion can be split into four components: x, y, z, w
        clip.SetCurve("", typeof(Transform), propertyName + ".x", new AnimationCurve(new Keyframe(0f, value.x)));
        clip.SetCurve("", typeof(Transform), propertyName + ".y", new AnimationCurve(new Keyframe(0f, value.y)));
        clip.SetCurve("", typeof(Transform), propertyName + ".z", new AnimationCurve(new Keyframe(0f, value.z)));
        clip.SetCurve("", typeof(Transform), propertyName + ".w", new AnimationCurve(new Keyframe(0f, value.w)));
    }

    void AddKeyframeForFloat(string propertyName, float value, AnimationClip clip)
    {
        clip.SetCurve("", typeof(Transform), propertyName, new AnimationCurve(new Keyframe(0f, value)));
    }
}

*/