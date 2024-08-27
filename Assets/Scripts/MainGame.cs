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
    [SerializeField] Material day, Night;

    private void Start()
    {
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
            lights.SetActive(false);
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

    public void BlockBall()
    {
        if (swingAnim.enabled)
        {
            swingAnim.Play("block");
        }
        else
        {
            StartCoroutine(DelayBlockAnim("block"));
        }
    }

    public void Cut()
    {
        if (swingAnim.enabled)
        {
            swingAnim.Play("cut");
        }
        else
        {
            StartCoroutine(DelayBlockAnim("cut"));
        }
    }

    public void offDrive()
    {
        if (swingAnim.enabled)
        {
            swingAnim.Play("offDrive");
        }
        else
        {
            StartCoroutine(DelayBlockAnim("offDrive"));
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
        swingAnim.enabled = true;
        swingAnim.Play(animName);
        yield return new WaitForSeconds(blockAnim.length);
        swingAnim.enabled = false;
    }
}
