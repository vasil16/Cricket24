using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



public class MainGame : MonoBehaviour
{
    [SerializeField] GameObject[] cams;
    [SerializeField] BatMovement batMovement;
    [SerializeField] GameObject homePanel, startObj, lights, sun, canvas;
    [SerializeField] Animator swingAnim;
    [SerializeField] AnimationClip blockAnim;
    [SerializeField] Material day, Night, floodLight;
    public static int camIndex;
    [SerializeField] Transform batter;

    public Renderer stadium;
    private MaterialPropertyBlock mpb;

    [SerializeField] Color[] color;

    public static MainGame instance;

    private void Awake()
    {
        if(instance == null)
            instance = this;
    }


    private void Start()
    {
        camIndex = 1;
        canvas.SetActive(true);

        mpb = new MaterialPropertyBlock();

        // Assign different colors to each submesh
        for (int i = 0; i < stadium.sharedMaterials.Length; i++)
        {

            mpb.SetColor("_BaseColor", color[i]);

            // Apply the MPB to the submesh index
            stadium.SetPropertyBlock(mpb, i);
        }
    }


    private void OnApplicationQuit()
    {
        floodLight.EnableKeyword("_EMISSION");
    }

    public void TimeSelect(int index)
    {
        if(index==0)
        {
            RenderSettings.skybox = day;
            lights.SetActive(false);
            floodLight.DisableKeyword("_EMISSION");
            sun.SetActive(true);
            Debug.Log("day");
        }
        else
        {
            RenderSettings.skybox = Night;
            lights.SetActive(true);
            floodLight.EnableKeyword("_EMISSION");
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

    public void MoveBatter(int side)
    {
        batter.position += Vector3.forward * side *0.1f;
        batter.TryGetComponent(out Animator anim);
        anim.Play("move");
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
