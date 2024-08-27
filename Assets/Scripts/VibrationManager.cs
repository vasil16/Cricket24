using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.NiceVibrations;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager instance;

    private void Awake()
    {
        instance = this;
    }

    public void Vibrate()
    {
        MMVibrationManager.Vibrate();
    }

    public void HapticVibration(HapticTypes type)
    {
        MMVibrationManager.Haptic(type);
    }

}
