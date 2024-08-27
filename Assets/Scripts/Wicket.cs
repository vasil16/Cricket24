using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wicket : MonoBehaviour
{
    [SerializeField] GameObject panel;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            NewBall.instance.isGameOver = true;
            panel.SetActive(true);
            VibrationManager.instance.HapticVibration(MoreMountains.NiceVibrations.HapticTypes.Failure);
        }
    }
}
