using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bowl : MonoBehaviour
{

    public static Bowl instance;

    public bool ready;

    public Animator anim;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Deliver()
    {
        ready = true;        
    }
}
