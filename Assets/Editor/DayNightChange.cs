using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class DayNightChange : MonoBehaviour
{
    [SerializeField] private Material day, night, floodLight;
    [SerializeField] private GameObject lights, sun;

    [MenuItem("Time/Day")]
    public static void Option1()
    {
        var dayNightChanger = FindObjectOfType<DayNightChange>();
        if (dayNightChanger != null)
        {
            Debug.Log("Day option selected");
            dayNightChanger.TimeSelect(0);
        }
        else
        {
            Debug.LogWarning("DayNightChange script not found in the scene.");
        }
    }

    [MenuItem("Time/Night")]
    public static void Option2()
    {
        var dayNightChanger = FindObjectOfType<DayNightChange>();
        if (dayNightChanger != null)
        {
            Debug.Log("Night option selected");
            dayNightChanger.TimeSelect(1);
        }
        else
        {
            Debug.LogWarning("DayNightChange script not found in the scene.");
        }
    }

    [MenuItem("Time/Reset")]
    public static void Option3()
    {
        var dayNightChanger = FindObjectOfType<DayNightChange>();
        if (dayNightChanger != null)
        {
            Debug.Log("Reset option selected");
            dayNightChanger.floodLight.EnableKeyword("_EMISSION");
        }
        else
        {
            Debug.LogWarning("DayNightChange script not found in the scene.");
        }
    }

    // This method will handle switching between day and night
    public void TimeSelect(int index)
    {
        if (index == 0) // Day
        {
            RenderSettings.skybox = day;
            if (lights != null) lights.SetActive(false);
            if (floodLight != null) floodLight.DisableKeyword("_EMISSION");
            if (sun != null) sun.SetActive(true);
            Debug.Log("Switched to day");
        }
        else // Night
        {
            RenderSettings.skybox = night;
            if (lights != null) lights.SetActive(true);
            if (floodLight != null) floodLight.EnableKeyword("_EMISSION");
            if (sun != null) sun.SetActive(false);
            Debug.Log("Switched to night");
        }
    }
}
