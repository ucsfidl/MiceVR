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
	private float vCycPerSec;  // for animation of the each tree
	private float hCycPerSec;  // for animation of the each tree


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

	// Update is called once per frame
	// Used to make the grating move along the cylinder, if required
	void Update() {
		if (vCycPerSec > 0 && GetShaderVFreq() > 1) {
			float vp = this.crown.GetComponent<Renderer>().material.GetFloat("_VPhase");
			this.crown.GetComponent<Renderer>().material.SetFloat("_VPhase", vp + 360 / vCycPerSec * Time.deltaTime);
		}
		if (hCycPerSec > 0 && GetShaderHFreq() > 1) {
			float hp = this.crown.GetComponent<Renderer>().material.GetFloat("_HPhase");
			this.crown.GetComponent<Renderer>().material.SetFloat("_HPhase", hp + 360 / hCycPerSec * Time.deltaTime);
		}
	}

    void OnTriggerEnter(Collider c)
    {
		//Debug.Log ("WaterTree at " + this.gameObject.transform.position.x + " triggered by " + c.tag);
		if (c.tag == "Player") {
			if (this.enabled) {
				//Debug.Log ("Dispensing water");
				Globals.playerInWaterTree = true;
                if (!this.depleted) {
                    int len = Globals.firstTurn.Count;
                    float multiplier = 1;
                    GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
                    // (1) COMPUTE REWARD ENLARGEMENT
                    // If the mouse has not had a reward in some time, give a proportionally large reward, up to 5x the normal reward size, if they turned the opposite direction as their turning bias
                    if (gos.Length > 1 && len >= 20)  // The world involves some sort of choice, so there is a turning bias to calculate
                    {
                        float recentAccuracy = Globals.GetLastAccuracy(20);  // Returns as a decimal
                        float turn0Bias = Globals.GetTurnBias(20, 0);
                        float turn1Bias = Globals.GetTurnBias(20, 1);
                        float turn2Bias = 1 - turn0Bias - turn1Bias;

                        float chance = 0.5F;
                        int biasDir = -1;
                        float biasAmt = -1;

                        if (!Globals.gameType.Equals("det_blind"))  // List 3-choice games here 
                        {
                            if (turn0Bias > turn1Bias)
                            {
                                biasDir = 0;
                                biasAmt = turn0Bias;
                            }
                            else
                            {
                                biasDir = 1;
                                biasAmt = turn1Bias;
                            }
                        }
                        else
                        {
                            chance = (float)1 / 3;
                            if (turn0Bias > turn1Bias && turn0Bias > turn2Bias)
                            {
                                biasDir = 0;
                                biasAmt = turn0Bias;
                            } 
                            else if (turn1Bias > turn0Bias && turn1Bias > turn2Bias)
                            {
                                biasDir = 1;
                                biasAmt = turn1Bias;
                            }
                            else
                            {
                                biasDir = 2;
                                biasAmt = turn2Bias;
                            }
                        }

                        if (recentAccuracy < chance)  // Mouse is performing below chance, suggesting they are biased and would benefit from a larger reward on a correct trial that goes against the bias!
                        {
                            if (biasAmt > 1.2 * chance) // e.g. if chance is 50%,  bias must be 60% or greater to multiply the reward
                            {
                                // Only boost reward if mouse hit the tree in the opposite direction of their bias!
                                if (!this.gameObject.transform.position.x.Equals(gos[biasDir].transform.position.x))
                                    multiplier += (chance - recentAccuracy) / (chance/4);
                            }
                        }
                        Debug.Log("chance = " + chance + ", biasDir = " + biasDir + ", biasAmt = " + biasAmt);
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
                    // DONE COMPUTING REWARD ENLARGEMENT

                    // (2) Actually give or withold reward, depending on the gametype!
					if (Globals.gameType.Equals ("detection") || Globals.gameType.Equals ("det_target")) {
						if (respawn)
							GiveReward (rewardDur, true);
					} else if (Globals.gameType.Equals ("det_blind")) {
						if (this.GetShaderVFreq () == 0) {  // The mouse ran into the special center tree - give reward only if no other trees displayed
							bool alone = true;
							for (int i = 0; i < gos.Length - 1; i++) {  // This special tree is always listed last
								if (gos [i].transform.GetChild (0).gameObject.activeSelf) {
									alone = false;
								}
							}
							if (alone) {
								GiveReward (rewardDur, true);
							} else {  // error trial
								WitholdReward ();
							}
						} else {
							GiveReward (rewardDur, true);
						}
					} else if (Globals.gameType.Equals ("discrimination")) {
						if (correctTree)
							GiveReward (rewardDur, true);
						else
							WitholdReward ();                        
					} else if (Globals.gameType.Equals ("disc_target")) {
						if (respawn) {
							if (correctTree)
								GiveReward (rewardDur, true);
							else
								WitholdReward ();
						}
					}
                    else if (Globals.gameType.Equals("match") || Globals.gameType.Equals("nonmatch"))  // There are three trees - a central initial tree, and 1 on left and 1 on right
                    {
                        if (!respawn)  // This is the starting central tree
                        {
                            GiveReward(rewardDur, false);
                        }
                        else if (correctTree)
                        {
                            GiveReward(rewardDur, true);
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
		}
    }

    private void GiveReward(int rewardDur, bool addToTurns)
    {
        GameObject.Find("UDPSender").GetComponent<UDPSend>().SendWaterReward(rewardDur);
        this.depleted = true;
        this.mouseObject = GameObject.FindGameObjectWithTag("MainCamera");
        this.mouseObject.GetComponent<AudioSource>().Play();
        Globals.numberOfEarnedRewards++;
        Globals.sizeOfRewardGiven.Add(Globals.rewardSize / Globals.rewardDur * rewardDur);
        Globals.rewardAmountSoFar += Globals.rewardSize / Globals.rewardDur * rewardDur;

        Globals.hasNotTurned = false;
        if (addToTurns)
        {
            Globals.numCorrectTurns++;
            Globals.firstTurn.Add(this.gameObject.transform.position.x);
            Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
            Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
        }

        if (respawn)
        {
            Globals.numberOfTrials++;
            Globals.trialDelay = correctTurnDelay;
            GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(Color.black);
            Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
            Globals.WriteToLogFiles();
        }
    }

    private void WitholdReward()
    {
        Globals.hasNotTurned = false;
        Globals.firstTurn.Add(this.gameObject.transform.position.x);
        Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
        Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
        Globals.sizeOfRewardGiven.Add(0);
        if (respawn)
        {
            Globals.numberOfTrials++;
            Globals.trialDelay = incorrectTurnDelay;
            GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(Color.white);
            Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
        }

        Globals.WriteToLogFiles();
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
        //this.topCap.SetActive(true);
        //this.bottomCap.SetActive(true);
    }

    public void SetShader(float HFreq, float VFreq)
    {
        this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
        this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
        //this.topCap.SetActive(true);
        //this.bottomCap.SetActive(true);
    }

	// Support for curvy trees - will fail if Curvy hasn't been set as the Shader material
	public void SetShader(float HFreq, float VFreq, float HAmplitude, float HNumCycles, float HSmooth, float VAmplitude, float VNumCycles, float VSmooth)
	{
		this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HAmplitude", HAmplitude);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HNumCycles", HNumCycles);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HSmooth", HSmooth);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VAmplitude", VAmplitude);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VNumCycles", VNumCycles);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VSmooth", VSmooth);
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

	public void SetCycPerSec(float vcps, float hcps)
	{
		this.vCycPerSec = vcps;
		this.hCycPerSec = hcps;
	}

    public void SetCorrect(bool c)
    {
        this.correctTree = c;
    }

	public void ChangeShader(String shaderStr) {
		Material m = new Material (Shader.Find (shaderStr));
		this.crown.GetComponent<Renderer>().material = m;
	}

    public void ChangeTexture(Texture t)
    {
        waterMaterial = new Material(Shader.Find("Unlit/Texture"));
        this.crown.GetComponent<Renderer>().material = waterMaterial;
        this.crown.GetComponent<Renderer>().material.mainTexture = t;
        this.topCap.SetActive(true);
        this.bottomCap.SetActive(true);
    }

	public void SetColors(Color c1, Color c2) {
		this.crown.GetComponent<Renderer>().material.SetColor("_Color1", c1);
		this.crown.GetComponent<Renderer>().material.SetColor("_Color2", c2);
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
