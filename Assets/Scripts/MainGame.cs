using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGame : MonoBehaviour
{
    [SerializeField] GameObject[] cams;
    [SerializeField] BatMovement batMovement;
    [SerializeField] GameObject homePanel, startObj, lights, sun;
    [SerializeField] Animator swingAnim;
    [SerializeField] AnimationClip blockAnim;
    [SerializeField] Material day, Night, floodLight;
    public static int camIndex;

    private void Start()
    {
        camIndex = 1;
    }

    private void OnApplicationQuit()
    {
        floodLight.EnableKeyword("_EMISSION");
    }

    private void Update()
    {
        Application.targetFrameRate = 60;
    }

    public void TimeSelect(int index)
    {
        if(index==0)
        {
            RenderSettings.skybox = day;
            //lights.SetActive(false);
            floodLight.DisableKeyword("_EMISSION");
            sun.SetActive(true);
            Debug.Log("day");
        }
        else
        {
            RenderSettings.skybox = Night;
            lights.SetActive(true);
            sun.SetActive(false);
            Debug.Log("night");
        }
    }

    public void CameraSelection(int index)
    {
        camIndex = index;
        for(int i =0; i<cams.Length;i++)
        {
            if(i == index)
            {
                cams[i].SetActive(true);
            }
            else
            {
                cams[i].SetActive(false);
            }
        }
    }

    public void ControlSelection(int choice)
    {
        batMovement.controlChoice = choice;
        batMovement.UpdateControl();
        if(choice==0)
        {
            batMovement.enabled = false;
        }
        else
        {
            batMovement.enabled = true;
        }
    }

    public void PlayButton()
    {
        homePanel.SetActive(false);
        startObj.SetActive(true);
        batMovement.started = true;
    }

    public void PlayShot(string shot)
    {
        if (swingAnim.enabled)
        {
            swingAnim.Play(shot);
        }
        else
        {
            StartCoroutine(DelayBlockAnim(shot));
        }
    }

  

    public void Pull()
    {     
        swingAnim.Play("pull");
    }

    public void vDrive()
    {
        swingAnim.Play("shot2");
    }

    public void loftDrive()
    {
        swingAnim.Play("shot");
    }

    IEnumerator DelayBlockAnim(string animName)
    {
        //swingAnim.enabled = true;
        swingAnim.Play(animName);
        yield return new WaitForSeconds(blockAnim.length +0.12f);
        //swingAnim.enabled = false;
    }
}
