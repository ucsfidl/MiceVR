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

public class GameControlScript : MonoBehaviour
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
	public Text mouseNameText;
	public Text scenarioNameText;
    public UDPSend udpSender;
    public MovementRecorder movementRecorder;

	private float rawSpeedDivider;  // Normally 60f; previously 200f
	private float rawRotationDivider;  // Normally 500f; previously -4000f
    private int respawnAmplitude = 2000;
    private int runNumber;
    private float runTime;
    private long frameCounter, previousFrameCounter;
    private System.Collections.Generic.Queue<float> last5Mouse2Y, last5Mouse1Y;
    public string state;
	public string prevState;
    private bool firstFrameRun;
    private bool playerInWaterTree, playerInDryTree;
    private Loader scenarioLoader;
    private CharacterController characterController;
    private DebugControl debugControlScript;
    private bool timeoutState;

	private int smoothingWindow = 6;  // Amount to smoothen the player movement.  1 worked fine with MX310 at UCSF, but G403 at Berkeley glitches sometimes, so try longer smoothing window to help.
	private bool waitedOneFrame = false;  // When mouse hits tree, need to wait a few frames before it turns black, and then pause the game

	private Vector3 startingPos;
	private Quaternion startingRot;
	private Vector3 prevPos;
	private Quaternion prevRot;

	private int centralViewVisible;

	private DateTime pauseStartTime;
	private int pauseStart = 0;
	private int pauseEnd = 0;
	private int pauseTime = 3; // frames - 1 is too few given noise in capture - This pauseTime is the number of frames in between trials, for camera frame analysis to find trial starts and ends!

	private float visiblePauseAtTrialStart = 2F;  // For first 2s of each trial, world won't move, so mouse can see stimulus - 1s seems too little time for my mice
	private DateTime lastTrialStartDateTime;

	private GameObject bb;

	// Use this for initialization
    void Start() {
		//Application.targetFrameRate = 60;
        this.frameCounter = this.previousFrameCounter = 0;
        this.runNumber = 1;
		this.last5Mouse1Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
		this.last5Mouse2Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
        this.state = "LoadScenario";
        this.firstFrameRun = false;
        this.scenarioLoader = GameObject.FindGameObjectWithTag("generator").GetComponent<Loader>();
        this.characterController = GameObject.Find("FPSController").GetComponent<CharacterController>();
        this.debugControlScript = GameObject.Find("FPSController").GetComponent<DebugControl>();
        this.characterController.enabled = false;  // Keeps me from moving the character while typing entries into the form
        Globals.numberOfEarnedRewards = 0;
        Globals.numberOfUnearnedRewards = 0;
        Globals.rewardAmountSoFar = 0;
		Globals.numberOfDryTrees = 0;
		Globals.numNonCorrectionTrials = 1;  // Start on first trial
        this.timeoutState = false;

		this.startingPos = this.player.transform.position;
		this.startingRot = this.player.transform.rotation;

		this.prevPos = this.startingPos;
		this.prevRot = this.startingRot;

		// Will this fix the issue where rarely colliding with a wall causes mouse to fly above the wall?  No.
		this.characterController.enableOverlapRecovery = false;  

		/*
		GameObject ifld = GameObject.Find ("ScenarioInput");
		EventSystemManager.currentSystem.SetSelectedGameObject (ifld);
		Debug.Log ("focused!");
		*/

        init();
		Globals.InitFOVs ();
    }

    // Update is called once per frame
    void Update() {
		if ((this.state == "Running" || this.state == "Paused") && Globals.numCameras > 0) {
			// To detect the occurence of a trial start or end, skip a frame trigger so that Matlab gets the signal direct through the camera,as the signal directly from unity is flaky
			if (pauseStart > 0) {
				pauseStart = pauseStart-1;
			} else if (pauseEnd > 0) {
				pauseEnd = pauseEnd-1;
			} else {
				Globals.currFrame = Globals.currFrame + 1;
				this.udpSender.SendFrameTrigger ();
			}
		}

		// Report location of the current target in screen coordinates
		/* Uncomment to show bounding box
		if (this.state == "Running" && (!this.prevPos.Equals(this.player.transform.position) || !this.prevRot.Equals(this.player.transform.rotation))) {  // Only update log when the position or rotation changes
			Vector3 targetLoc = Globals.targetLoc [Globals.targetLoc.Count - 1];
			GameObject targetGO = Globals.GetTreeGameObjectFromXPos (targetLoc.x);
			GameObject crown = targetGO.GetComponent<WaterTreeScript> ().crown;

			// Initialize bounding box if not done already, to help with debugging
			if (bb == null) {
				bb = GameObject.CreatePrimitive (PrimitiveType.Cube);
				bb.GetComponent<Collider> ().enabled = false;
			}
			
			// Draw a bounding box and see if this is accurate
			bb.transform.position = crown.transform.position;
			bb.transform.rotation = crown.transform.rotation;
			bb.transform.localScale = Globals.GetAbsoluteScale (crown.transform);
			bb.GetComponent<Renderer> ().material.color = Color.cyan;

			//Rect r = GUIRectWithObject (targetGO.GetComponent<WaterTreeScript>().crown);
			//Debug.Log (r.ToString ());
		}
		*/

		// Keep mouse from scaling walls
		if (this.player.transform.position.y > this.startingPos.y + 0.1) {
			Vector3 tempPos = this.player.transform.position;
			tempPos.y = this.startingPos.y;
			tempPos.x = this.prevPos.x;
			tempPos.z = this.prevPos.z;
			this.player.transform.position = tempPos;
		}
		this.prevPos = this.player.transform.position;
		//Debug.Log ("PrevPos = " + prevPos.ToString());
		this.prevRot = this.player.transform.rotation;
		//Debug.Log ("PrevRot = " + prevRot.ToString());

		// Reveal targets if the mouse reaches the correct ZPos and the target had been hidden
		if (this.state == "Running" && !Globals.hiddenShown && Globals.GetCurrentWorld().revealZPos.Count > 0 && this.player.transform.position.z >= Globals.GetCurrentWorld ().revealZPos [0]) {  // only pay attention to the first value in the list - expand if support added for target-dependent reveal zpos'
			GameObject[] gos = Globals.GetTrees ();
			int targetIdx = Globals.targetIdx [Globals.targetIdx.Count - 1];
			if (targetIdx != Globals.CATCH_IDX) {
				gos [targetIdx].GetComponent<WaterTreeScript> ().Show ();
			}
			Globals.hiddenShown = true;
		}

        switch (this.state) {
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

			case "Blacken":
				Blacken();
				break;

			case "Unblacken":
				Unblacken ();
				break;

            case "GameOver":
                GameOver();
                break;

            default:
                break;
        }
    }

	void LateUpdate() {
		CatchKeyStrokes ();
	}

    public void init() {
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
		string _numMice = "";
		string _monitorPositiveElevation = "";
		string _monitorNegativeElevation = "";
		string _monitorAzimuth = "";
		string _fovNasalAzimuth = "";
		string _fovTemporalAzimuth = "";
		string _occluderXScale = "";
		string _occluderYScale = "";
		string _vrGoogleSheetsName = "";
		string _numCameras = "";

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
			_numMice = xn["numMice"].InnerText;
			_monitorPositiveElevation = xn ["monitorPositiveElevation"].InnerText;
			_monitorNegativeElevation = xn ["monitorNegativeElevation"].InnerText;
			_monitorAzimuth = xn ["monitorAzimuth"].InnerText;
			_fovNasalAzimuth = xn ["fovNasalAzimuth"].InnerText;
			_fovTemporalAzimuth = xn ["fovTemporalAzimuth"].InnerText;
			_occluderXScale = xn ["occluderXScale"].InnerText;
			_occluderYScale = xn ["occluderYScale"].InnerText;
			_vrGoogleSheetsName = xn ["vrGoogleSheetsName"].InnerText;
			_numCameras = xn ["numCameras"].InnerText;
        }

        int.TryParse(_runDuration, out this.runDuration);
        int.TryParse(_numberOfRuns, out this.numberOfRuns);
		int.TryParse(_numberOfAllRewards, out this.numberOfAllRewards);
		float.TryParse(_rawSpeedDivider, out this.rawSpeedDivider);
		float.TryParse(_rawRotationDivider, out this.rawRotationDivider);
		int.TryParse (_centralViewVisible, out this.centralViewVisible);
        int.TryParse(_rewardDur, out Globals.rewardDur);
        float.TryParse(_rewardSize, out Globals.rewardSize);
		int.TryParse(_numMice, out Globals.numMice);
		float.TryParse(_monitorPositiveElevation, out Globals.monitorPositiveElevation);
		float.TryParse(_monitorNegativeElevation, out Globals.monitorNegativeElevation);
		float.TryParse(_monitorAzimuth, out Globals.monitorAzimuth);
		float.TryParse(_fovNasalAzimuth, out Globals.fovNasalAzimuth);
		float.TryParse(_fovTemporalAzimuth, out Globals.fovTemporalAzimuth);
		float.TryParse(_occluderXScale, out Globals.occluderXScale);
		float.TryParse(_occluderYScale, out Globals.occluderYScale);
		Globals.vrGoogleSheetsName = _vrGoogleSheetsName;
		int.TryParse(_numCameras, out Globals.numCameras);

		//Globals.SetCentrallyVisible(centralViewVisible);

		//Debug.Log ("Init view value: " + _centralViewVisible);
		//Debug.Log ("Central view shift: " + Globals.centralViewVisibleShift);
        // trying to avoid first drops of water
        this.udpSender.ForceStopSolenoid();
        this.udpSender.setAmount(Globals.rewardDur);
        this.udpSender.CheckReward();
    }

    private void CatchKeyStrokes() {
		if (Input.GetKeyUp(KeyCode.Escape) && !this.state.Equals ("WaitingForQuitCmd")) {
			this.state = "GameOver";
			// Disable opto lights, if on, right here instead of in GameOver() so it doesn't repeatedly send the signal and block the aruino communication, preventing other signals (like manual water) from getting through
			udpSender.GetComponent<UDPSend>().OptoTurnOffAll();
		}
        
        if (!this.state.Equals("LoadScenario") || (this.state.Equals("LoadScenario") && EventSystem.current.currentSelectedGameObject == null)) {
			if (Input.GetKeyUp (KeyCode.U)) {
				this.udpSender.SendWaterReward (Globals.rewardDur);
				Globals.numberOfUnearnedRewards++;
				Globals.rewardAmountSoFar += Globals.rewardSize;
				updateRewardAmountText ();
				Debug.Log ("gave reward = " + Globals.rewardAmountSoFar);
			} else if (Input.GetKeyUp (KeyCode.T)) {
				TeleportToBeginning ();
			} else if (Input.GetKeyUp (KeyCode.B)) {
				if (!this.state.Equals ("Blacken")) {
					this.prevState = this.state;
					this.state = "Blacken";
				} else {
					this.state = "Unblacken";
				}
			} else if (Input.GetKeyUp (KeyCode.O)) {  // toggle opto state just for this trial, if this is screwing with the mouse behavior
				if (Globals.currOptoState != Globals.optoOff) {
					udpSender.GetComponent<UDPSend> ().OptoTurnOffAll ();
					Globals.currOptoState = Globals.optoOff;
				} else {
					if (Globals.optoSide != Globals.optoLorR) { // OptoTurnOn does not support LorR
						udpSender.GetComponent<UDPSend> ().OptoTurnOn (Globals.optoSide);  
						Globals.currOptoState = Globals.optoSide;
					}
				}
			}
        }
    }

    private void TeleportToBeginning() {
        this.player.transform.position = this.startingPos;
        this.player.transform.rotation = this.startingRot;
    }

    /*
     * Waits until a tree config is loaded
     * */
    private void LoadScenario() {
        if (this.scenarioLoader.scenarioLoaded == true) {
            this.menuPanel.SetActive(false);
            this.state = "StartGame";
        }
    }

    /*
     * Waits for user input to start the game
     * */
    private void StartGame() {
        //Debug.Log ("In StartGame()");
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;
        this.fadeToBlackText.text = "Press SPACE to start";
		//Debug.Log ("waiting for space bar");
		if (Input.GetKeyUp (KeyCode.Space)) {
			//Debug.Log ("Running");
			this.runTime = Time.time;
			Globals.gameStartTime = DateTime.Now;
			//Debug.Log("Game started at " + Globals.gameStartTime.ToLongTimeString());
			this.movementRecorder.SetRun (this.runNumber);
			this.movementRecorder.SetFileSet (true);
			Color t = this.fadeToBlack.color;
			t.a = 0f;
			this.fadeToBlack.color = t;
			this.fadeToBlackText.text = "";
			this.fadeToBlack.gameObject.SetActive (false);

			this.firstFrameRun = true;
			this.debugControlScript.enabled = true;
			Globals.hasNotTurned = true;
			Globals.numCorrectTurns = 0;
			this.characterController.enabled = true;  // Bring back character movement

			this.mouseNameText.text = "Name: " + Globals.mouseName;
			//this.scenarioNameText.text = "Scenario: " + Globals.scenarioName + " (Day " + Globals.trainingDayNumber + ", session #" + Globals.scenarioSessionNumber + ", setting " + Globals.inputDeg + ")";
			this.scenarioNameText.text = "Scenario: " + Globals.scenarioName + " (Day " + Globals.trainingDayNumber + ", session #" + Globals.scenarioSessionNumber + ", fov " + Globals.visibleNasalBoundary + ", "
			+ Globals.visibleTemporalBoundary + ", " + Globals.visibleHighBoundary + ", " + Globals.visibleLowBoundary + ")";

			Globals.InitLogFiles ();
			Globals.trialStartTime.Add (DateTime.Now);
			lastTrialStartDateTime = DateTime.Now;
			Respawn ();
			this.state = "Running";
		}
	}

    /*
     * dry trees timeout state
     * */
    private void Timeout() {
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;

        if (!Globals.timeoutState)
        {
            StartCoroutine(Wait());
            Globals.timeoutState = true;
        }
    }

    IEnumerator Wait() {
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
    private void Run() {
		// send SYNC msg on first frame of every run.
        if (this.firstFrameRun) {
            this.udpSender.SendRunSync();
            this.firstFrameRun = false;
        }

        if (Globals.playerInDryTree && !Globals.timeoutState) {
            this.state = "Timeout";
        }

		TimeSpan te = DateTime.Now.Subtract(lastTrialStartDateTime);
		if (te.TotalMilliseconds >= visiblePauseAtTrialStart * 1000) {
			MovePlayer();
		}

		if (this.udpSender.CheckReward ())
			this.movementRecorder.logReward(false, true);
		updateTrialsText();
		updateRewardAmountText ();

        te = DateTime.Now.Subtract(Globals.gameStartTime);
		TimeSpan te2 = DateTime.Now.Subtract (Globals.trialStartTime[Globals.trialStartTime.Count - 1]);
		this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}:{2:00.00}:{3}", te.Hours * 60 + te.Minutes, te.Seconds, Time.deltaTime * 1000, frameCounter++) + " - " + 
			string.Format("{0:D2}:{1:D2}", te2.Hours*60 + te2.Minutes, te2.Seconds);
		
        if (Time.time - this.runTime >= this.runDuration) {
            // fadetoblack + respawn
            this.movementRecorder.SetFileSet(false);
            this.fadeToBlack.gameObject.SetActive(true);
            this.state = "Fading";
        }
	}

    public void OccludeTree(float treeLocX) {
        GameObject treeOccluder = GameObject.Find("TreeOccluder");
        Vector3 lp = treeOccluder.transform.localPosition;
		if (treeLocX > Globals.worldXCenter)  // Target tree is on right side
            lp.x = -Globals.centralViewVisibleShift;
		else if (treeLocX < Globals.worldXCenter)
            lp.x = Globals.centralViewVisibleShift;
        treeOccluder.transform.localPosition = lp;
		Debug.Log ("Tree at " + treeLocX);
    }

    /*
     * Fade to Black
     * */
    private void Fade() {
        Color t = this.fadeToBlack.color;
        t.a += Time.deltaTime;
        this.fadeToBlack.color = t;

        if (this.fadeToBlack.color.a >= .95f)
        {
            this.state = "Reset";
        }
    }

	// Called when the user presses F to make the screen black - ball movements no longer move the mouse through the world.  Convenience to try to see if one can snap the mouse out of a period of inattention.
	// Screen stays black until the user presses F again to unfreeze.
	public void Blacken() {
		TimeSpan te = DateTime.Now.Subtract(Globals.gameStartTime);
		this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}:{2:G4}:{3}", te.Hours * 60 + te.Minutes, te.Seconds, Time.deltaTime * 1000, frameCounter++);

		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = Color.black;
	}

	public void Unblacken() {
		Color t = this.fadeToBlack.color;
		t.a = 0f;
		this.fadeToBlack.color = t;
		this.fadeToBlackText.text = "";
		this.fadeToBlack.gameObject.SetActive(false);

		this.state = this.prevState;
	}

	// Game is paused briefly between trials
	public void Pause() {
		updateRewardAmountText ();
        if (Globals.numNonCorrectionTrials > 1) {
			updateCorrectTurnsText ();
			this.lastAccuracyText.text = "Last 20 accuracy (in this world): " + Math.Round(Globals.GetLastAccuracy(20) * 100) + "%";
        }
			
		// Update the time display
		TimeSpan te = DateTime.Now.Subtract(Globals.gameStartTime);
		this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}:{2:G4}:{3}", te.Hours * 60 + te.Minutes, te.Seconds, Time.deltaTime * 1000, frameCounter++);

		// Only proceed if elapsed time is greater than or equal to trialDelay
		te = DateTime.Now.Subtract(pauseStartTime);
		if (te.TotalMilliseconds >= Globals.trialDelay * 1000) {
			//Debug.Log ("Ending Pause()");
			float totalEarnedRewardSize = 0;
			float totalRewardSize = 0;
			for (int i = 0; i < Globals.sizeOfRewardGiven.Count; i++) {
				totalEarnedRewardSize += (float)System.Convert.ToDouble(Globals.sizeOfRewardGiven[i]);
			}
			totalRewardSize = totalEarnedRewardSize + Globals.numberOfUnearnedRewards * Globals.rewardSize;
			pauseEnd = pauseTime;
			if (totalRewardSize >= Globals.totalRewardSize) {
				this.state = "GameOver";
				// Turn off opto lights here instead of in GameOver() so Unity doesn't repeatedly send the signal and block the arduino communication, preventing other signals (like manual water) from getting through
				udpSender.GetComponent<UDPSend>().OptoTurnOffAll();
			} else {
				this.state = "Respawn";
			}
		}
	}

    /*
     * Reset all trees
     * */
	public void ResetScenario(Color c) {
        this.runTime = Time.time;
        this.runNumber++;

        foreach (WaterTreeScript script in GameObject.Find("Trees").GetComponentsInChildren<WaterTreeScript>()) {
            script.Refill();
		}
        this.debugControlScript.enabled = false;

		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = c;
		this.state = "Paused";

		this.pauseStartTime = DateTime.Now;
		this.pauseStart = pauseTime;

        // Move the player now, as the screen goes to black and the app detects collisions between the new tree and the player 
        // if the player is not moved.
        TeleportToBeginning();
		//Debug.Log ("Moved player to beginning");
    }

    private void Respawn() {
		//Debug.Log ("Respawn");
		// Is this teleportation needed? I think not.  Remove at some point.
        TeleportToBeginning();

		// Declare the important variables needed later for logging
		int idx;
		Vector3 loc;
		float hfreq;
		float vfreq;
		float angle;
		float distractorAngle;  // we will use 360 as the null value
		int worldIdx;
		int optoState = Globals.optoOff;
		int blockSize = Globals.blockSize;  // Since this can be different for each world, keep a local variable here to store this difference, if there is one

		// Do this tally early, before optostate might change, which will affect this in the current implementation where the optostate history is not tracked in memory
		if (Globals.CurrentlyCorrectionTrial()) {
			Globals.correctionTrialMarks.Add (1);
		} else {
			Globals.correctionTrialMarks.Add (0);
		}

		// If the last trial was an error and correction trials are enabled in the scenario, just do a redo, unless it was a catch trial!
		if (Globals.CurrentlyCorrectionTrial ()) {
			Debug.Log ("in correction trial");
			idx = Globals.targetIdx [Globals.targetIdx.Count - 1];
			loc = Globals.targetLoc [Globals.targetLoc.Count - 1];
			hfreq = Globals.targetHFreq [Globals.targetHFreq.Count - 1];
			vfreq = Globals.targetVFreq [Globals.targetVFreq.Count - 1];
			angle = Globals.targetAngle [Globals.targetAngle.Count - 1];
			worldIdx = Globals.worldIdxList [Globals.worldIdxList.Count - 1];
			distractorAngle = Globals.distractorAngle [Globals.distractorAngle.Count - 1];
			optoState = Globals.optoStates [Globals.optoStates.Count - 1];
			Globals.worldIdxList.Add(worldIdx);  		// Record which world this trial is on - must happen before below
			// If trees are hidden at the start, rehide the tree
			if (Globals.GetCurrentWorld ().hiddenIdx.Contains (idx)) {
				GameObject[] gos = Globals.GetTrees ();
				gos [idx].GetComponent<WaterTreeScript> ().Hide ();
			}
		} else { 		// So if this is not a correction trial, then re-render everything per usual
			Globals.ClearWorld (); // Wipes out all targets and walls, only to be rendered again

			int newWorldIdx;  // Need to initialize for the compiler
			if (Globals.alternateWorlds) {
				if (Globals.worldIdxList.Count < 1) {
					newWorldIdx = 0;
				} else {
					newWorldIdx = ((int)Globals.worldIdxList [Globals.worldIdxList.Count - 1] + 1) % Globals.worlds.Count;
				}
			} else {
				if (Globals.worldBlockSize > 0) {  // Using precomputed blocks for picking the world on a trial
					if (Globals.numNonCorrectionTrials % Globals.worldBlockSize == 1) {
						// Time to precompute a new block
						int numWorlds = Globals.worlds.Count;
						int[] precompWorldBlock = new int[Globals.worldBlockSize];
						// Fill a bag with worldIdxs in proportion to presentation rates, then pull from the bag randomly to fill the world queue
						List<int> bagOfWorldIdxs = new List<int> ();
						int[] worldMaxFreq = new int[numWorlds];  // Max number of each world per block
						decimal[] worldProb = new decimal[numWorlds];

						// First, setup all the arrays
						for (int wIdx = 0; wIdx < numWorlds; wIdx++) {
							worldProb [wIdx] = (decimal)Globals.worlds [wIdx].presoFrac;
							worldMaxFreq [wIdx] = (int)Math.Ceiling (Globals.worldBlockSize * worldProb [wIdx]);
						}
						// Second, fill the bag of worlds
						for (int i = 0; i < worldMaxFreq.Length; i++) {
							for (int j = 0; j < worldMaxFreq [i]; j++) {
								bagOfWorldIdxs.Add (i);
							}
						}

						while (true) {
							List<int> bagOfWorldIdxsCopy = new List<int> (bagOfWorldIdxs);
							int ran, currWorld;
							for (int i = 0; i < Globals.worldBlockSize; i++) {
								ran = UnityEngine.Random.Range (0, bagOfWorldIdxsCopy.Count ());
								currWorld = bagOfWorldIdxsCopy [ran];
								precompWorldBlock [i] = currWorld;
								bagOfWorldIdxsCopy.RemoveAt (ran);
							}

							// Guarantee that probe worlds, which do not get correction trials, occur in each block
							bool allProbesFound = false;
							if (Globals.probeWorldIdx.Count == 0) {
								allProbesFound = true;
							} else {
								foreach (int pIdx in Globals.probeWorldIdx) {
									if (precompWorldBlock.Contains (pIdx)) {
										allProbesFound = true;
									} else {
										allProbesFound = false;
										break;
									}
								}
							}

							// Ensure that first trial in each block is not a probe, and that there are never 2 identical probes in a row, for when lesion or light always on - if light is sometimes OFF, repeats are OK
							bool regenBlock = false;
							bool testForRepeatProbes = false;  // Flag used to see if repeats should be avoided. We don't avoid repeats if 50% of the trials include probes, only if less than 50% do
							foreach (int pIdx in Globals.probeWorldIdx) {
								if (Globals.worlds[pIdx].presoFrac < 0.5) {
									testForRepeatProbes = true;
								}
							}
							if (testForRepeatProbes) {
								if (Globals.optoSide == Globals.optoOff || (Globals.optoSide != Globals.optoOff && Globals.optoTrialsPerBlock == blockSize)) {
									if (Globals.probeWorldIdx.Contains (precompWorldBlock [0])) {  // If first trial in block is a probe, regen block
										regenBlock = true;
									} else {
										int lastId = precompWorldBlock [0];
										foreach (int id in precompWorldBlock.Skip(1)) {
											if ((Globals.probeWorldIdx.Contains (id) && id == lastId)) {
												regenBlock = true;
												break;
											}
											lastId = id;
										}
									}
								}
							}

							if (allProbesFound && !regenBlock) {
								break;
							} else {
								Debug.Log (String.Join (",", precompWorldBlock.Select (x => x.ToString ()).ToArray ()));
								Debug.Log ("Violated probe worlds placement policies in world block, so try to generate a new worldBlock");	
							}
						}
						Debug.Log (String.Join (".", bagOfWorldIdxs.Select (x => x.ToString ()).ToArray ()));
						Debug.Log (String.Join (".", precompWorldBlock.Select (x => x.ToString ()).ToArray ()));

						Globals.precompWorldBlock = precompWorldBlock;
						newWorldIdx = precompWorldBlock [0];

					} else {
						newWorldIdx = Globals.GetWorldToActivateFromBlock ();
					}
				} else {  // Not using blocks, so just give a random world
					newWorldIdx = Globals.RANDOM_WORLD;
				}
			}
			worldIdx = Globals.RenderWorld (newWorldIdx);
			Globals.worldIdxList.Add(worldIdx);  			// Record which world this trial is on - MUST HAPPEN BEFORE BELOW

			// Now that we know what world we will be rendering, check to see if there is or will be a precomputed blockSize for this world.  Since worldPresoFracs can vary, the blockSize will vary per world
			if (Globals.GetWorldPresoFrac(worldIdx) != 0) { // initial value is 0 
				float presoFrac = Globals.GetWorldPresoFrac(worldIdx);
				blockSize = (int)Math.Round (presoFrac * Globals.worldBlockSize);
			}
			//Debug.Log (blockSize);

			GameObject[] gos = Globals.GetTrees ();

			// Just initial values to catch trial values, to slightly simplify code below
			idx = Globals.CATCH_IDX;
			loc = new Vector3(Globals.CATCH_IDX, Globals.CATCH_IDX, Globals.CATCH_IDX);
			hfreq = Globals.CATCH_IDX;
			vfreq = Globals.CATCH_IDX;
			angle = Globals.CATCH_IDX;
			distractorAngle = 360;  // we will use 360 as the null value

			int treeToActivate = 0;
			float r = UnityEngine.Random.value;

			// PRECOMPUTE TRIAL ORDER if BLOCKS ENABLED
			// Support pre-computing blocks of trials, at the beginning and after each block
			// Added support for multi-world blocks
			// Added support for randomly ordered blocks of worlds
			// Only works with bias-correction disabled
			if (blockSize > 0 && !Globals.biasCorrection && 
				(((Globals.worlds.Count == 1 || Globals.alternateWorlds) && Math.Ceiling((double)Globals.numNonCorrectionTrials / Globals.worlds.Count) % blockSize == 1) ||  
					(Globals.precompWorldBlock != null && Array.IndexOf(Globals.precompWorldBlock, worldIdx) == ((Globals.numNonCorrectionTrials-1) % Globals.worldBlockSize)))) {
				int numTrees = gos.Count ();
				int[] precompTrialBlock = new int[blockSize];

				// Algorithm is to generate a list of stimulus locations in proportion to the appearance probabilities, 
				// and then remove from that List until the block is filled.
				List<int> stimLocs = new List<int> ();
				int[] maxFreq = new int[numTrees];  // Max number of each stimulus per block
				int maxCatch = (int)Math.Round (blockSize * Globals.catchFreq);  // For some reason Ceiling rounds up 2 to 3...
				// If catchFreq is non-zero but maxCatch is 0, force it to be 1 (for 2L_2R_R_10 and 3L_3R_R_10)
				if (Globals.catchFreq != 0 && maxCatch == 0) {
					maxCatch = 1;
				}
				decimal[] prob = new decimal[numTrees];

				// First, setup all the arrays
				for (int i = 0; i < numTrees; i++) {
					if (Globals.presoFracSpecified) {
						prob [i] = (decimal)gos [i].GetComponent<WaterTreeScript> ().GetPresoFrac () - (decimal)(Globals.catchFreq / numTrees);
					} else {
						prob [i] = (decimal)(1F / numTrees) - (decimal)(Globals.catchFreq / numTrees);
					}
					maxFreq [i] = (int)Math.Ceiling (blockSize * prob [i]);
				}

				// Second, fill the bag of stim locs
				for (int i = 0; i < maxFreq.Length; i++) {
					for (int j = 0; j < maxFreq [i]; j++) {
						stimLocs.Add (i);
					}
				}
				for (int i = 0; i < maxCatch; i++) {
					stimLocs.Add (Globals.CATCH_IDX);
				}
					
				// Guarantee that there is a probe trial in each block, regardless of probe probability
				//Debug.Log("Guarantee probe in each block");
				while (true) {
					List<int> stimLocsCopy = new List<int> (stimLocs);
					int ran, currStim;
					for (int i = 0; i < blockSize; i++) {
						ran = UnityEngine.Random.Range (0, stimLocsCopy.Count ());
						currStim = stimLocsCopy [ran];
						precompTrialBlock [i] = stimLocsCopy [ran];
						stimLocsCopy.RemoveAt (ran);
					}

					bool allProbesFound = false;
					if (Globals.GetCurrentWorld ().probeIdx.Count == 0) {
						allProbesFound = true;
					} else {
						foreach (int pid in Globals.GetCurrentWorld().probeIdx) {
							if (precompTrialBlock.Contains (pid)) {
								allProbesFound = true;
							} else {
								allProbesFound = false;
								break;
							}
						}
					}

					// Ensure that first trial in each block is not a probe, and that there are never 2 identical probes in a row, for when lesion or light always on
					// Do this only if if less than 50% of targets are probes, as in some worlds (one-sided 2AFC, 2-choice world) with only 2 choices it is impossible to implement the noRepeats policy
					bool noRepeatProbes = true;
					bool testForRepeatProbes = false;  // Flag used to see if repeats should be avoided. We don't avoid repeats if 50% of the trials include probes, only if less than 50% do
					bool catchPlacementOK = true;
					foreach (int probeIdx in Globals.GetCurrentWorld().probeIdx) {
						if (Globals.presoFracSpecified) {
							if (gos [probeIdx].GetComponent<WaterTreeScript> ().GetPresoFrac () < 0.5) {
								testForRepeatProbes = true;
							}
						}
					}
					if (!testForRepeatProbes) {
						if ((float)Globals.GetCurrentWorld ().probeIdx.Count / Globals.GetCurrentWorld ().trees.Count < 0.5) {
							testForRepeatProbes = true;
						}
					}

					if (testForRepeatProbes) {
						if (Globals.optoSide == Globals.optoOff || (Globals.optoSide != Globals.optoOff && Globals.optoTrialsPerBlock == blockSize)) {
							if (Globals.GetCurrentWorld ().probeIdx.Contains (precompTrialBlock [0])) {
								noRepeatProbes = false;
							} else {
								int lastId = precompTrialBlock [0];
								foreach (int id in precompTrialBlock.Skip(1)) {
									if ((Globals.GetCurrentWorld ().probeIdx.Contains (id) && id == lastId)) {
										noRepeatProbes = false;
										break;
									}
									lastId = id;
								}
							}
						}
					}

					// Ensure that the first trial is not a catch trial, that there are never 2 catch trials in a row, and that there is at least 1 catch trial per block.
					if (precompTrialBlock [0] == Globals.CATCH_IDX || (Globals.catchFreq > 0 && !precompTrialBlock.Contains(Globals.CATCH_IDX))) {
						catchPlacementOK = false;
					} else {
						int lastId = precompTrialBlock [0];
						foreach (int id in precompTrialBlock.Skip(1)) {
							if (id == Globals.CATCH_IDX && id == lastId) {
								catchPlacementOK = false;
								break;
							}
							lastId = id;
						}
					}
												
					if (allProbesFound && noRepeatProbes && catchPlacementOK) {
						break;
					} else {
						Debug.Log (String.Join (",", precompTrialBlock.Select (x => x.ToString ()).ToArray ()));
						Debug.Log ("Violated probe placement policies, try again");	
					}
				}
				Debug.Log (String.Join (",", stimLocs.Select (x => x.ToString ()).ToArray ()));
				Debug.Log (String.Join (",", precompTrialBlock.Select (x => x.ToString ()).ToArray ()));

				Globals.SetCurrentWorldPrecompTrialBlock(precompTrialBlock);

				// Next, if extinctionFreq > 0, precomp Extinction Trial Block mask
				// First, initialize to 0
				int[] precompExtinctBlock = new int[blockSize];
				for (int i = 0; i < blockSize; i++) {
					precompExtinctBlock [i] = 0;
				}
				// Next, update values depending on extinctFreq and which trial type, as no extinction trials when only center target is present
				if (Globals.extinctFreq > 0) {
					int maxExtinct = (int)Math.Round (blockSize * Globals.extinctFreq);  // For some reason Ceiling rounds up 2 to 3...
					if (maxExtinct == 0) {
						maxExtinct = 1;  // Do this so that on 3L_3R levels, will get some extinction trials on probe side
					}
					// For left (idx=0) and right (idx=1) trials, randomly pick the trials to get maxExtinct done
					for (int locIdx = 0; locIdx < 2; locIdx++) {
						int[] arrIdx = precompTrialBlock.Select((b,i) => b == locIdx ? i : -1).Where(i => i != -1).ToArray();
						List<int> prevExtinctIdx = new List<int> ();
						for (int i = 0; i < maxExtinct; i++) {
							while(true) { // Keep randomly picking indexes for the extinction trial until one is found that hasn't been used before
								int ranIdx = UnityEngine.Random.Range (0, arrIdx.Length);  // A starting index of 0 allows the first trial of a type to be an extinction trial.  If this seems wrong later, change here
								if (!prevExtinctIdx.Contains (ranIdx)) {
									precompExtinctBlock [arrIdx[ranIdx]] = 1;  // Mark this trial as being an extinction trial
									prevExtinctIdx.Add (ranIdx);
									break;
								}
							}
						}

					}
				}
				Debug.Log (String.Join (",", precompExtinctBlock.Select (x => x.ToString ()).ToArray ()));
				Globals.SetCurrentWorldPrecompExtinctBlock(precompExtinctBlock);

				// Next, if optoAlternation is turned off and this is an opto game, precompute the opto state for each trial
				if (Globals.optoSide != Globals.optoOff && !Globals.optoAlternation) { // an optoSide was specified and optoAlternation is turned off
					int[] precompOptoBlock = new int[blockSize];

					// First, count the number of trials at each stimloc
					int[] numTrialsPerStimLoc = new int[numTrees];
					int numCatchTrials = 0;
					for (int i = 0; i < blockSize; i++) {
						if (precompTrialBlock [i] != Globals.CATCH_IDX) {  // if not a catch trial
							numTrialsPerStimLoc [precompTrialBlock [i]] = numTrialsPerStimLoc [precompTrialBlock [i]] + 1;
						} else {
							precompOptoBlock[i] = Globals.optoSide;  // not used for anything yet
						}

					}
					int minVal = numTrialsPerStimLoc.Min ();
					int infrequentStimLoc = Array.IndexOf (numTrialsPerStimLoc, minVal);

					// Second, for each stimLoc, set its optoState ON based on the 1/2 of the frequency of the most infrequent stim location
					// Or, if optoTrialsPerBlock set, use that value.
					int numOptoOn = minVal / 2;
					if (Globals.optoTrialsPerBlock != -1) {
						numOptoOn = Globals.optoTrialsPerBlock;
						if (Globals.optoSide == Globals.optoLorR)
							numOptoOn *= 2;
					}
					for (int i = 0; i < numTrees; i++) {
						List<int> optoStates = new List<int> ();
						int flag = 0;
						for (int j = 0; j < numOptoOn; j++) {
							if (Globals.optoSide == Globals.optoLorR) {
								if (flag == 0) {
									optoStates.Add (Globals.optoL);
									flag = 1;
								} else {
									optoStates.Add (Globals.optoR);
									flag = 0;
								}
							} else {
								optoStates.Add (Globals.optoSide);
							}
						}
						for (int j = 0; j < numTrialsPerStimLoc [i] - numOptoOn; j++) {
							optoStates.Add (Globals.optoOff);
						}
						//Debug.Log (String.Join(",", optoStates.Select(x=>x.ToString()).ToArray()));
						// While optoStates left, assign them randomly
						int lastIdx = 0;
						while (true) {
							int currIdx = Array.IndexOf (precompTrialBlock, i, lastIdx);
							if (currIdx == -1) {
								break;
							}
							int ran = UnityEngine.Random.Range (0, optoStates.Count ());
							//Debug.Log (currIdx);
							precompOptoBlock [currIdx] = optoStates [ran];
							optoStates.RemoveAt (ran);
							lastIdx = currIdx + 1;
						}
					}
					Debug.Log (String.Join (",", precompOptoBlock.Select (x => x.ToString ()).ToArray ()));
					Globals.SetCurrentWorldPrecompOptoBlock(precompOptoBlock);
				}
			}
			// END PRECOMPUTE TRIAL ORDERS FOR BLOCKS

			string gameType = Globals.GetGameType (worldIdx);
			//Debug.Log (gameType);
			//Debug.Log ("World #: " + worldIdx);

			if (gameType.Equals ("detection") || gameType.Equals ("det_target") || gameType.Equals ("disc_target")) {
				if (gos.Length == 1) { // Linear track
					loc = gos [0].transform.position;
					if (Globals.varyOrientation) {
						if (r > 0.5) { // Half the time, swap the orientation of the tree
							gos [0].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
							hfreq = gos [0].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
							vfreq = gos [0].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
							angle = gos [0].GetComponent<WaterTreeScript> ().GetShaderRotation ();
						}
					} else {
						hfreq = gos [0].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = gos [0].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						angle = gos [0].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					}
				} else if (gos.Length == 2 || gameType.Equals ("det_target") || gameType.Equals ("disc_target")) {
					float catchThresh = 1 - Globals.catchFreq;
					float rThresh0 = 0.5F;
					if (Globals.presoRatio > 0) { // Works for 2-choice only, YN and 2AFC
						rThresh0 = (float)Globals.presoRatio / (Globals.presoRatio + 1);
					}
					if (Globals.biasCorrection && Globals.numNonCorrectionTrials > 1) {
						rThresh0 = 1 - Globals.GetTurnBias (20, 0);  // varies the boundary based on history of mouse turns
					}
					rThresh0 = rThresh0 * catchThresh;  // Support catch trials
					Debug.Log ("Loc: [0, " + rThresh0 + ", " + catchThresh + ", 1] - " + r);
					// If r is less than rThresh0, enable target #0; if not, if r is less than catchThresh, enable target #1; if not then do a catch trial (no targets enabled)
					treeToActivate = r < rThresh0 ? 0 : r < catchThresh ? 1 : Globals.CATCH_IDX;
					while (true) {
						// Don't allow the very first trial to be a catch trial, as that might be more confusing for the mouse
						if ((Globals.numNonCorrectionTrials == 1 && treeToActivate == Globals.CATCH_IDX) || (Globals.numNonCorrectionTrials > 1 && Globals.CurrentlyCatchTrial() && treeToActivate == Globals.CATCH_IDX)) {
							r = UnityEngine.Random.value;
							treeToActivate = r < rThresh0 ? 0 : r < catchThresh ? 1 : Globals.CATCH_IDX;
						} else {
							break;
						}
					}						

					if (gos.Length == 2) {
						if (blockSize > 0) {
							treeToActivate = Globals.GetTreeToActivateFromBlock ();
							//Debug.Log (treeToActivate);
							if (treeToActivate != Globals.CATCH_IDX) {
								hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
								vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
								angle = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ();
							}
						}
						SetupTreeActivation (gos, treeToActivate, 2);
					} else if (gameType.Equals ("det_target")) {
						SetupTreeActivation (gos, treeToActivate, 2);
						gos [2].GetComponent<WaterTreeScript> ().SetShader (hfreq, vfreq, angle);
						if (Globals.varyOrientation) {
							float r2 = UnityEngine.Random.value;
							if (r2 > 0.5) { // Swap orientation of target tree
								if (treeToActivate != Globals.CATCH_IDX) {
									gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq, angle);
								}
								gos [2].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq, angle);
							}
							Debug.Log ("Ori: [0, 0.5, 1] - " + r2);
						}
					} else if (gameType.Equals ("disc_target")) {
						float r2 = UnityEngine.Random.value;
						int treeToDistract = treeToActivate == 1 ? 0 : 1;  // Likely won't support catch trials - think about this later if needed

						gos [treeToActivate].GetComponent<WaterTreeScript> ().SetCorrect (true);
						gos [treeToDistract].GetComponent<WaterTreeScript> ().SetCorrect (false);

						if (Globals.varyOrientation) {
							gos [treeToActivate].GetComponent<WaterTreeScript> ().SetColors (new Color (1, 1, 1), new Color (0, 0, 0));
							if (r2 < 0.5) {
								gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (4, 1);
							} else {
								gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (1, 4);
							}
							hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
							vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
							angle = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ();
							gos [2].GetComponent<WaterTreeScript> ().SetShader (hfreq, vfreq, angle);

							gos [treeToDistract].GetComponent<WaterTreeScript> ().SetColors (Globals.distColor1, Globals.distColor2);
							gos [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq, angle);
						} else {
							gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (4, 4);
							if (r2 < 0.5) {
								gos [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (4, 1);
							} else {
								gos [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (1, 4);
							}
						}
						Debug.Log ("Ori: [0, 0.5, 1] - " + r2);
					}
					if (treeToActivate != Globals.CATCH_IDX) {
						loc = gos [treeToActivate].transform.position;
						hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						angle = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					}
				} else if (gos.Length == 4) {
					if (blockSize > 0) {
						treeToActivate = Globals.GetTreeToActivateFromBlock();
					} else {
						float catchThresh = 1 - Globals.catchFreq;
						float thresh0 = 0.25F;
						float thresh1 = 0.5F;
						float thresh2 = 0.75F;

						if (Globals.biasCorrection && Globals.CurrentWorldHasAlreadyAppeared()) {
							// Turn on bias correction after testing that logic works!
							// Bias correction algo #1
							float tf0 = Globals.GetTurnBias (20, 0);
							float tf1 = Globals.GetTurnBias (20, 1);
							float tf2 = Globals.GetTurnBias (20, 2);
							float tf3 = 1 - (tf0 + tf1 + tf2);

							float t0 = 1 - tf0;
							float t1 = 1 - tf1;
							float t2 = 1 - tf2;
							float t3 = 1 - tf3;

							Debug.Log ("turning biases: " + tf0 + ", " + tf1 + ", " + tf2 + ", " + tf3);

							thresh0 = t0 / (t0 + t1 + t2 + t3);
							thresh1 = t1 / (t0 + t1 + t2 + t3) + thresh0;
							thresh2 = t2 / (t0 + t1 + t2 + t3) + thresh1;
						}

						thresh0 = thresh0 * catchThresh;
						thresh1 = thresh1 * catchThresh;
						thresh2 = thresh2 * catchThresh;

						Debug.Log ("random: " + r + " --- range: [0, " + thresh0 + ", " + thresh1 + ", " + thresh2 + ", " + catchThresh + ", 1]");

						treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : r < thresh2 ? 2 : r < catchThresh ? 3 : Globals.CATCH_IDX;
						while (true) {
							// Don't allow the very first trial to be a catch trial, as that might be more confusing for the mouse
							if ((Globals.numNonCorrectionTrials == 1 && treeToActivate == Globals.CATCH_IDX) || (Globals.numNonCorrectionTrials > 1 && Globals.firstTurnHFreq[Globals.firstTurnLoc.Count-1] == Globals.CATCH_IDX && treeToActivate == Globals.CATCH_IDX)) {
								r = UnityEngine.Random.value;
								treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : r < thresh2 ? 2 : r < catchThresh ? 3 : Globals.CATCH_IDX;
							} else {
								break;
							}
						}
					}
					SetupTreeActivation (gos, treeToActivate, 4); // it is important that this happens before the following clause as the following clause might activate more targets to test for extinction
					if (treeToActivate != Globals.CATCH_IDX) {
						// If is an extinction trial, show the rear target
						if (Globals.CurrentlyExtinctionTrial()) {  
							gos [treeToActivate+2].GetComponent<WaterTreeScript> ().Show ();  // Activate rear left or rear right tree
							Debug.Log("in extinction trial");
						}
						loc = gos [treeToActivate].transform.position;
						hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						angle = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					}
				}
			} else if (gameType.Equals ("det_blind")) {
				if (gos.Length == 3) {
					if (blockSize > 0) {
						treeToActivate = Globals.GetTreeToActivateFromBlock();
					} else {
						float catchThresh = 1 - Globals.catchFreq;
						double thresh0 = 0.333D;
						double thresh1 = 0.666D;

						if (Globals.biasCorrection && Globals.numNonCorrectionTrials > 1) {
							float tf0 = Globals.GetTurnBias (20, 0);
							float tf1 = Globals.GetTurnBias (20, 1);

							// Algorithm #1 - Ninny kept refusing to go straight, so modified
							/*
							float t0 = 1 - tf0;
				            float t1 = 1 - tf1;
				            float t2 = tf0 + tf1;

				            float thresh0 = t0 / (t0 + t1 + t2);
				            float thresh1 = t1 / (t0 + t1 + t2) + thresh0;
				            */

							// Algorithm #2: Treat as 2 line equation, solve, rebalance and normalize

							float tf2 = 1 - (tf0 + tf1);

							Debug.Log ("turning biases: " + tf0 + ", " + tf1 + ", " + tf2);

							// Solve
							double p0 = tf0 < 1 / 3 ? -2 * tf0 + 1 : -tf0 / 2 + 0.5;
							double p1 = tf1 < 1 / 3 ? -2 * tf1 + 1 : -tf1 / 2 + 0.5;
							double p2 = tf2 < 1 / 3 ? -2 * tf2 + 1 : -tf2 / 2 + 0.5;

							//Debug.Log ("raw trial prob: " + p0 + ", " + p1 + ", " + p2);

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
						} else if (!Globals.biasCorrection) {
							bool allTreesHavePresoFrac = true;
							for (int i = 0; i < gos.Length; i++) {
								if (gos [i].GetComponent<WaterTreeScript> ().GetPresoFrac () < 0) {  // Not set, so ignore all presoFracs, if others were set
									allTreesHavePresoFrac = false;
								}
							}
							if (allTreesHavePresoFrac) {
								thresh0 = gos [0].GetComponent<WaterTreeScript> ().GetPresoFrac ();
								thresh1 = thresh0 + gos [1].GetComponent<WaterTreeScript> ().GetPresoFrac ();
							}
						}

						thresh0 = thresh0 * catchThresh;
						thresh1 = thresh1 * catchThresh;

						Debug.Log ("STIMLOC: [0, " + thresh0 + ", " + thresh1 + ", " + catchThresh + ", 1] - " + r);
						treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : r < catchThresh ? 2 : Globals.CATCH_IDX;
						while (true) {
							// Don't allow the very first trial to be a catch trial or 2 catch trials in a row, as that might be more confusing for the mouse
							if ((Globals.numNonCorrectionTrials == 1 && treeToActivate == Globals.CATCH_IDX) || (Globals.numNonCorrectionTrials > 1 && Globals.firstTurnHFreq[Globals.firstTurnLoc.Count-1] == Globals.CATCH_IDX && treeToActivate == Globals.CATCH_IDX)) {
								r = UnityEngine.Random.value;
								treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : r < catchThresh ? 2 : Globals.CATCH_IDX;
							} else {
								break;
							}
						}
					}

					SetupTreeActivation (gos, treeToActivate, 3);
					Debug.Log (treeToActivate);
					if (treeToActivate != Globals.CATCH_IDX) {  // enable the center target only if this is not a catch trial
						// If not an extinction trial, show the center target
						if (!Globals.CurrentlyExtinctionTrial()) {  
							gos [2].GetComponent<WaterTreeScript> ().Show ();  // Activate center tree
						}
						loc = gos [treeToActivate].transform.position;
						hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						angle = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					}
				} else if (gos.Length == 2) { // For training lesioned animals who have not been previously trained
					if (blockSize > 0) {
						treeToActivate = Globals.GetTreeToActivateFromBlock();
					} else {
						float catchThresh = 1 - Globals.catchFreq;
						float rThresh0 = 0.5F;
						if (Globals.presoRatio > 0) { // Works for 2-choice only, YN and 2AFC
							rThresh0 = (float)Globals.presoRatio / (Globals.presoRatio + 1);
						}
						if (Globals.biasCorrection && Globals.numNonCorrectionTrials > 1) {
							rThresh0 = 1 - Globals.GetTurnBias (20, 0);  // varies the boundary based on history of mouse turns
						}
						rThresh0 = rThresh0 * catchThresh;  // Support catch trials
						Debug.Log ("Loc: [0, " + rThresh0 + ", " + catchThresh + ", 1] - " + r);
						// If r is less than rThresh0, enable target #0; if not, if r is less than catchThresh, enable target #1; if not then do a catch trial (no targets enabled)
						treeToActivate = r < rThresh0 ? 0 : r < catchThresh ? 1 : Globals.CATCH_IDX;
						while (true) {
							// Don't allow the very first trial to be a catch trial, as that might be more confusing for the mouse
							if ((Globals.numNonCorrectionTrials == 1 && treeToActivate == Globals.CATCH_IDX) || (Globals.numNonCorrectionTrials > 1 && Globals.firstTurnHFreq[Globals.firstTurnLoc.Count-1] ==Globals.CATCH_IDX && treeToActivate == Globals.CATCH_IDX)) {
								r = UnityEngine.Random.value;
								treeToActivate = r < rThresh0 ? 0 : r < catchThresh ? 1 : Globals.CATCH_IDX;
							} else {
								break;
							}
						}
					}

					SetupTreeActivation (gos, treeToActivate, 2);
					if (treeToActivate != Globals.CATCH_IDX) { // On blind test catch trials, even hide the persistent center tree!
						gos [1].GetComponent<WaterTreeScript> ().Show ();  // Activate center tree - only necessary with persistent shadow
						loc = gos [treeToActivate].transform.position;
						hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						angle = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					}
				}
			} else if (gameType.Equals ("discrimination")) {  // No concept of catch trials yet for the discrimination task
				// Randomize orientations
				float rThresh0 = 0.5F;
				if (Globals.numNonCorrectionTrials > 1 && Globals.biasCorrection) {  // Add bias correction option if needed later
					rThresh0 = 1 - Globals.GetTurnBias (20, 0);
				}

				// Randomize phase, if enabled
				float rewardedHPhase = Globals.randomPhase && Globals.rewardedHFreq != 1 ? UnityEngine.Random.value * 360 : 0;
				float rewardedVPhase = Globals.randomPhase && Globals.rewardedVFreq != 1 ? UnityEngine.Random.value * 360 : 0;
				float distractorHPhase = Globals.randomPhase && Globals.distractorHFreq != 1 ? UnityEngine.Random.value * 360 : 0;
				float distractorVPhase = Globals.randomPhase && Globals.distractorVFreq != 1 ? UnityEngine.Random.value * 360 : 0;

				// Randomize distractor, if multiple
				int rDistAng = UnityEngine.Random.Range (0, Globals.distractorAngles.Count ());
				distractorAngle = Globals.distractorAngles [rDistAng];
				Debug.Log ("Picked distractorAngle = " + distractorAngle);

				if (r < rThresh0) {
					gos [0].GetComponent<WaterTreeScript> ().SetShader (Globals.rewardedHFreq, rewardedHPhase, Globals.rewardedVFreq, rewardedVPhase, Globals.rewardedAngle); // later, listen to the deg argument
					gos [1].GetComponent<WaterTreeScript> ().SetShader (Globals.distractorHFreq, distractorHPhase, Globals.distractorVFreq, distractorVPhase, distractorAngle);
					loc = gos [0].transform.position;
					hfreq = gos [0].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
					vfreq = gos [0].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
					angle = gos [0].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					gos [0].GetComponent<WaterTreeScript> ().SetCorrect (true);
					gos [1].GetComponent<WaterTreeScript> ().SetCorrect (false);
				} else {
					gos [0].GetComponent<WaterTreeScript> ().SetShader (Globals.distractorHFreq, distractorHPhase, Globals.distractorVFreq, distractorVPhase, distractorAngle);
					gos [1].GetComponent<WaterTreeScript> ().SetShader (Globals.rewardedHFreq, rewardedHPhase, Globals.rewardedVFreq, rewardedVPhase, Globals.rewardedAngle);
					loc = gos [1].transform.position;
					hfreq = gos [1].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
					vfreq = gos [1].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
					angle = gos [1].GetComponent<WaterTreeScript> ().GetShaderRotation ();
					gos [0].GetComponent<WaterTreeScript> ().SetCorrect (false);
					gos [1].GetComponent<WaterTreeScript> ().SetCorrect (true);
				}
				Debug.Log ("[0, " + rThresh0 + ", 1] - " + r);
			} else if (gameType.Equals ("match") || gameType.Equals ("nonmatch")) { // No concept of catch trials yet for this task
				// First, pick an orientation at random for the central tree
				float targetHFreq = gos [2].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
				float targetVFreq = gos [2].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
				float targetAngle = gos [2].GetComponent<WaterTreeScript> ().GetShaderRotation ();

				if (r < 0.5) { // Switch target to opposite of previous
					gos [2].GetComponent<WaterTreeScript> ().SetShader (targetVFreq, targetHFreq, targetAngle);
				}
				// Second, randomly pick which side the matching orientation is on
				float rSide = UnityEngine.Random.value;

				// Try to balance location of match based on the turning bias
				float rThresh0 = 0.5F;
				if (Globals.numNonCorrectionTrials > 1) {  // Add bias correction option if needed later
					rThresh0 = 1 - Globals.GetTurnBias (20, 0);
				}
				if (rSide < rThresh0) { // Set the left tree to be the rewarded side
					gos [0].GetComponent<WaterTreeScript> ().SetCorrect (true);
					gos [1].GetComponent<WaterTreeScript> ().SetCorrect (false);
					loc = gos [0].transform.position;
					// TODO: Fix the angle treatment here
					if (gameType.Equals ("match")) {
						gos [0].GetComponent<WaterTreeScript> ().SetShader (targetHFreq, targetVFreq, targetAngle);
						gos [1].GetComponent<WaterTreeScript> ().SetShader (targetVFreq, targetHFreq, targetAngle);
						// TODO - do the right thing with angles here, LATER
						hfreq = targetHFreq;
						vfreq = targetVFreq;
						angle = targetAngle;
					} else {
						gos [0].GetComponent<WaterTreeScript> ().SetShader (targetVFreq, targetHFreq, targetAngle);
						gos [1].GetComponent<WaterTreeScript> ().SetShader (targetHFreq, targetVFreq, targetAngle);
						hfreq = targetVFreq;
						vfreq = targetHFreq;
						angle = targetAngle;
					}
				} else { // Set the right tree to match
					gos [0].GetComponent<WaterTreeScript> ().SetCorrect (false);
					gos [1].GetComponent<WaterTreeScript> ().SetCorrect (true);
					loc = gos [1].transform.position;
					if (gameType.Equals ("match")) {
						gos [0].GetComponent<WaterTreeScript> ().SetShader (targetVFreq, targetHFreq, targetAngle);
						gos [1].GetComponent<WaterTreeScript> ().SetShader (targetHFreq, targetVFreq, targetAngle);
						hfreq = targetHFreq;
						vfreq = targetVFreq;
						angle = targetAngle;
					} else {
						gos [0].GetComponent<WaterTreeScript> ().SetShader (targetHFreq, targetVFreq, targetAngle);
						gos [1].GetComponent<WaterTreeScript> ().SetShader (targetVFreq, targetHFreq, targetAngle);
						hfreq = targetVFreq;
						vfreq = targetHFreq;
						angle = targetAngle;
					}
				}
				Debug.Log ("[0, " + rThresh0 + ", 1] - " + rSide);
			}

			// On correction trials, the occluders will already be set, so only need to do the below calculation on new trials
			// NO bias correction with FOV location yet, but may need to add later
			if (Globals.perim && (((gos.Length == 3 || gos.Length == 2) && loc.x != Globals.worldXCenter) || gos.Length == 4)) {  // perimetry is enabled, so pick from the set of random windows to use
				int start;
				int end = Globals.fovsForPerimScaleInclusive [Globals.perimScale];
				if (Globals.perimRange) {  // Also include larger size stim in the stim set
					start = 0;
				} else {  // Only include stim that are of the size specified in the scenario file
					start = end - Globals.fovsForPerimScaleInclusive [Globals.perimScale - 1];
					if (Globals.perimScale == 1) { // Special case
						start = start - 1;
					}
				}
				int rFOV = UnityEngine.Random.Range (start, end);
				Debug.Log ("FOV range (" + start + " - " + end + "), picked " + rFOV);
				Globals.SetOccluders (loc.x, rFOV);
			} else {
				Globals.SetOccluders (loc.x);
				//Debug.Log ("no dynamic occlusion");
			}

			// OPTOGENETICS!
			if (Globals.optoSide != -1) {  // A side for optogenetics was specified
				if (Globals.optoAlternation) {  // If it should alternate, then alternate it, with every even trial getting light on  NOTE: this feature does not support LorR
					if (Globals.probeIdx.Contains (treeToActivate)) { 				// if the current trial is a probe trial
						if (Globals.probeLastOpto == false) {  // if the last trial was light OFF, turn light on this time
							if (Globals.optoSide == Globals.optoLorR) {
								optoState = Globals.optoL;
							} else {
								optoState = Globals.optoSide;
							}
						}
						Globals.probeLastOpto = !Globals.probeLastOpto;  // regardless of whether last probe was light on or off, alternate
					} else if (!Globals.probeIdx.Contains (treeToActivate) && Globals.numNonCorrectionTrials % 2 == 0) {
						if (Globals.optoSide == Globals.optoLorR) {
							optoState = Globals.optoL;
						} else {
							optoState = Globals.optoSide;
						}
					}
				} else {
					if (blockSize > 0) {
						optoState = Globals.GetCurrentWorld().precompOptoBlock [(int)Math.Ceiling((double)Globals.numNonCorrectionTrials / Globals.worlds.Count - 1) % blockSize];
					} else {
						float rOpto = UnityEngine.Random.value;
						if (rOpto < Globals.optoFraction) {  // also does not support optoLorR
							if (Globals.optoSide == Globals.optoLorR) {
								optoState = Globals.optoL;
							} else {
								optoState = Globals.optoSide;
							}
						}
					}
				}
			}
			idx = treeToActivate;

			if (treeToActivate == Globals.CATCH_IDX) { // This is a catch trial
				Globals.numCatchTrials++;
			}
		}

		if (Globals.CurrentlyExtinctionTrial ()) {  
			Globals.extinctionTrialMarks.Add (1);
		} else {
			Globals.extinctionTrialMarks.Add (0);
		}

		Globals.targetIdx.Add (idx);
        Globals.targetLoc.Add(loc);
        Globals.targetHFreq.Add(hfreq);
        Globals.targetVFreq.Add(vfreq);
		Globals.targetAngle.Add(angle);
		Globals.distractorAngle.Add (distractorAngle);
		udpSender.GetComponent<UDPSend> ().OptoTurnOn (optoState);  // Just reuse the last optoState for optogenetic correction trials
		Globals.optoStates.Add (optoState);
		Globals.currOptoState = optoState;  // Used to toggle opto state by user
					
        this.runTime = Time.time;
		this.movementRecorder.SetRun(this.runNumber);  // indicates movement recorder data is going into a new trial file
		this.movementRecorder.SetFileSet(true);
		Color t = this.fadeToBlack.color;
		t.a = 0f;
		this.fadeToBlack.color = t;
		this.fadeToBlackText.text = "";
		this.fadeToBlack.gameObject.SetActive(false);

		this.firstFrameRun = true;
		this.debugControlScript.enabled = true;

		Globals.hasNotTurned = true;
		Globals.hiddenShown = false;  // Flag to keep track of whether the hidden target have been shown on this trial

        Globals.trialStartTime.Add(DateTime.Now);
		lastTrialStartDateTime = DateTime.Now;

		// Update again after the pause, as the world might have changed between trials
		updateCorrectTurnsText();
		this.lastAccuracyText.text = "Last 20 accuracy (in this world): " + Math.Round(Globals.GetLastAccuracy(20) * 100) + "%";
        this.state = "Running";
	}

    private void SetupTreeActivation(GameObject[] gos, int treeToActivate, int maxTrees) {
		for (int i = 0; i < maxTrees; i++) {
            gos[i].SetActive(true);
			gos [i].GetComponent<WaterTreeScript> ().SetCorrect (false);
			if (i == treeToActivate) {
				gos [i].GetComponent<WaterTreeScript> ().SetCorrect (true);
				List<int> hiddenIdx = Globals.GetCurrentWorld ().hiddenIdx;
				if (hiddenIdx.Count > 0 && hiddenIdx.Contains (treeToActivate)) {
					gos [i].GetComponent<WaterTreeScript> ().Hide ();
				} else {
					gos [i].GetComponent<WaterTreeScript> ().Show ();
				}
			} else {
				gos [i].GetComponent<WaterTreeScript> ().Hide ();
			}
        }
    }

    private void GameOver() {
		this.movementRecorder.SetFileSet (false);  // Stop the movement recorder as the game is over - previous files kept recording location
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;
        this.fadeToBlackText.text = "GAME OVER MUSCULUS!";
		if (Globals.gameEndTime == DateTime.MinValue) {
			Globals.gameEndTime = DateTime.Now;
			Debug.Log ("Set end time to now");
		}

		if (Input.GetKeyUp(KeyCode.Escape)) {
            StartCoroutine(CheckForQ());
			this.state = "WaitingForQuitCmd";
        }
		if (Input.GetKeyUp (KeyCode.R)) {
			this.state = "Respawn";
		}
    }

    private IEnumerator CheckForQ() {
        Debug.Log("Waiting for Q");
        yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Q));
        Debug.Log("quitting!");
		this.udpSender.close();

		if (DateTime.Compare(Globals.gameStartTime, DateTime.MinValue) != 0) {  // Only record stats if the game has started, otherwise just exit
			Globals.WriteStatsFile ();  // make sure before WriteStatsToGoogleSheet();
			bool wroteData = Globals.WriteStatsToGoogleSheet ();  // sometimes fails due to bad internet connection?  
			if (wroteData) {
				Application.Quit ();
			} else {
				this.fadeToBlackText.text = "Data not saved in sheets, so manually enter";
				this.state = "NotSavedToSheets";
				yield return new WaitUntil (() => Input.GetKeyUp (KeyCode.Q));
				Application.Quit ();
			}
		} else {
			Application.Quit ();
		}

    }

    private void MovePlayer() {
		if (Globals.newData) {
			Globals.newData = false;

			if (this.last5Mouse2Y.Count == smoothingWindow)
				this.last5Mouse2Y.Dequeue ();
			if (this.last5Mouse1Y.Count == smoothingWindow)
				this.last5Mouse1Y.Dequeue ();

			// Mouse 1 does rotation, mouse 2 does forward/backward movement
			// Updated to account for new design where the mice are rotated sideways
			if (Globals.numMice == 2) {
				if (Globals.gameTurnControl.Equals ("roll"))
					this.last5Mouse1Y.Enqueue (Globals.sphereInput.mouse1X);
				else
					this.last5Mouse1Y.Enqueue (Globals.sphereInput.mouse1Y);
				this.last5Mouse2Y.Enqueue (Globals.sphereInput.mouse2X);
			} else if (Globals.numMice == 1) {  // In this case, mouse is also rotated sideways, so x and y are flipped
				// Be sure to run MouseUDP_1_mouse.py on RPi, instead of regular MouseUDP.py
				// The if statements deal with mice that give noisy data on some ball movements
				if (Math.Abs (Mathf.Rad2Deg * Globals.sphereInput.mouse1Y / this.rawRotationDivider) < 2) {
					this.last5Mouse1Y.Enqueue (Globals.sphereInput.mouse1Y);
				} else {
					this.last5Mouse1Y.Enqueue (this.last5Mouse1Y.Average ());
				}
				if (Math.Abs (Globals.sphereInput.mouse1X / (this.rawSpeedDivider / Globals.speedAdjustment)) < (1 * Globals.speedAdjustment)) {
					this.last5Mouse2Y.Enqueue (Globals.sphereInput.mouse1X);
				} else {
					this.last5Mouse2Y.Enqueue (this.last5Mouse2Y.Average ());
				}

				Debug.Log (Mathf.Rad2Deg * Globals.sphereInput.mouse1Y / this.rawRotationDivider);
				Debug.Log (Globals.sphereInput.mouse1X / (this.rawSpeedDivider / Globals.speedAdjustment));
			}
		
			// transform sphere data into unity movement
			//if (this.frameCounter - this.previousFrameCounter > 1)
			//print("lost packets: " + this.frameCounter + "/" + this.previousFrameCounter);
			this.previousFrameCounter = this.frameCounter;

			this.player.transform.Rotate(Vector3.up, Mathf.Rad2Deg * (this.last5Mouse1Y.Average()) / this.rawRotationDivider);
            
			Vector3 rel = this.player.transform.forward * (this.last5Mouse2Y.Average () / (this.rawSpeedDivider / Globals.speedAdjustment));
			//this.player.transform.position = this.player.transform.position + rel;
			this.characterController.Move (rel);
			this.udpSender.SendMousePos (this.player.transform.position);
			this.udpSender.SendMouseRot (this.player.transform.rotation.eulerAngles.y);


			// Send UDP msg out
			//this.udpSender.SendPlayerState(this.player.transform.position, this.player.transform.rotation.eulerAngles.y, Globals.playerInWaterTree, Globals.playerInDryTree);
		} else {
			//Debug.Log ("no new data");
		}
    }

    public void FlushWater() {
        this.udpSender.FlushWater();
    }

	private void updateTrialsText() {
		this.numberOfTrialsText.text = "Trial: #" + Globals.numNonCorrectionTrials.ToString ();
		if (Globals.CurrentlyCorrectionTrial () && Globals.worldIdxList[Globals.worldIdxList.Count - 1] == Globals.worldIdxList[Globals.worldIdxList.Count - 2])  // Need to make sure current world is same as prev to keep this label up
			this.numberOfTrialsText.text += " (CORRECTION #" + Globals.numCorrectionTrialsSinceLastCorrectTrial + ")";
	}

	private void updateRewardAmountText() {
		this.rewardAmountText.text = "Reward: " + Math.Round(Globals.rewardAmountSoFar).ToString() + " of " + Math.Round(Globals.totalRewardSize) + " ul";
	}

	private void updateCorrectTurnsText() {
		float numNonCorrectionTrials = (float)Globals.numNonCorrectionTrials - 1 - Globals.numCatchTrials;
		if (Globals.CurrentlyCorrectionTrial ()) {
			numNonCorrectionTrials = numNonCorrectionTrials + 1;
		}
		this.numberOfCorrectTurnsText.text = "Correct: " +
			Globals.numCorrectTurns.ToString ()
			+ " (" +
			Mathf.Round((float)Globals.numCorrectTurns / numNonCorrectionTrials * 100).ToString() + "%" 
			+ Globals.GetTreeAccuracy(false) + ")";
	}

}