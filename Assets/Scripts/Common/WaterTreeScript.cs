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

	private float rewardMulti;

	private float presoFrac;

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
		float vp = 0;
		float hp = 0;;
		if (Math.Abs(vCycPerSec) > 0 && GetShaderVFreq() > 1) {
			vp = this.crown.GetComponent<Renderer>().material.GetFloat("_VPhase");
			this.crown.GetComponent<Renderer>().material.SetFloat("_VPhase", (vp + 360 * vCycPerSec * Time.deltaTime) % 360);
		}
		if (Math.Abs(hCycPerSec) > 0 && GetShaderHFreq() > 1) {
			hp = this.crown.GetComponent<Renderer>().material.GetFloat("_HPhase");
			this.crown.GetComponent<Renderer>().material.SetFloat("_HPhase", (hp + 360 * hCycPerSec * Time.deltaTime) % 360);
		}
	}

    void OnTriggerEnter(Collider c) {
		if (c.tag == "Player") {
			if (this.enabled) {
				Globals.playerInWaterTree = true;
                if (!this.depleted) {
                    int len = Globals.firstTurn.Count;
                    float biasMultiplier = 1;
					GameObject[] gos = Globals.GetTrees();
                    // (1) COMPUTE REWARD ENLARGEMENT
                    // If the mouse has not had a reward in some time, give a proportionally large reward, up to 5x the normal reward size, if they turned the opposite direction as their turning bias
					if (gos.Length > 1 && len >= 20 && this.rewardMulti == 0 && !Globals.biasCorrection) { // The world involves some sort of choice, and reward ratios are not fixed, so there is a turning bias to calculate
                        float recentAccuracy = Globals.GetLastAccuracy(20);  // Returns as a decimal
                        float turn0Bias = Globals.GetTurnBias(20, 0);
                        float turn1Bias = Globals.GetTurnBias(20, 1);
						float turn2Bias = Globals.GetTurnBias(20, 2);
						float turn3Bias = 1 - (turn0Bias + turn1Bias + turn2Bias);
						float maxBias = Math.Max (Math.Max (Math.Max (turn0Bias, turn1Bias), turn2Bias), turn3Bias);

						float chance = 1F / gos.Length;
                        int biasDir;
                        float biasAmt;

						if (maxBias == turn0Bias) {
							biasDir = 0;
							biasAmt = turn0Bias;
						} else if (maxBias == turn1Bias) {
							biasDir = 1;
							biasAmt = turn1Bias;
						} else if (maxBias == turn2Bias) {
							biasDir = 2;
							biasAmt = turn2Bias;
						} else {
							biasDir = 3;
							biasAmt = turn3Bias;
						}

						if (recentAccuracy < chance) { // Mouse is performing below chance, suggesting they are biased and would benefit from a larger reward on a correct trial that goes against the bias!
							if (biasAmt > 1.2 * chance) { // e.g. if chance is 50%,  bias must be 60% or greater to multiply the reward
								if (!this.gameObject.transform.position.x.Equals (gos [biasDir].transform.position.x)) {
									//Debug.Log ("adding to biasMultiplyer");
									biasMultiplier += (chance - recentAccuracy) / (chance / 4);
								}
                            }
                        }
                    }

                    int rewardDur;
					if (this.rewardDur == Globals.rewardDur) { // The scenario file did not specify a specific reward for this tree
						if (this.rewardMulti != 0) {  // Scenario specified a fixed multilple of the default value, for shifting biase
							rewardDur = (int)(Globals.rewardDur * this.rewardMulti);  // Assumes linearity, but may need to do actual measurements to see what the reward ratios actually are
						} else {
							rewardDur = (int)(Globals.rewardDur * biasMultiplier);
						}
					} else { // The scenario file did specify a specific reward for this tree - so don't do any multiplier trickery
                        rewardDur = this.rewardDur;
                    }
                    // DONE COMPUTING REWARD ENLARGEMENT

                    // (2) Actually give or withold reward, depending on the gametype!
					string gameType = Globals.GetGameType (Globals.worldID[Globals.worldID.Count - 1]);
					if (gameType.Equals ("detection") || gameType.Equals ("det_target")) {
						if (respawn)
							GiveReward (rewardDur, true, true);
					} else if (gameType.Equals ("det_blind")) {
						if (this.GetShaderVFreq () == 0) {  // The mouse ran into the special center tree - give reward only if no other trees displayed, unless this is a test game
							bool alone = true;
							float otherActiveTreeLocX = float.NaN;
							for (int i = 0; i < gos.Length - 1; i++) {  // This special tree is always listed last
								if (gos [i].transform.GetChild (0).gameObject.activeSelf) {
									alone = false;
									otherActiveTreeLocX = gos [i].transform.position.x;
								}
							}
							if (alone) {  // Give reward if center is only tree or if mouse goes center on a probe trial (contralesional tree)
								GiveReward (rewardDur, true, true);
							} else if ( Globals.probeLocX == otherActiveTreeLocX) {
								GiveReward (rewardDur, true, false);
							} else {  // error trial
								WitholdReward ();
							}
						} else {
							GiveReward (rewardDur, true, true);
						}
					} else if (gameType.Equals ("discrimination")) {
						if (correctTree)
							GiveReward (rewardDur, true, true);
						else
							WitholdReward ();                        
					} else if (gameType.Equals ("disc_target")) {
						if (respawn) {
							if (correctTree)
								GiveReward (rewardDur, true, true);
							else
								WitholdReward ();
						}
					} else if (gameType.Equals("match") || gameType.Equals("nonmatch")) { // There are three trees - a central initial tree, and 1 on left and 1 on right
						if (!respawn) { // This is the starting central tree
							GiveReward(rewardDur, false, false);
                        } else if (correctTree) {
							GiveReward(rewardDur, true, true);
                        } else {
                            WitholdReward();
                        }
                    }
                }
            } else {
                WitholdReward();
			}
		}
    }

	private void GiveReward(int rewardDur, bool addToTurns, bool trueCorrect) {
		// If probabilistic reward, give appropriately, depending on history of reward! (not just simple random number compared to target as before)
		float r = UnityEngine.Random.value;
		int interTrialInterval;
		Color c;
		float xPos = this.gameObject.transform.position.x;
		float adjRewardThreshold = Globals.probReward;

		if (Globals.probReward < 1) {
			// Get actual reward rate at the location of this tree, and modulate the reward rate in proportion to the distance from this target
			float actualRewardRate = Globals.GetActualRewardRate(xPos);
			if (actualRewardRate < Globals.probReward) {
				//adjRewardThreshold = 1 - actualRewardRate / Globals.probReward + actualRewardRate;
				adjRewardThreshold = 1;
			} else {
				adjRewardThreshold = (1 - (actualRewardRate - Globals.probReward) / (1 - Globals.probReward)) * Globals.probReward;
			}
			Debug.Log ("Target reward rate=" + Globals.probReward + ", actual reward rate=" + actualRewardRate + ", adj threshold=" + adjRewardThreshold + ", random val=" + r);
		}

		if (r <= adjRewardThreshold) {
			GameObject.Find ("UDPSender").GetComponent<UDPSend> ().SendWaterReward (rewardDur);
			Globals.numberOfEarnedRewards++;
			Globals.sizeOfRewardGiven.Add(Globals.rewardSize / Globals.rewardDur * rewardDur);
			Globals.rewardAmountSoFar += Globals.rewardSize / Globals.rewardDur * rewardDur;
			this.mouseObject = GameObject.FindGameObjectWithTag("MainCamera");
			this.mouseObject.GetComponent<AudioSource>().Play();

			interTrialInterval = correctTurnDelay;
			c = Color.black;

			// If correction trials are enabled and probabilistic reward is enabled, correction trials DO NOT count toward probabilistic reward counts
			if (!Globals.CurrentlyCorrectionTrial()) {  // Current trial is NOT a correction trial
				Globals.IncrementRewardAtStimLoc (xPos);
			}
		} else {
			Globals.sizeOfRewardGiven.Add(0);
			if (Globals.probabilisticWhiteNoiseWhenNoReward) {
				interTrialInterval = incorrectTurnDelay;
				c = Color.white;
			} else {
				interTrialInterval = correctTurnDelay;
				c = Color.black;
			}
		}
		// If correction trials are enabled and probabilistic reward is enabled, correction trials DO NOT count toward probabilistic reward counts
		if (!Globals.CurrentlyCorrectionTrial()) {  // Current trial is NOT a correction trial
			Globals.IncrementTurnToStimLoc (xPos);
		}

		this.depleted = true;
        Globals.hasNotTurned = false;

		if (addToTurns) {
			if (trueCorrect) {
				if (!Globals.CurrentlyCorrectionTrial()) { // Not correction trial
					Globals.numCorrectTurns++;
				}
			}
			Globals.firstTurn.Add (this.gameObject.transform.position.x);
			Globals.firstTurnHFreq.Add (this.gameObject.GetComponent<WaterTreeScript> ().GetShaderHFreq ());
			Globals.firstTurnVFreq.Add (this.gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
			Globals.firstTurnAngle.Add (this.gameObject.GetComponent<WaterTreeScript> ().GetShaderRotation ());
	        }

		// Must come after in-memory log is updated above
		Globals.lastTrialWasIncorrect = 0;

        if (respawn) {
			if (!Globals.CurrentlyCorrectionTrial ()) {
				Globals.numNonCorrectionTrials++;
			} else {  // Might be off by 1 bug here, as nonCorrectionTrials is the current trial #, but numCorrectionTrials is the total number of correction trials
				Globals.numCorrectionTrials++;
			}
			Globals.trialDelay = interTrialInterval;
			GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(c);
            Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
            Globals.WriteToLogFiles();
			GameObject.Find("UDPSender").GetComponent<UDPSend>().OptoTurnOffAll();
        }
	}

	// WithholdReward() is called only on incorrect trials, not correct trials where reward was witheld
    private void WitholdReward() {
		float xPos = this.gameObject.transform.position.x;
		Globals.IncrementTurnToStimLoc (xPos);
		Color c = Color.white;
		int interTrialInterval = incorrectTurnDelay;

		// If probabilistic rewards given, make the visual cue and trial delay of an error the same as a correct trial, to discourage learning during testing.
		if (Globals.probReward < 1 && !Globals.probabilisticWhiteNoiseWhenNoReward) {
			c = Color.black;
			interTrialInterval = correctTurnDelay;
		}

		Globals.lastTrialWasIncorrect = 1;
        Globals.hasNotTurned = false;
        Globals.firstTurn.Add(this.gameObject.transform.position.x);
        Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
        Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
		Globals.firstTurnAngle.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderRotation());
        Globals.sizeOfRewardGiven.Add(0);
        if (respawn) {
			if (!Globals.CurrentlyCorrectionTrial ()) {
				Globals.numNonCorrectionTrials++;
			} else {  // Might be off by 1 bug here, as nonCorrectionTrials is the current trial #, but numCorrectionTrials is the total number of correction trials
				Globals.numCorrectionTrials++;
			}
			Globals.trialDelay = interTrialInterval;
            GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(c);
            Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
        }

        Globals.WriteToLogFiles();
		GameObject.Find("UDPSender").GetComponent<UDPSend>().OptoTurnOffAll();
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

    public void SetShader(float HFreq, float VFreq)
    {
		this.enabled = false; // Turns off Update routine while we fiddle with variables
        this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
        this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VPhase", 0);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HPhase", 0);
		this.enabled = true;
		//this.topCap.SetActive(true);
        //this.bottomCap.SetActive(true);
    }

	public void SetShader(float HFreq, float VFreq, float deg)
	{
		this.enabled = false; // Turns off Update routine while we fiddle with variables
		this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VPhase", 0);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HPhase", 0);
		this.enabled = true;
		//this.topCap.SetActive(true);
		//this.bottomCap.SetActive(true);
	}

	public void SetShader(float HFreq, float HPhase, float VFreq, float VPhase, float deg)
	{
		this.enabled = false; // Turns off Update routine while we fiddle with variables
		this.crown.GetComponent<Renderer>().material.SetFloat("_Deg", deg);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HPhase", HPhase);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VPhase", VPhase);
		this.enabled = true;
		//this.topCap.SetActive(true);
		//this.bottomCap.SetActive(true);
	}

	// Support for curvy trees - will fail if Curvy hasn't been set as the Shader material
	public void SetShader(float HFreq, float HPhase, float VFreq, float VPhase, float HAmplitude, float HNumCycles, float HWavePhase, float VAmplitude, float VNumCycles, float VWavePhase, float Smooth)
	{
		this.enabled = false; // Turns off Update routine while we fiddle with variables
		this.crown.GetComponent<Renderer>().material.SetFloat("_HFreq", HFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HPhase", HPhase);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VFreq", VFreq);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VPhase", VPhase);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HAmplitude", HAmplitude);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HNumCycles", HNumCycles);
		this.crown.GetComponent<Renderer>().material.SetFloat("_HWavePhase", HWavePhase);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VAmplitude", VAmplitude);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VNumCycles", VNumCycles);
		this.crown.GetComponent<Renderer>().material.SetFloat("_VWavePhase", VWavePhase);
		this.crown.GetComponent<Renderer>().material.SetFloat("_Smooth", Smooth);
		this.enabled = true;
	}

	public void SetOpacity(float opacity) {
		this.crown.GetComponent<Renderer>().material.SetFloat("_Transparency", opacity);
		Color c = this.bottomCap.GetComponent<Renderer> ().material.color;
		c.a = 0;
		this.bottomCap.GetComponent<Renderer> ().material.color = c;

		c = this.topCap.GetComponent<Renderer> ().material.color;
		c.a = 0;
		this.topCap.GetComponent<Renderer> ().material.color = c;

		c = this.trunk.GetComponent<Renderer> ().material.color;
		c.a = 0;
		this.trunk.GetComponent<Renderer> ().material.color = c;

		c = this.waterBase.GetComponent<Renderer> ().material.color;
		c.a = 0;
		this.waterBase.GetComponent<Renderer> ().material.color = c;
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
	public float ResetPhase()
	{
		return this.crown.GetComponent<Renderer>().material.GetFloat("_Deg");
	}		
    public void SetRewardSize(float r)
    {
        this.rewardDur = (int)Math.Round(r / (Globals.rewardSize / Globals.rewardDur));
        //Debug.Log("Reward duration set: " + this.rewardDur);
    }
	public void SetRewardMulti(float m) {
		this.rewardMulti = m;
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

	public void SetVCycPerSec(float vcps)
	{
		this.vCycPerSec = vcps;
	}
	public float GetVCycPerSec()
	{
		return this.vCycPerSec;
	}
	public void SetHCycPerSec(float hcps)
	{
		this.hCycPerSec = hcps;
	}
	public float GetHCycPerSec()
	{
		return this.hCycPerSec;
	}

    public void SetCorrect(bool c)
    {
        this.correctTree = c;
    }

	public void SetPresoFrac(float p) {
		this.presoFrac = p;
	}
	public float GetPresoFrac() {
		return this.presoFrac;
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

	public Color GetColor1() {
		return this.crown.GetComponent<Renderer> ().material.GetColor ("_Color1");
	}

	public Color GetColor2() {
		return this.crown.GetComponent<Renderer> ().material.GetColor ("_Color2");
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
			if (Globals.treeMarkers && t.name.Equals ("TopCap")) {
				Vector3 pos = t.position;
				pos.y = 0;
				t.position = pos;
				// Need to do both below to make it persistently visible regardless of angle and screen location
				t.gameObject.layer = LayerMask.NameToLayer ("Default");
				t.gameObject.GetComponent<Renderer> ().material.renderQueue = 2000;  // Sets RenderQueue to Geometry to make it visible on all screens
			} else {
				t.gameObject.SetActive (false);
			}
			//Debug.Log ("Inactivated = " + t.gameObject.name);
		}
		this.enabled = false;
		this.gameObject.GetComponent<CapsuleCollider> ().enabled = true;  // Something is making this false...
		//Debug.Log ("Collider is " + this.gameObject.GetComponent<CapsuleCollider> ().enabled);
	}

	public void Show()
	{
		foreach (Transform t in this.gameObject.transform) {
			if (Globals.treeMarkers && t.name.Equals ("TopCap")) {
				Vector3 pos = t.position;
				pos.y = 0;
				t.position = pos;
				t.gameObject.layer = LayerMask.NameToLayer ("Default");
				t.gameObject.GetComponent<Renderer> ().material.renderQueue = 2000;  // Sets RenderQueue to Geometry to make it visible on all screens
				//t.parent.transform.gameObject.layer = LayerMask.NameToLayer ("Default");
			}
			
			t.gameObject.SetActive (true);
			//Debug.Log ("Activated = " + t.gameObject.name);
		}
		this.enabled = true;
		this.gameObject.GetComponent<CapsuleCollider> ().enabled = true;
	}
}