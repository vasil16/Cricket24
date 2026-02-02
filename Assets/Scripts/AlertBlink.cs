using UnityEngine;

public class GlobalAlertController : MonoBehaviour
{
    [Header("Target Objects")]
    public Renderer[] indicators;

    [Header("Settings")]
    [ColorUsage(true, true)]
    public Color alertColor = Color.red;
    public float blinkSpeed = 6f;
    public float intensityMultiplier = 4f;

    private MaterialPropertyBlock _propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // Calculate the blink value once per frame
        // Use Mathf.PingPong for a smooth but linear "alarm" feel
        float lerp = Mathf.PingPong(Time.time * blinkSpeed, 1.0f);

        // Square the lerp to make the "off" state last slightly longer (more cinematic)
        float power = lerp * lerp;

        Color finalColor = alertColor * power * intensityMultiplier;

        // Apply the value to all indicators in the array
        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] != null)
            {
                indicators[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor(EmissionColor, finalColor);
                indicators[i].SetPropertyBlock(_propBlock);
            }
        }
    }
}