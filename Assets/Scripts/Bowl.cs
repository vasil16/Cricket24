using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bowl : MonoBehaviour
{

    public static Bowl instance;

    public Transform ball;

    public bool ready;


    private void Awake()
    {
        instance = this;
    }

    public void Deliver()
    {
        //ball.SetParent(null);
        ready = true;        
    }

    public void BatterTrigger()
    {
        Gameplay.instance.SetBatter();
    }
}
