using System.Collections;
using UnityEngine;
using UnityEngine.UI;



public class MainGame : MonoBehaviour
{
    [SerializeField] GameObject[] cams;
    [SerializeField] BatMovement batMovement;
    [SerializeField] GameObject homePanel, startObj, lights, sun, canvas;
    [SerializeField] AnimationClip blockAnim;
    [SerializeField] Material day, Night, floodLight;
    public int camIndex;
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
        //Screen.SetResolution(1280, 720, true);
        //mpb = new MaterialPropertyBlock();

        // Assign different colors to each submesh
        //for (int i = 0; i < stadium.sharedMaterials.Length; i++)
        //{

        //    mpb.SetColor("_BaseColor", color[i]);

        //    // Apply the MPB to the submesh index
        //    stadium.SetPropertyBlock(mpb, i);
        //}

        //#if UNITY_IOS
        //                Application.targetFrameRate = 90;

        //#else
        //        Application.targetFrameRate = 60;
        //        #endif

    }

    public Text fpsText;            // Assign in inspector
    public float updateInterval = 0.5f;

    private float timeAccum = 0f;
    private int frames = 0;
    private float timer = 0f;

    void FixedUpdate()
    {
        timeAccum += Time.unscaledDeltaTime;
        frames++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float fps = frames / timeAccum;
            fpsText.text = $"FPS: {fps:F1}";
            timer = 0f;
            timeAccum = 0f;
            frames = 0;
        }
    }


    private void OnApplicationQuit()
    {
        //floodLight.EnableKeyword("_EMISSION");
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
        //batMovement.started = true;
    }

    
    public void MoveBatter(int side)
    {
        batter.position += Vector3.right * side *.04f;
        batter.TryGetComponent(out Animator anim);
        //anim.Play("move");
    }
}
