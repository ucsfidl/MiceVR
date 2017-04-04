using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System;

public class WaterTreeScript : MonoBehaviour {

	public GameObject crown, trunk, waterBase, topCap, bottomCap;
    public Color waterBaseColor;
    
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

	private int incorrectTurnDelay = 4;  // sec
	private int correctTurnDelay = 2;  // sec

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
		//Debug.Log ("WaterTree at " + this.gameObject.transform.position.x + " triggered by " + c.tag);
		if (c.tag == "Player") {
			Globals.numberOfTrials++;
			if (this.enabled) {
				//Debug.Log ("Dispensing water");
				Globals.playerInWaterTree = true;
				//GameObject.Find ("UDPSender").GetComponent<UDPSend> ().SendInWater ();
				if (!this.depleted) {
                    // If the mouse has not had a reward in some time, give a proportionally large reward, up to 4x the normal reward size
                    int len = Globals.firstTurn.Count;
                    float multiplier = 1;
                    if (len >= 20)
                    {
                        float recentAccuracy, adjRecentAccuracy;  // varies the boundary based on mouse's accuracy
                        int recentCorrect = 0;
                        int start;
                        int end;
                        end = len;
                        start = len - 20;
                        for (int i = start; i < end; i++)
                        {
                            if (System.Convert.ToInt32(Globals.firstTurn[i]) == System.Convert.ToInt32(Globals.targetLoc[i]))
                            {
                                recentCorrect++;
                            }
                        }
                        recentAccuracy = (float)recentCorrect / (end - start);
                        adjRecentAccuracy = (float)0.5 - recentAccuracy;
                        if (adjRecentAccuracy > 0)
                            multiplier += adjRecentAccuracy * 10;  // Give max up to 6x normal reward size
                    }
                    int rewardDur = (int)(Globals.rewardDur * multiplier);
                    GameObject.Find("UDPSender").GetComponent<UDPSend>().SendWaterReward(rewardDur);
					Debug.Log ("Water reward size = " + rewardDur);

                    //GameObject.Find("movementRecorder").GetComponent<MovementRecorder>().logReward(true,false);
                    this.depleted = true;
					this.mouseObject = GameObject.FindGameObjectWithTag ("MainCamera");
					this.mouseObject.GetComponent<AudioSource> ().Play ();
					Globals.numberOfEarnedRewards++;
                    Globals.sizeOfRewardGiven.Add(Globals.rewardSize / Globals.rewardDur * rewardDur);
                    Globals.rewardAmountSoFar += Globals.rewardSize / Globals.rewardDur * rewardDur;
				}
				// NB edit
				if (Globals.hasNotTurned) {
					Globals.hasNotTurned = false;
					Globals.numCorrectTurns++;
					Globals.firstTurn.Add (this.gameObject.transform.position.x);
				}
				Globals.trialDelay = correctTurnDelay;
				GameObject.Find ("GameControl").GetComponent<GameControlScript> ().ResetScenario (Color.black);
			} else {
				if (Globals.hasNotTurned) {
					Globals.hasNotTurned = false;
					Globals.firstTurn.Add (this.gameObject.transform.position.x);
                    Globals.sizeOfRewardGiven.Add(0);
                }
                //  Added line below to respawn even on incorrect turns, as Harvey does
                Globals.trialDelay = incorrectTurnDelay;
				GameObject.Find ("GameControl").GetComponent<GameControlScript> ().ResetScenario (Color.white);
			}
            Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
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

	public void Hide()
	{
		foreach (Transform t in this.gameObject.transform) {
			t.gameObject.SetActive (false);
			//Debug.Log ("Inactivated = " + t.gameObject.name);
		}
		this.enabled = false;
		this.gameObject.GetComponent<CapsuleCollider> ().enabled = true;  // Something is making this false...
		//Debug.Log ("Collider is " + this.gameObject.GetComponent<CapsuleCollider> ().enabled);
	}

	public void Show()
	{
		foreach (Transform t in this.gameObject.transform) {
			t.gameObject.SetActive (true);
			//Debug.Log ("Activated = " + t.gameObject.name);
		}
		this.enabled = true;
		this.gameObject.GetComponent<CapsuleCollider> ().enabled = true;
	}
}
