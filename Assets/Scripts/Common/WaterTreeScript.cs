using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Ports;

public class WaterTreeScript : MonoBehaviour {

	public GameObject crown, trunk, waterBase, topCap, bottomCap;
    public Color waterBaseColor;
    public float wateringSeconds = 0.5f;
    
    private int rewardLevel;
    private bool depleted;
    private bool training;

    private Material waterMaterial;
    private GameObject mouseObject;

    private Material editMaterial;
    private bool tex;
    private Texture ogTexture;
    private float ogHFreq, ogVFreq, ogDegree;

    public bool texture, gradient, angular;

	// Use this for initialization
	void Start ()
    {
        this.depleted = false;
        this.rewardLevel = 1;
	}

    public void Init()
    {
        Shader unlitTex = Shader.Find("Unlit/Texture");
        Shader gradientTex = Shader.Find("Custom/Gradient");
        
        if (this.GetComponent<AngularTreeScript>() != null)
        {
            angular = true;
        }
        if (this.crown.GetComponent<Renderer>().material.shader == unlitTex)
        {
            tex = true;
            ogTexture = this.crown.GetComponent<Renderer>().material.mainTexture;

            texture = true;
        }
        else if (this.crown.GetComponent<Renderer>().material.shader == gradientTex)
        {
            tex = false;
            ogHFreq = this.crown.GetComponent<Renderer>().material.GetFloat("_HFreq");
            ogVFreq = this.crown.GetComponent<Renderer>().material.GetFloat("_VFreq");
            ogDegree = this.crown.GetComponent<Renderer>().material.GetFloat("_Deg");

            gradient = true;
        }
    }

    void OnTriggerEnter(Collider c)
    {
		//Debug.Log ("WaterTree OnTriggerEnter");
		//Debug.Log ((Globals.numberOfRewards).ToString());
        if (c.tag == "Player")
        {
            Globals.playerInWaterTree = true;
            GameObject.Find("UDPSender").GetComponent<UDPSend>().SendInWater();
            if (!this.depleted)
            {
                GameObject.Find("UDPSender").GetComponent<UDPSend>().SendWaterReward(this.rewardLevel);
				//GameObject.Find("movementRecorder").GetComponent<MovementRecorder>().logReward(true,false);
                this.depleted = true;
                this.mouseObject = GameObject.FindGameObjectWithTag("MainCamera");
                this.mouseObject.GetComponent<AudioSource>().Play();
                Globals.numberOfRewards++;
            }
			// NB edit
			GameObject.Find ("GameControl").GetComponent<GameControlScript> ().ResetScenario ();
        }
    }

    void OnTriggerExit()
    {
        Globals.playerInWaterTree = false;
        if (this.training)
            this.depleted = false;
    }

    public void Refill()
    {
        this.depleted = false;
        if( this.training )
            this.waterBase.GetComponent<Renderer>().material.color = this.waterBaseColor;
    }

    public void SetForTraining(bool b)
    {
        this.training = b;
        this.waterBase.GetComponent<Renderer>().material.color = (this.training) ? this.waterBaseColor : Color.black;
    }

    public void ChangeShader(float HFreq, float VFreq, float deg)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
        this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
        this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void ChangeTexture(Texture t)
    {
        waterMaterial = new Material(Shader.Find("Unlit/Texture"));
        this.crown.GetComponent<Renderer>().material = waterMaterial;
        this.crown.GetComponent<Renderer>().material.mainTexture = t;
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void ChangeShaderRotation(float deg)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
        EnableCaps();
    }

    public void ChangeColor(Color c)
    {
        editMaterial = new Material(Shader.Find("Unlit/Color"));
        this.crown.GetComponent<Renderer>().material = editMaterial;
        this.crown.GetComponent<Renderer>().material.color = c;
        DisableCaps();
    }

    public void ResetColor()
    {
        if (tex && this.crown.GetComponent<Renderer>().material.shader != Shader.Find("Unlit/Texture"))
        {
            ChangeTexture(ogTexture);
        }
        else if (!tex && this.crown.GetComponent<Renderer>().material.shader != Shader.Find("Custom/Gradient"))
        {
            Material gradientMaterial = new Material(Shader.Find("Custom/Gradient"));
            this.crown.GetComponent<Renderer>().material = gradientMaterial;
            ChangeShader(ogHFreq, ogVFreq, ogDegree);
        }
    }

    public void EnableCaps()
    {
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void DisableCaps()
    {
        this.topCap.SetActive(false);
        this.bottomCap.SetActive(false);
    }

    public void ChangeTopRing(float waterAngularAngle)
    {
        this.GetComponent<AngularTreeScript>().ChangeTopRing(waterAngularAngle);
        DisableCaps();
    }

    public void ChangeBottomRing(float waterAngularAngle)
    {
        this.GetComponent<AngularTreeScript>().ChangeBottomRing(waterAngularAngle);
        DisableCaps();
    }

    public float ReturnAngle()
    {
        return this.crown.GetComponent<Renderer>().material.GetFloat("_Deg");
    }

    public void DisableHoverScript()
    {
        GetComponent<HoverScript>().enabled = false;
    }
}
