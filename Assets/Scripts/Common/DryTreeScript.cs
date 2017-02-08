using UnityEngine;
using System.Collections;

public class DryTreeScript : MonoBehaviour {

    public GameObject crown, topCap, bottomCap;
    private Material dryMaterial;

    private Material editMaterial;
    private bool tex;
    private Texture ogTexture;
    private float ogHFreq, ogVFreq, ogDegree;
    private UDPSend udpSender;

    public bool texture, gradient, angular;

    /*
    void Start()
    {
        
    }
     * */

    void OnTriggerEnter(Collider c)
    {
        Globals.playerInDryTree = true;
        GameObject.Find("UDPSender").GetComponent<UDPSend>().SendInDry();
		Globals.numberOfDryTrees++;
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

    void OnTriggerExit()
    {
        Globals.playerInDryTree = false;
        Globals.timeoutState = false;
    }

    public void ChangeShader(float HFreq, float VFreq, float deg)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
        this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
        this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void ChangeShaderRotation(float deg)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void ChangeTexture(Texture t)
    {
        dryMaterial = new Material(Shader.Find("Unlit/Texture"));
        this.crown.GetComponent<Renderer>().material = dryMaterial;
        this.crown.GetComponent<Renderer>().material.mainTexture = t;
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void ChangeColor(Color c)
    {
        editMaterial = new Material(Shader.Find("Unlit/Color"));
        this.crown.GetComponent<Renderer>().material = editMaterial;
        this.crown.GetComponent<Renderer>().material.color = c;
        this.topCap.SetActive(false);
        this.bottomCap.SetActive(false);
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

    public void ChangeBottomRing(float a)
    {
        this.GetComponent<AngularTreeScript>().ChangeBottomRing(a);
        DisableCaps();
    }

    public void ChangeTopRing(float a)
    {
        this.GetComponent<AngularTreeScript>().ChangeTopRing(a);
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
