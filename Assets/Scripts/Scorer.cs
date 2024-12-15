using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scorer : MonoBehaviour
{
    public static Scorer instance;

    public int score;
    [SerializeField] Text scoreText, overText, deliveryDetails;
    string overDetail = "";
    public int teamScore;

    private void Awake()
    {
        instance = this;
    }

    public void UpdateScore(int runs, int wickets, int overs, int ballsLaunched, string lastDelivery)
    {
        teamScore += runs;
        scoreText.text = teamScore + " - " + wickets;
        overText.text = $"{overs}.{ballsLaunched}";
        Debug.Log("last  " + lastDelivery);
        overDetail += lastDelivery +"  "; 
        deliveryDetails.text = overDetail;
    }    
}
