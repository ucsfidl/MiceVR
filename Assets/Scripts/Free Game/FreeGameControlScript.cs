 using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class FreeGameControlScript : MonoBehaviour
{
    public int runDuration;
    public int numberOfRuns;
	public int numberOfAllRewards;
    public GameObject player;
    public GameObject menuPanel;
    public Image fadeToBlack;
    public Text fadeToBlackText;
    public Text rewardAmountText;
	public Text numberOfDryTreesText;
	public Text numberOfCorrectTurnsText;
	public Text numberOfTrialsText;
    public Text lastAccuracyText;
    public Text timeElapsedText;
    public ArduinoComm ard;
    public FreeMovementRecorder movementRecorder;

	private float rawSpeedDivider;  // Normally 60f; previously 200f
	private float rawRotationDivider;  // Normally 500f; previously -4000f
    private int respawnAmplitude = 2000;
    private int runNumber;
    private float runTime;
    private long frameCounter, previousFrameCounter;
    private System.Collections.Generic.Queue<float> last5Mouse2Y, last5Mouse1Y;
    public string state;
    private bool firstFrameRun;
    private bool playerInWaterTree, playerInDryTree;
    private FreeLoader scenarioLoader;
    private CharacterController characterController;
    private DebugControl debugControlScript;
    private bool timeoutState;

	private int smoothingWindow = 1;  // Amount to smoothen the player movement
	private bool waitedOneFrame = true;  // When mouse hits tree, need to wait a few frames before it turns black, and then pause the game

	private Vector3 startingPos;
	private Quaternion startingRot;
	private Vector3 prevPos;

	private int centralViewVisible;

	private DateTime lastRewardTime;
	private int minRewardInterval = 1000;

	private bool correctTrial = true;
	private int treeToActivate;

	private DateTime oldestStartPokeTime = DateTime.MinValue;
	private float sampleHFreq;
	private float sampleVFreq;
	private float sampleDeg = 0;
	private float nonSampleHFreq;
	private float nonSampleVFreq;
	private float nonSampleDeg = 0;
	private bool startTreeSet = false;

	private DateTime firstStimOnTime = DateTime.MinValue;
	private float disappearTreeDelay;

    // Use this for initialization
    void Start()
    {
		Debug.Log ("started!");
        this.frameCounter = this.previousFrameCounter = 0;
        this.runNumber = 1;
		this.last5Mouse1Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
		this.last5Mouse2Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
        this.state = "LoadScenario";
        this.firstFrameRun = false;
        this.scenarioLoader = GameObject.FindGameObjectWithTag("generator").GetComponent<FreeLoader>();
        this.characterController = GameObject.Find("FPSController").GetComponent<CharacterController>();
        this.debugControlScript = GameObject.Find("FPSController").GetComponent<DebugControl>();
        this.characterController.enabled = false;  // Keeps me from moving the character while typing entries into the form
        FreeGlobals.numberOfEarnedRewards = 0;
        FreeGlobals.numberOfUnearnedRewards = 0;
        FreeGlobals.rewardAmountSoFar = 0;
		FreeGlobals.numberOfDryTrees = 0;
		FreeGlobals.numberOfTrials = 0;  // Start on no trial
        this.timeoutState = false;

		this.startingPos = this.player.transform.position;
		this.startingRot = this.player.transform.rotation;

		this.prevPos = this.startingPos;

		// Will this fix the issue where rarely colliding with a wall causes mouse to fly above the wall?  No.
		this.characterController.enableOverlapRecovery = false;  

		/*
		GameObject ifld = GameObject.Find ("ScenarioInput");
		EventSystemManager.currentSystem.SetSelectedGameObject (ifld);
		Debug.Log ("focused!");
		*/

        init();
    }

    // Update is called once per frame
    void Update()
	{

		//Debug.Log ("Framerate: " + 1.0f / Time.deltaTime);
		CatchKeyStrokes ();

		//Debug.Log (this.state);

        switch (this.state)
        {
            case "LoadScenario":
                LoadScenario();
                break;

            case "StartGame":
                StartGame();
                break;

            case "Timeout":
                Timeout();
                break;

            case "Running":
                Run();
                break;

            case "Fading":
                Fade();
                break;

			case "Paused":
				Pause ();
				break;

            case "Reset":
                ResetScenario(Color.black);
                break;

            case "Respawn":
                Respawn();
                break;

            case "GameOver":
                GameOver();
                break;

            default:
                break;
        }
    }

    public void init()
    {
        if (!Directory.Exists(PlayerPrefs.GetString("configFolder")))
            Debug.Log("No config file");

        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        xmlDoc.LoadXml(File.ReadAllText(PlayerPrefs.GetString("configFolder") + "/gameConfig.xml", ASCIIEncoding.ASCII)); // load the file.

        XmlNodeList gameConfigList = xmlDoc.SelectNodes("document/config");

        string _runDuration = "";
        string _numberOfRuns = "";
		string _numberOfAllRewards = "";
		string _rawSpeedDivider = "";
		string _rawRotationDivider = "";
        string _rewardDur = "";
        string _centralViewVisible = "";
        string _rewardSize = "";

        foreach (XmlNode xn in gameConfigList)
        {
			_runDuration = xn["runDuration"].InnerText;
			_numberOfRuns = xn["numberOfRuns"].InnerText;
			_numberOfAllRewards = xn["numberOfAllRewards"].InnerText;
			_rawSpeedDivider = xn["rawSpeedDivider"].InnerText;
			_rawRotationDivider = xn["rawRotationDivider"].InnerText;
			_centralViewVisible = xn ["treeVisibleOnCenterScreen"].InnerText;
            _rewardDur = xn["rewardDur"].InnerText;
            _rewardSize = xn["rewardSize"].InnerText;
        }

        int.TryParse(_runDuration, out this.runDuration);
        int.TryParse(_numberOfRuns, out this.numberOfRuns);
		int.TryParse(_numberOfAllRewards, out this.numberOfAllRewards);
		float.TryParse(_rawSpeedDivider, out this.rawSpeedDivider);
		float.TryParse(_rawRotationDivider, out this.rawRotationDivider);
		int.TryParse (_centralViewVisible, out this.centralViewVisible);
        int.TryParse(_rewardDur, out FreeGlobals.rewardDur);
        float.TryParse(_rewardSize, out FreeGlobals.rewardSize);

        // Calculate tree view block value: 0 is full occlusion in the central screen = 120 degrees
        // 0.9 is full visibility with occluder pushed all the way to the screen
        FreeGlobals.centralViewVisibleShift = (float)(centralViewVisible * 0.58/120);  // 0.45/120

		//Debug.Log (FreeGlobals.centralViewVisibleShift);
        // trying to avoid first drops of water
        //this.udpSender.ForceStopSolenoid();
        //this.udpSender.setAmount(FreeGlobals.rewardDur);
        //this.udpSender.CheckReward();
    }

    private void CatchKeyStrokes()
    {
        if (Input.GetKey(KeyCode.Escape))
            this.state = "GameOver";
        
        if (!this.state.Equals("LoadScenario") || (this.state.Equals("LoadScenario") && EventSystem.current.currentSelectedGameObject == null))
        {
			int idx = -1;
			if (Input.GetKeyUp (KeyCode.U)) {
				idx = 0;
			} else if (Input.GetKeyUp (KeyCode.Y)) {
				idx = 1;
			} else if (Input.GetKeyUp (KeyCode.I)) {
				idx = 2;
			} else if (Input.GetKeyUp (KeyCode.Alpha7)) {
				idx = 3;
			} else if (Input.GetKeyUp (KeyCode.Alpha6)) {
				idx = 4;
			} else if (Input.GetKeyUp (KeyCode.Alpha8)) {
				idx = 5;
			}

			if (idx != -1) {
				int dur = FreeGlobals.freeRewardDur [idx];
				ard.sendReward (FreeGlobals.freeRewardSite [idx], dur);
				float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
				FreeGlobals.sizeOfRewardGiven.Add (rSize);
				FreeGlobals.rewardAmountSoFar += rSize;
			}
        }
    }

	private void TeleportToBeginning()
	{
		this.player.transform.position = this.startingPos;
		this.player.transform.rotation = this.startingRot;
	}

    /*
     * Waits until a tree config is loaded
     * */
    private void LoadScenario()
    {
        if (this.scenarioLoader.scenarioLoaded == true)
        {
            this.menuPanel.SetActive(false);
            this.state = "StartGame";
        }
    }

    /*
     * Waits for user input to start the game
     * */
    private void StartGame()
    {
        //Debug.Log ("In StartGame()");
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;
        this.fadeToBlackText.text = "Press SPACE to start";
		//Debug.Log ("waiting for space bar");
        if (Input.GetKeyUp(KeyCode.Space))
        {
            this.runTime = Time.time;
            FreeGlobals.gameStartTime = DateTime.Now;
            Debug.Log("Game started at " + FreeGlobals.gameStartTime.ToLongTimeString());
            this.movementRecorder.SetRun(this.runNumber);
            this.movementRecorder.SetFileSet(true);
            Color t = this.fadeToBlack.color;
            t.a = 0f;
            this.fadeToBlack.color = t;
            this.fadeToBlackText.text = "";
            this.fadeToBlack.gameObject.SetActive(false);

            this.firstFrameRun = true;
            this.debugControlScript.enabled = true;
			FreeGlobals.hasNotTurned = true;
			FreeGlobals.numCorrectTurns = 0;
            this.characterController.enabled = true;  // Bring back character movement
            this.state = "Running";

            FreeGlobals.InitLogFiles();
            //FreeGlobals.trialStartTime.Add(DateTime.Now.TimeOfDay);
        }
    }

    /*
     * dry trees timeout state
     * */
    private void Timeout()
    {
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;

        if (!FreeGlobals.timeoutState)
        {
			StartCoroutine(Wait());
            FreeGlobals.timeoutState = true;
        }
    }

    IEnumerator Wait()
    {
        // Maria: This is where we change seconds
        yield return new WaitForSeconds(15);

        Color t = this.fadeToBlack.color;
        t.a = 0f;
        this.fadeToBlack.color = t;
        this.fadeToBlackText.text = "";
        this.fadeToBlack.gameObject.SetActive(false);

        this.timeoutState = false;
        this.state = "Running";
    }

    /*
     * Send sync UDP.
     * Get UDP msgs and move the player
     * Send UDP msgs out with (pos, rot, inTree)
     */
    private void Run()
    {
		if (FreeGlobals.gameType.Equals ("nosepoke")) {			
			int rs = ard.CheckForMouseAction ();
//			if (rs == FreeGlobals.freeRewardSite [0] || rs == FreeGlobals.freeRewardSite[1] || rs == FreeGlobals.freeRewardSite[2]) {
			if (rs == FreeGlobals.freeRewardSite [0]) {
				if (DateTime.Now.Subtract (lastRewardTime).TotalMilliseconds > minRewardInterval) {				
					int dur = FreeGlobals.freeRewardDur [0];
					ard.sendReward (rs, dur);
					lastRewardTime = DateTime.Now;
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add (rSize);
					FreeGlobals.rewardAmountSoFar += rSize;
				}
			}
		} else if (FreeGlobals.gameType.Equals ("free_det")) {
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");
			switch (FreeGlobals.freeState) {

			case "pretrial":  // Mouse has not yet poked his nose in
				if (rs == FreeGlobals.freeRewardSite [0]) {
					if (!startTreeSet) {
						if (FreeGlobals.waterAtStart) {  // only give water if startTree not set, so only give water once
							int dur = FreeGlobals.freeRewardDur [rs / 2];
							ard.sendReward (rs, dur);
							lastRewardTime = DateTime.Now;
							float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
							FreeGlobals.sizeOfRewardGiven.Add (rSize);
							FreeGlobals.rewardAmountSoFar += rSize;
							startTreeSet = true;
						}

						// Bias correction
						// Had this OFF when training batch #1; this phase took 2-3 days
						/*
						float r = UnityEngine.Random.value;
						float[] rThresh = new float[gos.Length + 1];
						rThresh [0] = 0;
						rThresh [gos.Length] = 1;
						for (int i = 1; i < gos.Length; i++) {
							rThresh [i] = 1F / gos.Length * i;
						}

						if (FreeGlobals.numberOfTrials >= 1) {
							float[] bcs = new float[gos.Length];
							string bcsStr = "";
							for (int i = 0; i < gos.Length; i++) {
								bcs [i] = 1 - FreeGlobals.GetTurnBias (20, i);
								bcsStr += bcs [i] + " ";
							}
							float s = bcs.Sum ();
							Debug.Log (bcsStr);
							for (int i = 0; i < gos.Length - 1; i++) {
								bcs [i] = bcs [i] / s;  // Normalize all the bias corrections
								rThresh[i+1] = rThresh[i] + bcs[i];
							}
						}

						this.treeToActivate = 0;
						for (int i = 1; i < gos.Length+1; i++) {
							if (r >= rThresh [i - 1] && r <= rThresh [i])
								treeToActivate = i - 1;
						}


						SetupTreeActivation (gos, treeToActivate, gos.Length);
						string threshStr = "";
						for (int i = 1; i < rThresh.Length-1; i++) {
							threshStr += rThresh [i] + ", ";
						}
						Debug.Log("[0, " + threshStr + "1] - " + r);

						FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
						FreeGlobals.targetHFreq.Add(gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq());
						FreeGlobals.targetVFreq.Add(gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq());
						*/

						// No bias correction
						float r = UnityEngine.Random.value;
						if (r < 0.5) 
							this.treeToActivate = 0;
						else
							this.treeToActivate = 1;
						
						FreeGlobals.targetLoc.Add (gos [this.treeToActivate].transform.position.x);
						FreeGlobals.targetHFreq.Add (gos [this.treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
						FreeGlobals.targetVFreq.Add (gos [this.treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
						FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());
							
						FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);
						FreeGlobals.numberOfTrials++;

						// If the stim is persistent, log appropriate values
						if (FreeGlobals.persistenceDur != -1) {
							FreeGlobals.stimDur.Add (0);
							FreeGlobals.stimReps.Add (0);
						} else if (!FreeGlobals.stimPersists) {
							FreeGlobals.stimDur.Add (0);
							FreeGlobals.stimReps.Add (0);
							firstStimOnTime = DateTime.Now;
						} else {
							FreeGlobals.stimDur.Add (-1);
							FreeGlobals.stimReps.Add (1);
						}
					}

					SetupTreeActivation (gos, this.treeToActivate, 2);

					if (FreeGlobals.persistenceDur != -1) {
						DisappearTreeHelper ();
					}

					oldestStartPokeTime = DateTime.Now;  // Set time, to be evaluated in the stim_on state if startRewardDelay > 0
					FreeGlobals.freeState = "choice";
					startTreeSet = true;
				}
				break;
				
			case "choice":  // Mouse has poked his nose in, so only reward him if he goes to the correct lickport
				if (!FreeGlobals.stimPersists) {  // 1 is sent from arduino when break beam is made intact at startport
					if (rs == 1 && !firstStimOnTime.Equals(DateTime.MinValue)) { // Mouse pulled nose out of startport
						DisappearImpersistentTree ();
					} else if (rs == 0 && firstStimOnTime.Equals(DateTime.MinValue)) {  // Mouse poked nose back in to startport
						SetupTreeActivation (gos, this.treeToActivate, gos.Length);
						firstStimOnTime = DateTime.Now;
					}
				}

				if (FreeGlobals.startRewardDelay > 0) { // Make sure mouse has kept nose in for the minimum amount of time, else stim disappear
					if (rs == 1) { // Mouse pulled out nose from startport
						if (DateTime.Now.Subtract (oldestStartPokeTime).TotalMilliseconds < FreeGlobals.startRewardDelay) {
							FreeGlobals.freeState = "pretrial"; // go back to pretrial state
							SetupTreeActivation (gos, -1, gos.Length);	// Hide all trees
						}
					}
				}

				if ((FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [0].transform.position.x) &&
				    rs == FreeGlobals.freeRewardSite [1]) || // left tree is on and the mouse licked the lickport there
				    (FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [1].transform.position.x) &&
				    rs == FreeGlobals.freeRewardSite [2])) { // Reft tree is on and the mouse licked the lickport there
					int dur = FreeGlobals.freeRewardDur [rs / 2];
					Debug.Log ("in nosepoke");
					ard.sendReward (rs, dur);
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add (rSize);
					FreeGlobals.rewardAmountSoFar += rSize;

					SetupTreeActivation (gos, -1, 2); // Hide all trees 

					if (correctTrial) {
						FreeGlobals.numCorrectTurns++;
						FreeGlobals.firstTurn.Add (gos [rs / 2 - 1].transform.position.x);
						FreeGlobals.firstTurnHFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
						FreeGlobals.firstTurnVFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
						FreeGlobals.firstTurnDeg.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderRotation ());
						Debug.Log ("correct");
					}

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);
					FreeGlobals.WriteToLogFiles ();

					correctTrial = true;
					FreeGlobals.freeState = "pretrial";
					startTreeSet = false;
				} else if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) {
					if (correctTrial) {
						FreeGlobals.firstTurn.Add (gos [rs / 2 - 1].transform.position.x);
						FreeGlobals.firstTurnHFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
						FreeGlobals.firstTurnVFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
						FreeGlobals.firstTurnDeg.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderRotation ());
					}
					correctTrial = false;
					Debug.Log ("incorrect");
				}
				break;
			} 
		} else if (FreeGlobals.gameType.Equals ("free_det_const")) {  // constrained - forced to learn, won't get reward otherwise
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");
			switch (FreeGlobals.freeState) {

			case "pretrial":  // Mouse has not yet poked his nose in
				if (rs == FreeGlobals.freeRewardSite [0]) {
					if (!startTreeSet) {
						if (FreeGlobals.waterAtStart) {
							int dur = FreeGlobals.freeRewardDur [rs / 2];
							ard.sendReward (rs, dur);
							lastRewardTime = DateTime.Now;
							float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
							FreeGlobals.sizeOfRewardGiven.Add (rSize);
							FreeGlobals.rewardAmountSoFar += rSize;
						}

						float r = UnityEngine.Random.value;
						float[] rThresh = new float[gos.Length + 1];
						rThresh [0] = 0;
						rThresh [gos.Length] = 1;
						for (int i = 1; i < gos.Length; i++) {
							rThresh [i] = 1F / gos.Length * i;
						}

						// Bias correction
						// This was ON when training batch#1, which learned within 3 days
						if (FreeGlobals.numberOfTrials >= 1) {
							float[] bcs = new float[gos.Length];
							string bcsStr = "";
							for (int i = 0; i < gos.Length; i++) {
								bcs [i] = 1 - FreeGlobals.GetTurnBias (20, i);
								bcsStr += bcs [i] + " ";
							}
							float s = bcs.Sum ();
							Debug.Log (bcsStr);
							for (int i = 0; i < gos.Length - 1; i++) {
								bcs [i] = bcs [i] / s;  // Normalize all the bias corrections
								rThresh [i + 1] = rThresh [i] + bcs [i];
							}
						}
	 	
						this.treeToActivate = 0;
						for (int i = 1; i < gos.Length + 1; i++) {
							if (r >= rThresh [i - 1] && r <= rThresh [i])
								treeToActivate = i - 1;
						}

						string threshStr = "";
						for (int i = 1; i < rThresh.Length - 1; i++) {
							threshStr += rThresh [i] + ", ";
						}
						Debug.Log ("[0, " + threshStr + "1] - " + r);

						FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
						FreeGlobals.targetHFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
						FreeGlobals.targetVFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
						FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());

						FreeGlobals.numberOfTrials++;
						FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);

						// If the stim is persistent, log appropriate values
						if (FreeGlobals.persistenceDur != -1) {
							FreeGlobals.stimDur.Add (0);
							FreeGlobals.stimReps.Add (0);
						} else if (!FreeGlobals.stimPersists) {
							FreeGlobals.stimDur.Add (0);
							FreeGlobals.stimReps.Add (0);
							firstStimOnTime = DateTime.Now;
							Debug.Log (firstStimOnTime.ToString());
						} else {
							FreeGlobals.stimDur.Add (-1);
							FreeGlobals.stimReps.Add (1);
						}
					}

					SetupTreeActivation (gos, this.treeToActivate, gos.Length);

					if (FreeGlobals.persistenceDur != -1) {
						DisappearTreeHelper ();
					}

					oldestStartPokeTime = DateTime.Now;  // Set time, to be evaluated in the stim_on state if startRewardDelay > 0

					startTreeSet = true;
					FreeGlobals.freeState = "stim_on";
				}
				break;

			case "stim_on":  // Mouse has poked his nose in, so only reward him if he goes to the correct lickport
				if (!FreeGlobals.stimPersists) {  // 1 is sent from arduino when break beam is made intact at startport
					if (rs == 1 && !firstStimOnTime.Equals(DateTime.MinValue)) { // Mouse pulled nose out of startport
						DisappearImpersistentTree ();
					} else if (rs == 0 && firstStimOnTime.Equals(DateTime.MinValue)) {  // Mouse poked nose back in to startport
						SetupTreeActivation (gos, this.treeToActivate, gos.Length);
						firstStimOnTime = DateTime.Now;
					}
				}

				if (FreeGlobals.persistenceDur != -1) {  // Tree might have disappeared, in which case bring it back
					if (rs == 0) { // Mouse put nose back in startport
						SetupTreeActivation (gos, this.treeToActivate, gos.Length);
						DisappearTreeHelper ();
					}
				}

				if (FreeGlobals.startRewardDelay > 0) { // Make sure mouse has kept nose in for the minimum amount of time, else stim disappear
					if (rs == 1) { // Mouse pulled out nose from startport
						if (DateTime.Now.Subtract (oldestStartPokeTime).TotalMilliseconds < FreeGlobals.startRewardDelay) {
							FreeGlobals.freeState = "pretrial"; // go back to pretrial state
							SetupTreeActivation (gos, -1, gos.Length);	// Hide all trees
						}
					}
				}

				if ((gos.Length == 2 && (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2])) ||
				    (gos.Length == 4 && (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) ||
				    rs == FreeGlobals.freeRewardSite [4] || rs == FreeGlobals.freeRewardSite [5])) { // licked at 1 of 2 lick ports
					int idx = rs / 2 - 1;
					if (rs == FreeGlobals.freeRewardSite [4] || rs == FreeGlobals.freeRewardSite [5])
						idx = idx - 1;
					FreeGlobals.firstTurn.Add (gos [idx].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [idx].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.firstTurnDeg.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);

					SetupTreeActivation (gos, -1, gos.Length); // Hide all trees 
					FreeGlobals.freeState = "pretrial";
					startTreeSet = false;

					if ((FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [0].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [1]) || // left tree is on and the mouse licked the lickport there
					    (FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [1].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [2]) ||
					    (gos.Length == 4 &&
					    ((FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [2].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [4]) ||
					    (FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [3].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [5])))) { 
						int dur = FreeGlobals.freeRewardDur [rs / 2];
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
					} else {  // If wrong choice, show white noise for 4 sec
						FreeGlobals.sizeOfRewardGiven.Add (0);
						FreeGlobals.trialDelay = 6;
						this.fadeToBlack.gameObject.SetActive (true);
						this.fadeToBlack.color = Color.white;
						this.state = "Paused";
					}
					FreeGlobals.WriteToLogFiles ();
				}

				break;
			} 
		} else if (FreeGlobals.gameType.Equals ("free_det_blind")) {
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");
			switch (FreeGlobals.freeState) {

			case "pretrial":  // Mouse has not yet poked his nose in
				if (rs == FreeGlobals.freeRewardSite [0]) {
					if (FreeGlobals.waterAtStart) {
						int dur = FreeGlobals.freeRewardDur [rs / 2];
						ard.sendReward (rs, dur);
						lastRewardTime = DateTime.Now;
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;
					}

					float r = UnityEngine.Random.value;
					double thresh0 = 0.333;
					double thresh1 = 0.666;

					// Bias correction - turned back on
					float tf0 = FreeGlobals.GetTurnBias (20, 0);
					float tf1 = FreeGlobals.GetTurnBias (20, 1);

					if (!double.IsNaN (tf0) && !double.IsNaN (tf1)) {
						float tf2 = 1 - (tf0 + tf1);

						Debug.Log ("turning biases: " + tf0 + ", " + tf1 + ", " + tf2);

						// Solve
						double p0 = tf0 < 1 / 3 ? -2 * tf0 + 1 : -tf0 / 2 + 0.5;
						double p1 = tf1 < 1 / 3 ? -2 * tf1 + 1 : -tf1 / 2 + 0.5;
						double p2 = tf2 < 1 / 3 ? -2 * tf2 + 1 : -tf2 / 2 + 0.5;

						Debug.Log ("raw trial prob: " + p0 + ", " + p1 + ", " + p2);

						// Rebalance, pushing mouse to lowest freq direction
						double d;
						double max = Math.Max (tf0, Math.Max (tf1, tf2));
						double min = Math.Min (tf0, Math.Min (tf1, tf2));
						if (max - min > 0.21) { // Only rebalance if there is a big difference between the choices
							double pmax = Math.Max (p0, Math.Max (p1, p2));
							if (p0 == pmax) {
								d = Math.Abs (p1 - p2);
								p1 *= d;
								p2 *= d;
							} else if (p1 == pmax) {
								d = Math.Abs (p0 - p2);
								p0 *= d;
								p2 *= d;
							} else if (p2 == pmax) {
								d = Math.Abs (p0 - p1);
								p0 *= d;
								p1 *= d;
							}
						}

						// Normalize so all add up to 1
						p0 = p0 / (p0 + p1 + p2);
						p1 = p1 / (p0 + p1 + p2);
						p2 = p2 / (p0 + p1 + p2);

						thresh0 = p0;
						thresh1 = thresh0 + p1;
					}

					Debug.Log ("[0, " + thresh0 + ", " + thresh1 + ", 1] - " + r);
					this.treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : 2;
					SetupTreeActivation (gos, treeToActivate, 2);  // Activate 1 of the 2 eccentric trees if necessary
					gos [2].GetComponent<WaterTreeScript> ().Show ();  // Always activate the central tree

					FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
					FreeGlobals.targetHFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.targetVFreq.Add (gos [treeToActivate].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.freeState = "stim_on";
					FreeGlobals.numberOfTrials++;
					FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);

					// If the stim is persistent, log appropriate values
					if (FreeGlobals.persistenceDur == -1) {
						FreeGlobals.stimDur.Add (-1);
						FreeGlobals.stimReps.Add (1);
					} else {
						FreeGlobals.stimDur.Add (0);
						FreeGlobals.stimReps.Add (0);
						DisappearTreeHelper ();
					}
				
				}
				break;

			case "stim_on":  // Mouse has poked his nose in, so only reward him if he goes to the correct lickport
				if (!FreeGlobals.stimPersists) {  // 1 is sent from arduino when break beam is made intact at startport
					if (rs == 1) {  // Mouse pulled nose out of startport
						SetupTreeActivation (gos, -1, gos.Length);
					} else if (rs == 0) {
						SetupTreeActivation (gos, this.treeToActivate, 2);
						gos [2].GetComponent<WaterTreeScript> ().Show ();  // Always activate the central tree
					}
				}

				if (FreeGlobals.persistenceDur != -1) {  // Tree might have disappeared, in which case bring it back
					if (rs == 0) { // Mouse put nose back in startport
						SetupTreeActivation (gos, this.treeToActivate, 2);
						gos [2].GetComponent<WaterTreeScript> ().Show ();  // Always activate the central tree
						DisappearTreeHelper ();
					}
				}
					
				if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2] ||
				    rs == FreeGlobals.freeRewardSite [3]) { // licked at 1 of 3 lick ports
					FreeGlobals.firstTurn.Add (gos [rs / 2 - 1].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [rs / 2 - 1].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.firstTurnDeg.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);

					SetupTreeActivation (gos, -1, 3); // Hide all trees 
					FreeGlobals.freeState = "pretrial";

					if ((FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [0].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [1]) || // left tree is on and the mouse licked the lickport there
					    (FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [1].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [2]) || // Right tree is on and the mouse licked the lickport there
					    (FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [2].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [3])) { // Center tree is on and the mouse licked the lickport there 
						int dur = FreeGlobals.freeRewardDur [rs / 2];
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
					} else {  // If wrong choice, show white noise for 4 sec
						FreeGlobals.sizeOfRewardGiven.Add (0);
						FreeGlobals.trialDelay = 6;
						this.fadeToBlack.gameObject.SetActive (true);
						this.fadeToBlack.color = Color.white;
						this.state = "Paused";
					}
					FreeGlobals.WriteToLogFiles ();
				}
				break;
			} 
		} else if (FreeGlobals.gameType.Equals ("free_dmts")) {  
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");

			switch (FreeGlobals.freeState) {
			case "pretrial":  // Mouse has not yet poked his nose into the startport
				if (rs == FreeGlobals.freeRewardSite [0]) {  // Mouse poked nose into the startport
					if (!startTreeSet) {
						// Center tree is the third tree always, so turn that on with a random orientation
						// No bias correction with orientation of the sample - purely random
						float rSample = UnityEngine.Random.value;
						if (rSample < 0.5) { // Sample will be horizontal tree
							gos [2].GetComponent<WaterTreeScript> ().SetShader (FreeGlobals.rewardedHFreq, 1);
							sampleHFreq = FreeGlobals.rewardedHFreq;
							sampleVFreq = 1;
							nonSampleHFreq = 1;
							nonSampleVFreq = FreeGlobals.rewardedVFreq;
						} else { // Sample will be vertical tree
							gos [2].GetComponent<WaterTreeScript> ().SetShader (1, FreeGlobals.rewardedVFreq);
							sampleHFreq = 1;
							sampleVFreq = FreeGlobals.rewardedVFreq;
							nonSampleHFreq = FreeGlobals.rewardedHFreq;
							nonSampleVFreq = 1;
						}
						SetupTreeActivation (gos, 2, gos.Length);

						// Setup side trees to turn on after the choice delay
						float rCand = UnityEngine.Random.value;
						float[] rThresh = new float[3];
						rThresh [0] = 0;
						rThresh [2] = 1;
						for (int i = 1; i < 2; i++) {
							rThresh [i] = 1F / 2 * i;
						}

						// Bias correction
						// This was ON when training batch#1, which learned within 3 days
						if (FreeGlobals.numberOfTrials >= 1) {
							float[] bcs = new float[2];
							string bcsStr = "";
							for (int i = 0; i < 2; i++) {
								bcs [i] = 1 - FreeGlobals.GetTurnBias (20, i);
								bcsStr += bcs [i] + " ";
							}
							float s = bcs.Sum ();
							Debug.Log (bcsStr);
							for (int i = 0; i < 2 - 1; i++) {
								bcs [i] = bcs [i] / s;  // Normalize all the bias corrections
								rThresh [i + 1] = rThresh [i] + bcs [i];
							}
						}

						this.treeToActivate = 0;  // treeToActivate is actually the side of the rewarded tree - both will be activated in this scenario
						for (int i = 1; i < 2 + 1; i++) {
							if (rCand >= rThresh [i - 1] && rCand <= rThresh [i])
								this.treeToActivate = i - 1;
						}

						// Some debug output to confirm bias correction is working reasonably
						string threshStr = "";
						for (int i = 1; i < rThresh.Length - 1; i++) {
							threshStr += rThresh [i] + ", ";
						}
						Debug.Log ("[0, " + threshStr + "1] - " + rCand);

						// Now that I know which side tree will be rewarded, alter its stripes to match the sample
						gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq);
						// Alter shading of the non-rewarded tree
						if (treeToActivate == 0)
							gos [1].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq);
						else
							gos [0].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq);

						// Record the state to the log history kept in memory
						FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
						FreeGlobals.targetHFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
						FreeGlobals.targetVFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
						FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());

						FreeGlobals.numberOfTrials++;
						FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);

						FreeGlobals.stimDur.Add (-1);
						FreeGlobals.stimReps.Add (1);
					}

					// These side trees will only be activated after the choiceDelay
					if (FreeGlobals.startRewardDelay == 0) {
						if (FreeGlobals.choiceDelay > 0) {
							Invoke ("ChoicesAppear", FreeGlobals.choiceDelay / 1000);
						} else {  // Don't allow negative values for choiceDelay!
							Invoke ("ChoicesAppear", 0);
						}
						FreeGlobals.freeState = "sample_on";
					} else if (oldestStartPokeTime == DateTime.MinValue) {
						oldestStartPokeTime = DateTime.Now;
						startTreeSet = true;
					} else if (oldestStartPokeTime != DateTime.MinValue) {
						if (DateTime.Now.Subtract (oldestStartPokeTime).TotalMilliseconds > FreeGlobals.startRewardDelay) {
							if (FreeGlobals.waterAtStart) {
								int dur = FreeGlobals.freeRewardDur [rs / 2];
								ard.sendReward (rs, dur);
								lastRewardTime = DateTime.Now;
								float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
								FreeGlobals.sizeOfRewardGiven.Add (rSize);
								FreeGlobals.rewardAmountSoFar += rSize;
							}
							FreeGlobals.freeState = "sample_on";
						}
					}
				} else if (rs == 1) {  // mouse pulled nose out of startport
					oldestStartPokeTime = DateTime.MinValue;  // reset oldest start time to start over, forcing mouse to hold nose in for startRewardDelay duration
				}
				break;

			case "sample_on": 
				if (FreeGlobals.startRewardDelay > 0) {  // This means we are in DMTS #2, Sabrina's design
					/* Design #1 for Sabrina's DMTS
					if (rs == 1) {  // Mouse pulled out of the noseport after startRewardDelay
						ChoicesAppear ();
					}
					*/
					// Design #2 - Choices appear after time has elapsed, regardless of whether the mouse has pulled out of the noseport
					ChoicesAppear();

					oldestStartPokeTime = DateTime.MinValue;  // reset oldest start time for advancing to next level
					startTreeSet = false;
				}
				break;

			case "choices_on":  // Mouse has poked his nose in to start, and sample appeared, and after some delay, choices appeared
				if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) { // licked at 1 of 2 lick ports
					int idx = rs / 2 - 1;

					// Log the decision
					FreeGlobals.firstTurn.Add (gos [idx].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [idx].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.firstTurnDeg.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);

					if (idx == this.treeToActivate) {  // Mouse chose the right tree, so give reward and log it!
						int dur = FreeGlobals.freeRewardDur [rs / 2]; // This is not quite right - won't work if trees get unequal reward
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
						FreeGlobals.trialDelay = 3;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
					} else {  // Mouse chose the non-matching tree, so withold reward and log it!
						FreeGlobals.sizeOfRewardGiven.Add (0);
						FreeGlobals.trialDelay = 1.5F;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
						Debug.Log ("incorrect");
					}
					FreeGlobals.WriteToLogFiles ();

					Invoke ("DisappearAllTrees", FreeGlobals.trialDelay);
					//SetupTreeActivation (gos, -1, gos.Length); // Hide all trees to reset the task
					FreeGlobals.freeState = "pretrial";
				}
				break;
			} 
		} else if (FreeGlobals.gameType.Equals ("free_alt_mismatch")) {  
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");

			switch (FreeGlobals.freeState) {
			case "pretrial":  // Mouse has not yet poked his nose into the startport
				if (rs == FreeGlobals.freeRewardSite [0]) {  // Mouse poked nose into the startport
					// Setup side trees to turn on after the choice delay
					float rCand = UnityEngine.Random.value;
					float[] rThresh = new float[3];
					rThresh [0] = 0;
					rThresh [2] = 1;
					for (int i = 1; i < 2; i++) {
						rThresh [i] = 1F / 2 * i;
					}

					// Bias correction
					// This was ON when training batch#1, which learned within 3 days
					if (FreeGlobals.numberOfTrials >= 1) {
						float[] bcs = new float[2];
						string bcsStr = "";
						for (int i = 0; i < 2; i++) {
							bcs [i] = 1 - FreeGlobals.GetTurnBias (20, i);
							bcsStr += bcs [i] + " ";
						}
						float s = bcs.Sum ();
						Debug.Log (bcsStr);
						for (int i = 0; i < 2 - 1; i++) {
							bcs [i] = bcs [i] / s;  // Normalize all the bias corrections
							rThresh [i + 1] = rThresh [i] + bcs [i];
						}
					}

					this.treeToActivate = 0;  // treeToActivate is actually the side of the rewarded tree - both will be activated in this scenario
					for (int i = 1; i < 2 + 1; i++) {
						if (rCand >= rThresh [i - 1] && rCand <= rThresh [i])
							this.treeToActivate = i - 1;
					}

					// Some debug output to confirm bias correction is working reasonably
					string threshStr = "";
					for (int i = 1; i < rThresh.Length - 1; i++) {
						threshStr += rThresh [i] + ", ";
					}
					Debug.Log ("[0, " + threshStr + "1] - " + rCand);

					// Now that I know which side tree will be rewarded, alter its stripes to mismatch the animal's prev choice
					if (FreeGlobals.numberOfTrials == 0) { // This is the first trial, so pick horiz vs vert randomly as the rewarded tree
						float rOri = UnityEngine.Random.value;
						if (rOri < 0.5) {
							sampleHFreq = FreeGlobals.rewardedHFreq;
							sampleVFreq = 1;
							nonSampleHFreq = 1;
							nonSampleVFreq = FreeGlobals.rewardedVFreq;
						} else {
							sampleHFreq = 1;
							sampleVFreq = FreeGlobals.rewardedVFreq;
							nonSampleHFreq = FreeGlobals.rewardedHFreq;
							nonSampleVFreq = 1;
						}
					} else { // If not the first trial, set the rewarded orientation to be different from the animal's last choice
						if (FreeGlobals.firstTurnHFreq [FreeGlobals.firstTurnHFreq.Count - 1].Equals(FreeGlobals.rewardedHFreq)) {
							// Last choice was the horizontal orientation, so make vertical the rewarded tree this time
							sampleHFreq = 1;
							sampleVFreq = FreeGlobals.rewardedVFreq;
							nonSampleHFreq = FreeGlobals.rewardedHFreq;
							nonSampleVFreq = 1;
						} else {
							sampleHFreq = FreeGlobals.rewardedHFreq;
							sampleVFreq = 1;
							nonSampleHFreq = 1;
							nonSampleVFreq = FreeGlobals.rewardedVFreq;
						}
					}

					gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq);
					// Alter shading of the non-rewarded tree
					if (treeToActivate == 0)
						gos [1].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq);
					else
						gos [0].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq);

					// Record the state to the log history kept in memory
					FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
					FreeGlobals.targetHFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.targetVFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.numberOfTrials++;
					FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);

					FreeGlobals.stimDur.Add (-1);
					FreeGlobals.stimReps.Add (1);

					ChoicesAppear ();  // Display the trees
				}
				break;

			case "choices_on":  // Mouse has poked his nose in to start, and the choices have appeared
				if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) { // licked at 1 of 2 lick ports
					int idx = rs / 2 - 1;

					// Log the decision
					FreeGlobals.firstTurn.Add (gos [idx].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [idx].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.firstTurnDeg.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);


					if (idx == this.treeToActivate) {  // Mouse chose the right tree, so give reward and log it!
						int dur = FreeGlobals.freeRewardDur [rs / 2]; // This is not quite right - won't work if trees get unequal reward
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
						FreeGlobals.trialDelay = 3;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
					} else {  // Mouse chose the non-matching tree, so withold reward and log it!
						FreeGlobals.sizeOfRewardGiven.Add (0);
						FreeGlobals.trialDelay = 1.5F;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
						Debug.Log ("incorrect");
					}
					FreeGlobals.WriteToLogFiles ();

					Invoke ("DisappearAllTrees", FreeGlobals.trialDelay);
					//SetupTreeActivation (gos, -1, gos.Length); // Hide all trees to reset the task
					FreeGlobals.freeState = "pretrial";
				}
				break;
			}
		} else if (FreeGlobals.gameType.Equals ("free_disc")) {  
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");

			switch (FreeGlobals.freeState) {
			case "pretrial":  // Mouse has not yet poked his nose into the startport
				if (rs == FreeGlobals.freeRewardSite [0]) {  // Mouse poked nose into the startport
					// Setup side trees to turn on after the choice delay
					if (FreeGlobals.numPorts == 2) {
						float rCand = UnityEngine.Random.value;
						float[] rThresh = new float[3];
						rThresh [0] = 0;
						rThresh [2] = 1;
						for (int i = 1; i < 2; i++) {
							rThresh [i] = 1F / 2 * i;
						}

						// Bias correction
						// This was ON when training batch#1, which learned within 3 days
						if (FreeGlobals.numberOfTrials >= 1) {
							float[] bcs = new float[2];
							string bcsStr = "";
							for (int i = 0; i < 2; i++) {
								bcs [i] = 1 - FreeGlobals.GetTurnBias (20, i);
								bcsStr += bcs [i] + " ";
							}
							float s = bcs.Sum ();
							Debug.Log (bcsStr);
							for (int i = 0; i < 2 - 1; i++) {
								bcs [i] = bcs [i] / s;  // Normalize all the bias corrections
								rThresh [i + 1] = rThresh [i] + bcs [i];
							}
						}

						this.treeToActivate = 0;  // treeToActivate is actually the side of the rewarded tree - both will be activated in this scenario
						for (int i = 1; i < 2 + 1; i++) {
							if (rCand >= rThresh [i - 1] && rCand <= rThresh [i])
								this.treeToActivate = i - 1;
						}

						// Some debug output to confirm bias correction is working reasonably
						string threshStr = "";
						for (int i = 1; i < rThresh.Length - 1; i++) {
							threshStr += rThresh [i] + ", ";
						}
						Debug.Log ("[0, " + threshStr + "1] - " + rCand);
					} else if (FreeGlobals.numPorts == 3) {
						float r = UnityEngine.Random.value;
						double thresh0 = 0.333;
						double thresh1 = 0.666;

						// Bias correction - turned back on
						float tf0 = FreeGlobals.GetTurnBias (20, 0);
						float tf1 = FreeGlobals.GetTurnBias (20, 1);

						if (!double.IsNaN (tf0) && !double.IsNaN (tf1)) {
							float tf2 = 1 - (tf0 + tf1);

							Debug.Log ("turning biases: " + tf0 + ", " + tf1 + ", " + tf2);

							// Solve
							double p0 = tf0 < 1 / 3 ? -2 * tf0 + 1 : -tf0 / 2 + 0.5;
							double p1 = tf1 < 1 / 3 ? -2 * tf1 + 1 : -tf1 / 2 + 0.5;
							double p2 = tf2 < 1 / 3 ? -2 * tf2 + 1 : -tf2 / 2 + 0.5;

							Debug.Log ("raw trial prob: " + p0 + ", " + p1 + ", " + p2);

							// Rebalance, pushing mouse to lowest freq direction
							double d;
							double max = Math.Max (tf0, Math.Max (tf1, tf2));
							double min = Math.Min (tf0, Math.Min (tf1, tf2));
							if (max - min > 0.21) { // Only rebalance if there is a big difference between the choices
								double pmax = Math.Max (p0, Math.Max (p1, p2));
								if (p0 == pmax) {
									d = Math.Abs (p1 - p2);
									p1 *= d;
									p2 *= d;
								} else if (p1 == pmax) {
									d = Math.Abs (p0 - p2);
									p0 *= d;
									p2 *= d;
								} else if (p2 == pmax) {
									d = Math.Abs (p0 - p1);
									p0 *= d;
									p1 *= d;
								}
							}

							// Normalize so all add up to 1
							p0 = p0 / (p0 + p1 + p2);
							p1 = p1 / (p0 + p1 + p2);
							p2 = p2 / (p0 + p1 + p2);

							thresh0 = p0;
							thresh1 = thresh0 + p1;
						}

						Debug.Log ("[0, " + thresh0 + ", " + thresh1 + ", 1] - " + r);
						this.treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : 2;
					}

					if (FreeGlobals.numStim == 2) {
						// This need not be set each trial, but whatever
						if (FreeGlobals.rewardedOri.Equals ("h")) {
							sampleHFreq = FreeGlobals.rewardedHFreq;
							sampleVFreq = 1;
							nonSampleHFreq = 1;
							nonSampleVFreq = FreeGlobals.rewardedVFreq;
						} else if (FreeGlobals.rewardedOri.Equals ("v")) {
							sampleHFreq = 1;
							sampleVFreq = FreeGlobals.rewardedVFreq;
							nonSampleHFreq = FreeGlobals.rewardedHFreq;
							nonSampleVFreq = 1;
						} 
					} else if (FreeGlobals.numStim == 3) {
						if (FreeGlobals.rewardedOri.Equals ("h")) {
							sampleHFreq = FreeGlobals.rewardedHFreq;
							sampleVFreq = 1;
							nonSampleHFreq = 1;
							nonSampleVFreq = FreeGlobals.rewardedVFreq;
							nonSampleDeg = UnityEngine.Random.value < 0.5 ? 0 : 45;  // distractor will be vertical or oblique
						} else if (FreeGlobals.rewardedOri.Equals ("v")) {
							sampleHFreq = 1;
							sampleVFreq = FreeGlobals.rewardedVFreq;
							if (UnityEngine.Random.value < 0.5) {  // distractor is horizontal
								nonSampleHFreq = FreeGlobals.rewardedHFreq;
								nonSampleVFreq = 1;
							} else {  // distractor is oblique
								nonSampleHFreq = 1;
								nonSampleVFreq = FreeGlobals.rewardedVFreq;
								nonSampleDeg = 45;
							}
						} else if (FreeGlobals.rewardedOri.Equals ("o")) {
							sampleHFreq = 1;
							sampleVFreq = FreeGlobals.rewardedVFreq;
							sampleDeg = 45;
							if (UnityEngine.Random.value < 0.5) {  // distractor is horizontal
								nonSampleHFreq = FreeGlobals.rewardedHFreq;
								nonSampleVFreq = 1;
							} else {  // distractor is vertical
								nonSampleHFreq = 1;
								nonSampleVFreq = FreeGlobals.rewardedVFreq;
							}
						}
					}

					gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, sampleDeg);
					if (FreeGlobals.numPorts == 2) {
						// Alter shading of the non-rewarded tree
						if (treeToActivate == 0)
							gos [1].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq, nonSampleDeg);
						else
							gos [0].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq, nonSampleDeg);
					} else if (FreeGlobals.numPorts == 3) {
						int distractorLoc;
						if (treeToActivate == 0) {
							if (UnityEngine.Random.value < 0.5)
								distractorLoc = 1;
							else
								distractorLoc = 2;
						} else if (treeToActivate == 1) {
							if (UnityEngine.Random.value < 0.5)
								distractorLoc = 0;
							else
								distractorLoc = 2;
						} else if (treeToActivate == 2) {
							if (UnityEngine.Random.value < 0.5)
								distractorLoc = 0;
							else
								distractorLoc = 1;
						}
						gos [distractorLoc].GetComponent<WaterTreeScript> ().SetShader (nonSampleHFreq, nonSampleVFreq, nonSampleDeg);
					}

					// Record the state to the log history kept in memory
					FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
					FreeGlobals.targetHFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.targetVFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.numberOfTrials++;
					FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);

					FreeGlobals.stimDur.Add (-1);
					FreeGlobals.stimReps.Add (1);

					ChoicesAppear ();  // Display the trees
				}
				break;

			case "choices_on":  // Mouse has poked his nose in to start, and the choices have appeared
				if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) { // licked at 1 of 2 lick ports
					int idx = rs / 2 - 1;

					// Log the decision
					FreeGlobals.firstTurn.Add (gos [idx].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [idx].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.firstTurnDeg.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);

					if (idx == this.treeToActivate) {  // Mouse chose the right tree, so give reward and log it!
						int dur = FreeGlobals.freeRewardDur [rs / 2]; // This is not quite right - won't work if trees get unequal reward
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
						FreeGlobals.trialDelay = 3;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
					} else {  // Mouse chose the non-matching tree, so withold reward and log it!
						FreeGlobals.sizeOfRewardGiven.Add (0);
						FreeGlobals.trialDelay = 1.5F;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
						Debug.Log ("incorrect");
					}
					FreeGlobals.WriteToLogFiles ();

					Invoke ("DisappearAllTrees", FreeGlobals.trialDelay);
					//SetupTreeActivation (gos, -1, gos.Length); // Hide all trees to reset the task
					FreeGlobals.freeState = "pretrial";
				}
				break;
			} 		
		} else if (FreeGlobals.gameType.Equals ("free_disc_single_ori")) {  
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag ("water");

			switch (FreeGlobals.freeState) {
			case "pretrial":  // Mouse has not yet poked his nose into the startport
				if (rs == FreeGlobals.freeRewardSite [0]) {  // Mouse poked nose into the startport
					// Setup side trees to turn on after the choice delay
					float rCand = UnityEngine.Random.value;
					float[] rThresh = new float[3];
					rThresh [0] = 0;
					rThresh [2] = 1;
					for (int i = 1; i < 2; i++) {
						rThresh [i] = 1F / 2 * i;
					}

					// Bias correction
					// This was ON when training batch#1, which learned within 3 days
					if (FreeGlobals.numberOfTrials >= 1) {
						float[] bcs = new float[2];
						string bcsStr = "";
						for (int i = 0; i < 2; i++) {
							bcs [i] = 1 - FreeGlobals.GetTurnBias (20, i);
							bcsStr += bcs [i] + " ";
						}
						float s = bcs.Sum ();
						Debug.Log (bcsStr);
						for (int i = 0; i < 2 - 1; i++) {
							bcs [i] = bcs [i] / s;  // Normalize all the bias corrections
							rThresh [i + 1] = rThresh [i] + bcs [i];
						}
					}

					this.treeToActivate = 0;  // treeToActivate is actually the side of the rewarded tree - both will be activated in this scenario
					for (int i = 1; i < 2 + 1; i++) {
						if (rCand >= rThresh [i - 1] && rCand <= rThresh [i])
							this.treeToActivate = i - 1;
					}

					int otherTree = this.treeToActivate == 0 ? 1 : 0;

					// Some debug output to confirm bias correction is working reasonably
					string threshStr = "";
					for (int i = 1; i < rThresh.Length - 1; i++) {
						threshStr += rThresh [i] + ", ";
					}
					Debug.Log ("[0, " + threshStr + "1] - " + rCand);

					for (int i = 0; i < 2; i++) {
						gos [i].GetComponent<WaterTreeScript> ().ChangeShader ("Custom/Curvy");
					}

					// This need not be set each trial, but whatever
					if (FreeGlobals.rewardedOri.Equals ("h")) {
						sampleHFreq = FreeGlobals.rewardedHFreq;
						sampleVFreq = FreeGlobals.rewardedVFreq;
						if (FreeGlobals.rewardedLineType.Equals ("curvy")) {
							gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, FreeGlobals.rewardedAmplitude, FreeGlobals.rewardedNumCycles, 1, 0, 0, 1);
							gos [otherTree].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, 0, 0, 1, 0, 0, 1);
						} else if (FreeGlobals.rewardedLineType.Equals ("straight")) {
							gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, 0, 0, 1, 0, 0, 1);
							gos [otherTree].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, FreeGlobals.rewardedAmplitude, FreeGlobals.rewardedNumCycles, 1, 0, 0, 1);
						}
					} else if (FreeGlobals.rewardedOri.Equals ("v")) {
						sampleHFreq = FreeGlobals.rewardedHFreq;
						sampleVFreq = FreeGlobals.rewardedVFreq;
						if (FreeGlobals.rewardedLineType.Equals ("curvy")) {
							gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, 0, 0, 1, FreeGlobals.rewardedAmplitude, FreeGlobals.rewardedNumCycles, 1);
							gos [otherTree].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, 0, 0, 1, 0, 0, 1);
						} else if (FreeGlobals.rewardedLineType.Equals ("straight")) {
							gos [this.treeToActivate].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, 0, 0, 1, 0, 0, 1);
							gos [otherTree].GetComponent<WaterTreeScript> ().SetShader (sampleHFreq, sampleVFreq, 0, 0, 1, FreeGlobals.rewardedAmplitude, FreeGlobals.rewardedNumCycles, 1);
						}
					}
						
					// Record the state to the log history kept in memory
					FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
					FreeGlobals.targetHFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.targetVFreq.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.targetDeg.Add (gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ());
					// TODO: Add log of curvy vs. straight reward

					FreeGlobals.numberOfTrials++;
					FreeGlobals.trialStartTime.Add (DateTime.Now.TimeOfDay);

					FreeGlobals.stimDur.Add (-1);
					FreeGlobals.stimReps.Add (1);

					ChoicesAppear ();  // Display the trees
				}
				break;

			case "choices_on":  // Mouse has poked his nose in to start, and the choices have appeared
				if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) { // licked at 1 of 2 lick ports
					int idx = rs / 2 - 1;

					// Log the decision
					FreeGlobals.firstTurn.Add (gos [idx].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [idx].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());
					FreeGlobals.firstTurnDeg.Add (gos [idx].GetComponent<WaterTreeScript> ().GetShaderRotation ());

					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);

					if (idx == this.treeToActivate) {  // Mouse chose the right tree, so give reward and log it!
						int dur = FreeGlobals.freeRewardDur [rs / 2]; // This is not quite right - won't work if trees get unequal reward
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
						FreeGlobals.trialDelay = 3;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
					} else {  // Mouse chose the non-matching tree, so withold reward and log it!
						FreeGlobals.sizeOfRewardGiven.Add (0);
						FreeGlobals.trialDelay = 1.5F;
						//this.fadeToBlack.gameObject.SetActive (true);
						//this.fadeToBlack.color = Color.white;
						this.state = "Paused";
						Debug.Log ("incorrect");
					}
					FreeGlobals.WriteToLogFiles ();

					Invoke ("DisappearAllTrees", FreeGlobals.trialDelay);
					FreeGlobals.freeState = "pretrial";
				}
				break;
			}
		}

		if (FreeGlobals.rewardAmountSoFar >= FreeGlobals.totalRewardSize)
			this.state = "GameOver";
		
		//if (this.udpSender.CheckReward ())
		//	this.movementRecorder.logReward(false, true);
        //this.movementRecorder.logReward(this.udpSender.CheckReward());
        //this.movementRecorder.logReward(true);
		this.numberOfTrialsText.text = "Current trial: # " + FreeGlobals.numberOfTrials.ToString ();
        this.rewardAmountText.text = "Reward amount so far: " + Math.Round(FreeGlobals.rewardAmountSoFar).ToString();
        //this.numberOfEarnedRewardsText.text = "Number of earned rewards: " + FreeGlobals.numberOfEarnedRewards.ToString();
        //this.numberOfUnearnedRewardsText.text = "Number of unearned rewards: " + FreeGlobals.numberOfUnearnedRewards.ToString();
		//this.numberOfDryTreesText.text = "Number of dry trees entered: " + FreeGlobals.numberOfDryTrees.ToString();

		if (FreeGlobals.numberOfTrials > 1) {
			this.numberOfCorrectTurnsText.text = "Correct turns: " +
			FreeGlobals.numCorrectTurns.ToString ()
			+ " (" +
				Mathf.Round(((float)FreeGlobals.numCorrectTurns / ((float)FreeGlobals.numberOfTrials)) * 100).ToString() + "%" 
				+ FreeGlobals.GetTreeAccuracy() + ")";
			this.lastAccuracyText.text = "Last 20 accuracy: " + Math.Round(FreeGlobals.GetLastAccuracy(20) * 100) + "%";
		}
		//this.frameCounter++;
		//Debug.Log ("screen updated");

        TimeSpan te = DateTime.Now.Subtract(FreeGlobals.gameStartTime);
        this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}", te.Hours * 60 + te.Minutes, te.Seconds);
        
		/*
		 * if (Time.time - this.runTime >= this.runDuration)
        {
            // fadetoblack + respawn
            this.movementRecorder.SetFileSet(false);
            this.fadeToBlack.gameObject.SetActive(true);
            this.state = "Fading";
        }
        */
    }

	private void DisappearTreeHelper() {
		if (firstStimOnTime == DateTime.MinValue)
			firstStimOnTime = DateTime.Now;

		// If any invokes to disappear the tree had been called before, reset them by canceling and calling again
		CancelInvoke ("DisappearImpersistentTree");

		disappearTreeDelay = FreeGlobals.persistenceDur / 1000;
		Invoke ("DisappearImpersistentTree", disappearTreeDelay);
	}
		
	private void DisappearImpersistentTree() { // Called by Invoke
		Debug.Log (firstStimOnTime.ToLongTimeString());
		Debug.Log (DateTime.Now.ToLongTimeString());
		Debug.Log (DateTime.Now.Subtract (firstStimOnTime).TotalMilliseconds);
		GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
		SetupTreeActivation(gos, -1, gos.Length);

		FreeGlobals.stimDur [FreeGlobals.stimDur.Count - 1] = 
			System.Convert.ToDouble(FreeGlobals.stimDur [FreeGlobals.stimDur.Count - 1]) + DateTime.Now.Subtract (firstStimOnTime).TotalMilliseconds;
		FreeGlobals.stimReps [FreeGlobals.stimReps.Count - 1] = 
			System.Convert.ToInt32(FreeGlobals.stimReps[FreeGlobals.stimReps.Count - 1]) + 1;
		firstStimOnTime = DateTime.MinValue;

		Debug.Log (FreeGlobals.stimDur [FreeGlobals.stimDur.Count - 1]);
	}

	private void DisappearAllTrees() { // Called by Invoke
		GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
		SetupTreeActivation(gos, -1, gos.Length);
	}

	private void ChoicesAppear() { // Called by Invoke
		GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
		for (int i = 0; i < 2; i++)
		{
			gos[i].SetActive(true);
			gos[i].GetComponent<WaterTreeScript>().Show();
		}
		FreeGlobals.freeState = "choices_on";  // Transition states, so that the side lickports are now active
	}

    public void OccludeTree(float treeLocX)
    {
		/*
        GameObject treeOccluder = GameObject.Find("TreeOccluder");
        Vector3 lp = treeOccluder.transform.localPosition;
        if (treeLocX > 20000)  // Target tree is on right side
            lp.x = -FreeGlobals.centralViewVisibleShift;
        else if (treeLocX < 20000)
            lp.x = FreeGlobals.centralViewVisibleShift;
        treeOccluder.transform.localPosition = lp;
        */
    }

    /*
     * Fade to Black
     * */
    private void Fade()
    {
        Color t = this.fadeToBlack.color;
        t.a += Time.deltaTime;
        this.fadeToBlack.color = t;

        if (this.fadeToBlack.color.a >= .95f)
        {
            this.state = "Reset";
        }
    }

	public void Pause()
	{
        this.rewardAmountText.text = "Reward amount so far: " + Math.Round(FreeGlobals.rewardAmountSoFar).ToString();
        //this.numberOfEarnedRewardsText.text = "Number of earned rewards: " + FreeGlobals.numberOfEarnedRewards.ToString();
        //this.numberOfUnearnedRewardsText.text = "Number of unearned rewards: " + FreeGlobals.numberOfUnearnedRewards.ToString();
        if (FreeGlobals.numberOfTrials > 1) {
            this.numberOfCorrectTurnsText.text = "Correct turns: " +
                FreeGlobals.numCorrectTurns.ToString()
                + " (" +
                Mathf.Round(((float)FreeGlobals.numCorrectTurns / ((float)FreeGlobals.numberOfTrials)) * 100).ToString() + "%" 
                + FreeGlobals.GetTreeAccuracy() + ")";
            this.lastAccuracyText.text = "Last 20 accuracy: " + Math.Round(FreeGlobals.GetLastAccuracy(20) * 100) + "%";
        }

        // NB Hack to get screen to go black before pausing for trialDelay
        if (waitedOneFrame) {
			//System.Threading.Thread.Sleep (FreeGlobals.trialDelay * 1000);
			waitedOneFrame = false;
			StartCoroutine(WaitFor(FreeGlobals.trialDelay));

            // Append to stats file here
            /* NB: removed as we want the mouse to run for a certain number of rewards, not trials?
			if (this.runNumber > this.numberOfRuns)
				this.state = "GameOver";
			else
				this.state = "Respawn";
				*/
        }
	}


	IEnumerator WaitFor(float n)
	{
		yield return new WaitForSeconds(n);
		waitedOneFrame = true;
		float totalEarnedRewardSize = 0;
		float totalRewardSize = 0;
		for (int i = 0; i < FreeGlobals.sizeOfRewardGiven.Count; i++) {
			totalEarnedRewardSize += (float)System.Convert.ToDouble(FreeGlobals.sizeOfRewardGiven[i]);
		}
		//			if (FreeGlobals.numberOfEarnedRewards + FreeGlobals.numberOfUnearnedRewards >= this.numberOfAllRewards)
		// End game if mouse has gotten more than 1 ml - and send me a message to retrieve the mouse?
		totalRewardSize = totalEarnedRewardSize + FreeGlobals.numberOfUnearnedRewards * FreeGlobals.rewardSize;
		Debug.Log("Total reward so far: " + totalRewardSize + "; maxReward = " + FreeGlobals.totalRewardSize);

		if (totalRewardSize >= FreeGlobals.totalRewardSize)
			this.state = "GameOver";
		else
			this.state = "Respawn";
	}



    /*
     * Reset all trees
     * */
	public void ResetScenario(Color c)
    {
        this.runTime = Time.time;
        this.runNumber++;

        //print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
        foreach (WaterTreeScript script in GameObject.Find("Trees").GetComponentsInChildren<WaterTreeScript>())
        {
            script.Refill();
		}
        //print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
        this.debugControlScript.enabled = false;

		// NB edit (1 line)
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = c;
		this.state = "Paused";

        // Move the player now, as the screen goes to black and the app detects collisions between the new tree and the player 
        // if the player is not moved.
        TeleportToBeginning();

        // check for game over
		/*
        if (this.runNumber > this.numberOfRuns)
            this.state = "GameOver";
        else
            this.state = "Respawn";
		*/
    }

    private void Respawn()
    {
        //Debug.Log ("Respawning");

        // NB edit - commented out to teleport mouse back to the beginning
        //int x = 20000 - Random.Range(-1 * this.respawnAmplitude, this.respawnAmplitude);
        //int z = 20000 - Random.Range(-1 * this.respawnAmplitude, this.respawnAmplitude);
        //float rot = Random.Range(0f, 360f);

        //this.player.transform.position = new Vector3(x, this.player.transform.position.y, z);
        //this.player.transform.Rotate(Vector3.up, rot);

        // NB edit - The code below comes from starting the game - ideally make a helper function?
 
		// Hack to clear the buffer
		for (int i = 0; i < 20; i++) {
			ard.CheckForMouseAction ();
		}
		Debug.Log ("done clearing buffer");
        this.runTime = Time.time;
		this.movementRecorder.SetRun(this.runNumber);
		this.movementRecorder.SetFileSet(true);
		Color t = this.fadeToBlack.color;
		t.a = 0f;
		this.fadeToBlack.color = t;
		this.fadeToBlackText.text = "";
		this.fadeToBlack.gameObject.SetActive(false);

		this.firstFrameRun = true;
		this.debugControlScript.enabled = true;

		FreeGlobals.hasNotTurned = true;

        FreeGlobals.trialStartTime.Add(DateTime.Now.TimeOfDay);
        this.state = "Running";
	}

    private void SetupTreeActivation(GameObject[] gos, int treeToActivate, int maxTrees)
    {
        for (int i = 0; i < maxTrees; i++)  // Even in the 3-tree case, never deactivate the 3rd tree
        {
            gos[i].SetActive(true);
            if (i == treeToActivate)
                gos[i].GetComponent<WaterTreeScript>().Show();
            else
                gos[i].GetComponent<WaterTreeScript>().Hide();
        }
    }

    public void GameOver()
    {
        //Debug.Log ("In GameOver()");
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;
        this.fadeToBlackText.text = "GAME OVER MUSCULUS!";

		ard.close ();
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            StartCoroutine(CheckForQ());
        }
    }

    private IEnumerator CheckForQ()
    {
        Debug.Log("Waiting for Q");
        yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Q));
        Debug.Log("quitting!");
        FreeGlobals.WriteStatsFile();
		//this.udpSender.close();
        Application.Quit();
    }

    private void MovePlayer()
    {
		
		if (FreeGlobals.newData) {
			FreeGlobals.newData = false;

			// Keep a buffer of the last 5 movement deltas to smoothen some movement
			if (this.last5Mouse2Y.Count == smoothingWindow)
				this.last5Mouse2Y.Dequeue ();
			if (this.last5Mouse1Y.Count == smoothingWindow)
				this.last5Mouse1Y.Dequeue ();

            if (FreeGlobals.gameTurnControl.Equals("roll"))
                this.last5Mouse1Y.Enqueue(-FreeGlobals.sphereInput.mouse1Y);
            else
                this.last5Mouse1Y.Enqueue (FreeGlobals.sphereInput.mouse1X);

			this.last5Mouse2Y.Enqueue (FreeGlobals.sphereInput.mouse2Y);
		
			// transform sphere data into unity movement
			//if (this.frameCounter - this.previousFrameCounter > 1)
			//print("lost packets: " + this.frameCounter + "/" + this.previousFrameCounter);
			this.previousFrameCounter = this.frameCounter;

			this.player.transform.Rotate(Vector3.up, Mathf.Rad2Deg * (this.last5Mouse1Y.Average()) / this.rawRotationDivider);
            
			Vector3 rel = this.player.transform.forward * (this.last5Mouse2Y.Average () / this.rawSpeedDivider);
			//this.player.transform.position = this.player.transform.position + rel;
			this.characterController.Move (rel);
			//this.udpSender.SendMousePos (this.player.transform.position);
			//this.udpSender.SendMouseRot (this.player.transform.rotation.eulerAngles.y);

			//Debug.Log (this.last5Mouse2Y.Average ());
			//Debug.Log (Time.time * 1000);

			// Send UDP msg out
			//this.udpSender.SendPlayerState(this.player.transform.position, this.player.transform.rotation.eulerAngles.y, FreeGlobals.playerInWaterTree, FreeGlobals.playerInDryTree);
		} else {
			//Debug.Log ("no new data");
		}
    }

    public void FlushWater()
    {
        //this.udpSender.FlushWater();
    }

}