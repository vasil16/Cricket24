using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Scorer : MonoBehaviour
{
    public static Scorer instance;

    public int score;
    [SerializeField] TextMeshProUGUI scoreText;


    private void Awake()
    {
        instance = this;
    }

    public void UpdateScore(float angle, bool grounded)
    {
        if(angle < 0)
        {
            Debug.Log("angle  " + angle);
            score += 1;
            scoreText.text = score + "";
        }
        else if(angle > 0 && angle < 35)
        {
            Debug.Log("angle  " + angle);
            score += 2;
            scoreText.text = score + "";
        }
        else if (angle > 35 && angle < 50)
        {
            Debug.Log("angle  " + angle);
            score += 4;
            scoreText.text = score + "";
        }
        else if (angle > 50)
        {
            Debug.Log("angle  " + angle);
            if (grounded)
            {
                score += 3;
            }
            else
            {
                score += 6;
            }
            scoreText.text = score + "";
        }
    }
}
