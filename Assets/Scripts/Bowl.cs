using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bowl : MonoBehaviour
{
    public void Deliver()
    {
        Gameplay.instance.readyToBowl = true;
    }

    public void BatterTrigger()
    {
        Gameplay.instance.SetBatter();
    }
}
