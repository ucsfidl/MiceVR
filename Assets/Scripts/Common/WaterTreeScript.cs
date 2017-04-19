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

    private int rewardDur;  // amount of reward to dispense for this tree, in ms
    private bool respawn;
    private bool correctTree;


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
                    int len = Globals.firstTurn.Count;
                    float multiplier = 1;
                    if (len >= 20) // If the mouse has not had a reward in some time, give a proportionally large reward, up to 6x the normal reward size, if they turned the opposite direction as their turning bias
                    {
                        float recentAccuracy = (float)GameObject.Find("GameControl").GetComponent<GameControlScript>().GetLastAccuracy(20) / 100;  // Returns as int from 0-100
                        float adjRecentAccuracy = 0.5F - recentAccuracy;
                        float turn0Bias = (float)GameObject.Find("GameControl").GetComponent<GameControlScript>().GetTurnBias(20) / 100;

                        if (adjRecentAccuracy > 0)
                        {
                            GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
                            // Only boost reward if mouse hit the tree in the opposite direction of their bias!
                            if ((turn0Bias > 0.6 && this.gameObject.transform.position.x.Equals(gos[1].transform.position.x)) ||
                                (turn0Bias < 0.4 && this.gameObject.transform.position.x.Equals(gos[0].transform.position.x)))
                                multiplier += adjRecentAccuracy * 10;
                        }
                    }
                    int rewardDur;
                    if (this.rewardDur == Globals.rewardDur)  // The scenario file did not specify a specific reward for this tree
                    {
                        rewardDur = (int)(Globals.rewardDur * multiplier);
                    }
                    else // The scenario file did specify a specific reward for this tree - so don't do any multiplier trickery
                    {
                        rewardDur = this.rewardDur;
                    }
                    //Debug.Log("rewardDur = " + rewardDur);

                    if (Globals.gameType.Equals("detection"))
                    {
                        if (this.GetShaderVFreq() == 0)  // The mouse ran into the special center tree - give reward only if no other trees displayed
                        {
                            GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
                            Debug.Log("number of trees: " + gos.Length);
                            bool alone = true;
                            for (int i = 0; i < gos.Length - 1; i++)  // This special tree is always listed last
                            {
                                //Debug.Log("Tree enabled state is " + gos[i].transform.GetChild(0).gameObject.activeSelf.ToString());
                                if (gos[i].transform.GetChild(0).gameObject.activeSelf)
                                {
                                    alone = false;
                                }
                            }
                            if (alone)
                            {
                                GiveReward(rewardDur);
                            }
                            else  // error trial
                            {
                                WitholdReward();
                            }
                        }
                        else
                        {
                            GiveReward(rewardDur);
                        }
                    }
                    else if (Globals.gameType.Equals("match") || Globals.gameType.Equals("nonmatch"))  // There are three trees - a central initial tree, and 1 on left and 1 on right
                    {
                        if (!respawn)  // This is the starting central tree
                        {
                            GiveReward(rewardDur);
                        }
                        else if (correctTree)
                        {
                            GiveReward(rewardDur);
                        }
                        else
                        {
                            WitholdReward();
                        }
                    }
                }
            } else {
                WitholdReward();
			}
            Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
            Globals.WriteToLogFiles();
		}
    }

    private void GiveReward(int rewardDur)
    {
        GameObject.Find("UDPSender").GetComponent<UDPSend>().SendWaterReward(rewardDur);
        //Debug.Log("Water reward size = " + rewardDur);
        //GameObject.Find("movementRecorder").GetComponent<MovementRecorder>().logReward(true,false);
        this.depleted = true;
        this.mouseObject = GameObject.FindGameObjectWithTag("MainCamera");
        this.mouseObject.GetComponent<AudioSource>().Play();
        Globals.numberOfEarnedRewards++;
        Globals.sizeOfRewardGiven.Add(Globals.rewardSize / Globals.rewardDur * rewardDur);
        Globals.rewardAmountSoFar += Globals.rewardSize / Globals.rewardDur * rewardDur;
        if (Globals.hasNotTurned)
        {
            Globals.hasNotTurned = false;
            Globals.numCorrectTurns++;
            Globals.firstTurn.Add(this.gameObject.transform.position.x);
            Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
            Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
        }
        if (respawn)
        {
            Globals.trialDelay = correctTurnDelay;
            GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(Color.black);
        }
    }

    private void WitholdReward()
    {
        if (Globals.hasNotTurned)
        {
            Globals.hasNotTurned = false;
            Globals.firstTurn.Add(this.gameObject.transform.position.x);
            Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
            Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
            Globals.sizeOfRewardGiven.Add(0);
        }
        if (respawn)
        {
            Globals.trialDelay = incorrectTurnDelay;
            GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(Color.white);
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

    public void SetShader(float HFreq, float VFreq, float deg)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
        this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
        this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void SetShader(float HFreq, float VFreq)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
        this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

    public void SetShaderRotation(float deg)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
        EnableCaps();
    }

    public float GetShaderHFreq()
    {
        return this.crown.GetComponent<Renderer>().material.GetFloat("_HFreq");
    }

    public float GetShaderVFreq()
    {
        return this.crown.GetComponent<Renderer>().material.GetFloat("_VFreq");
    }

    public float GetShaderRotation()
    {
        return this.crown.GetComponent<Renderer>().material.GetFloat("_Deg");
    }

    public void SetRewardSize(float r)
    {
        this.rewardDur = (int)Math.Round(r / (Globals.rewardSize / Globals.rewardDur));
        Debug.Log("Reward duration set: " + this.rewardDur);
    }

    public void SetRespawn(bool r)
    {
        this.respawn = r;
    }

    public void SetCorrect(bool c)
    {
        this.correctTree = c;
    }

    public void ChangeTexture(Texture t)
    {
        waterMaterial = new Material(Shader.Find("Unlit/Texture"));
        this.crown.GetComponent<Renderer>().material = waterMaterial;
        this.crown.GetComponent<Renderer>().material.mainTexture = t;
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
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
            SetShader(ogHFreq, ogVFreq, ogDegree);
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
