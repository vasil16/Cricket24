using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bowl : MonoBehaviour
{
    public void Deliver()
    {
        //ball.SetParent(null);
        Gameplay.instance.readyToBowl = true;
    }

    public void BatterTrigger()
    {
        Gameplay.instance.SetBatter();
    }
}
