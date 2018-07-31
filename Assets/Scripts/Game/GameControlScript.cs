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
    private bool firstFrameRun;
    private bool playerInWaterTree, playerInDryTree;
    private Loader scenarioLoader;
    private CharacterController characterController;
    private DebugControl debugControlScript;
    private bool timeoutState;

	private int smoothingWindow = 1;  // Amount to smoothen the player movement
	private bool waitedOneFrame = false;  // When mouse hits tree, need to wait a few frames before it turns black, and then pause the game

	private Vector3 startingPos;
	private Quaternion startingRot;
	private Vector3 prevPos;

	private int centralViewVisible;

	private DateTime pauseStartTime;
	private int pauseStart = 0;
	private int pauseEnd = 0;
	private int pauseTime = 3; // frames - 1 is too few given noise in capture

    // Use this for initialization
    void Start()
    {
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
		Globals.numberOfTrials = 1;  // Start on first trial
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
		Globals.InitFOVs ();
    }

    // Update is called once per frame
    void Update()
	{
		if ((this.state == "Running" || this.state == "Paused") && Globals.numCameras > 0) {
			// To detect the occurence of a trial start or end, skip a frame trigger so that Matlab gets the signal direct through the camera,
			// as the signal directly from unity is flaky
			if (pauseStart > 0) {
				pauseStart = pauseStart-1;
			} else if (pauseEnd > 0) {
				pauseEnd = pauseEnd-1;
			} else {
				Globals.currFrame = Globals.currFrame + 1;
				this.udpSender.SendFrameTrigger ();
			}
		}
			
		// Keep mouse from scaling walls - 
		if (this.player.transform.position.y > this.startingPos.y + 0.1) {
			Vector3 tempPos = this.player.transform.position;
			tempPos.y = this.startingPos.y;
			tempPos.x = this.prevPos.x;
			tempPos.z = this.prevPos.z;
			this.player.transform.position = tempPos;
		}
		this.prevPos = this.player.transform.position;

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

	void LateUpdate() {
		//Debug.Log ("Framerate: " + 1.0f / Time.deltaTime);
		CatchKeyStrokes ();
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

		Debug.Log ("Init view value: " + _centralViewVisible);

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

    private void CatchKeyStrokes()
    {
		if (Input.GetKey(KeyCode.Escape) && !this.state.Equals("WaitingForQuitCmd"))
            this.state = "GameOver";
        
        if (!this.state.Equals("LoadScenario") || (this.state.Equals("LoadScenario") && EventSystem.current.currentSelectedGameObject == null))
        {
            if (Input.GetKeyUp(KeyCode.U))
            {
                this.udpSender.SendWaterReward(Globals.rewardDur);
                Globals.numberOfUnearnedRewards++;
                Globals.rewardAmountSoFar += Globals.rewardSize;
				updateRewardAmountText ();
				Debug.Log("gave reward = " + Globals.rewardAmountSoFar);
            } else if (Input.GetKeyUp(KeyCode.T))
            {
                // Mouse is stuck so teleport to beginning
                TeleportToBeginning();
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
    private void LoadScenario(){
        if (this.scenarioLoader.scenarioLoaded == true) {
            this.menuPanel.SetActive(false);
			Respawn ();
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
            Globals.gameStartTime = DateTime.Now;
            Debug.Log("Game started at " + Globals.gameStartTime.ToLongTimeString());
            this.movementRecorder.SetRun(this.runNumber);
            this.movementRecorder.SetFileSet(true);
            Color t = this.fadeToBlack.color;
            t.a = 0f;
            this.fadeToBlack.color = t;
            this.fadeToBlackText.text = "";
            this.fadeToBlack.gameObject.SetActive(false);

            this.firstFrameRun = true;
            this.debugControlScript.enabled = true;
			Globals.hasNotTurned = true;
			Globals.numCorrectTurns = 0;
            this.characterController.enabled = true;  // Bring back character movement
            this.state = "Running";

			this.mouseNameText.text = "Name: " + Globals.mouseName;
			//this.scenarioNameText.text = "Scenario: " + Globals.scenarioName + " (Day " + Globals.trainingDayNumber + ", session #" + Globals.scenarioSessionNumber + ", setting " + Globals.inputDeg + ")";
			this.scenarioNameText.text = "Scenario: " + Globals.scenarioName + " (Day " + Globals.trainingDayNumber + ", session #" + Globals.scenarioSessionNumber + ", fov " + Globals.visibleNasalBoundary + ", " 
				+ Globals.visibleTemporalBoundary + ", " + Globals.visibleHighBoundary + ", " + Globals.visibleLowBoundary + ")";

			Globals.InitLogFiles();
            Globals.trialStartTime.Add(DateTime.Now.TimeOfDay);
        }
    }

    /*
     * dry trees timeout state
     * */
    private void Timeout()
    {
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;

        if (!Globals.timeoutState)
        {
            StartCoroutine(Wait());
            Globals.timeoutState = true;
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
		// send SYNC msg on first frame of every run.
        if (this.firstFrameRun) {
            this.udpSender.SendRunSync();
            this.firstFrameRun = false;
        }

        if (Globals.playerInDryTree && !Globals.timeoutState) {
            this.state = "Timeout";
        }

        MovePlayer();
		if (this.udpSender.CheckReward ())
			this.movementRecorder.logReward(false, true);
		updateTrialsText();
		updateRewardAmountText ();

        TimeSpan te = DateTime.Now.Subtract(Globals.gameStartTime);
		this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}:{2:G4}:{3}", te.Hours * 60 + te.Minutes, te.Seconds, Time.deltaTime * 1000, frameCounter++);
        if (Time.time - this.runTime >= this.runDuration)
        {
            // fadetoblack + respawn
            this.movementRecorder.SetFileSet(false);
            this.fadeToBlack.gameObject.SetActive(true);
            this.state = "Fading";
        }
	}

    public void OccludeTree(float treeLocX)
    {
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
		updateRewardAmountText ();
        if (Globals.numberOfTrials > 1) {
			updateCorrectTurnsText ();
			this.lastAccuracyText.text = "Last 20 accuracy (in this world): " + Math.Round(Globals.GetLastAccuracy(20) * 100) + "%";
        }
			
		// Update the time display
		TimeSpan te = DateTime.Now.Subtract(Globals.gameStartTime);
		this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}:{2:G4}:{3}", te.Hours * 60 + te.Minutes, te.Seconds, Time.deltaTime * 1000, frameCounter++);

		// Only proceed if elapsed time is greater than or equal to trialDelay
		te = DateTime.Now.Subtract(pauseStartTime);
		if (te.TotalMilliseconds >= Globals.trialDelay * 1000) {
			float totalEarnedRewardSize = 0;
			float totalRewardSize = 0;
			for (int i = 0; i < Globals.sizeOfRewardGiven.Count; i++) {
				totalEarnedRewardSize += (float)System.Convert.ToDouble(Globals.sizeOfRewardGiven[i]);
			}
			totalRewardSize = totalEarnedRewardSize + Globals.numberOfUnearnedRewards * Globals.rewardSize;
			//Debug.Log("Total reward so far: " + totalRewardSize + "; maxReward = " + Globals.totalRewardSize);
			pauseEnd = pauseTime;
			if (totalRewardSize >= Globals.totalRewardSize)
				this.state = "GameOver";
			else
				this.state = "Respawn";
		}
	}

    /*
     * Reset all trees
     * */
	public void ResetScenario(Color c)
    {
        this.runTime = Time.time;
        this.runNumber++;

        //print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
        foreach (WaterTreeScript script in GameObject.Find("Trees").GetComponentsInChildren<WaterTreeScript>()) {
            script.Refill();
		}
        //print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
        this.debugControlScript.enabled = false;

		// NB edit (1 line)
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = c;
		this.state = "Paused";

		this.pauseStartTime = DateTime.Now;
		this.pauseStart = pauseTime;

		// Clear opto LEDs, if they were enabled, and pause duration gives some recovery
		udpSender.GetComponent<UDPSend>().OptoTurnOffAll();

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

    private void Respawn() {
        TeleportToBeginning();

		Globals.ClearWorld (); // Wipes out all trees and walls, only to be rendered again 
		Globals.RenderWorld (-1); // -1 indicates that the world rendered should be randomly selected, without any bias correction (i.e. worlds in which accuracy is better do not occur less than worlds in which accuracy is worse)

		GameObject[] gos = Globals.GetTrees ();

		// Just initial values, used only if there is 1 tree
        float locx = gos[0].transform.position.x;
        float hfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
        float vfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderVFreq();

        int treeToActivate = 0;
        float r = UnityEngine.Random.value;
		if (Globals.gameType.Equals("detection") || Globals.gameType.Equals("det_target") || Globals.gameType.Equals("disc_target")) {
			if (gos.Length == 1)  { // Linear track
                if (Globals.varyOrientation) {
					if (r > 0.5) { // Half the time, swap the orientation of the tree
                        gos[0].GetComponent<WaterTreeScript>().SetShader(vfreq, hfreq);
                        hfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
                        vfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
                    }
                }
            } else if (gos.Length == 2 || Globals.gameType.Equals("det_target") || Globals.gameType.Equals("disc_target")) {
				float rThresh0 = 0.5F;
				if (Globals.presoRatio > 0) { // Works for 2-choice only, YN and 2AFC
					rThresh0 = (float)Globals.presoRatio / (Globals.presoRatio + 1);
				}
				if (Globals.biasCorrection && Globals.numberOfTrials > 1) {
	                rThresh0 = 1 - Globals.GetTurnBias(20, 0);  // varies the boundary based on history of mouse turns
				}
                Debug.Log("Loc: [0, " + rThresh0 + ", 1] - " + r);
                treeToActivate = r < rThresh0 ? 0 : 1;
				hfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
				vfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();

				if (gos.Length == 2) {
					SetupTreeActivation (gos, treeToActivate, 2);
				} else if (Globals.gameType.Equals ("det_target")) {
					SetupTreeActivation (gos, treeToActivate, 2);
					gos [2].GetComponent<WaterTreeScript> ().SetShader (hfreq, vfreq);
					if (Globals.varyOrientation) {
						float r2 = UnityEngine.Random.value;
						if (r2 > 0.5) { // Swap orientation of target tree
							gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
							gos [2].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
						}
						Debug.Log ("Ori: [0, 0.5, 1] - " + r2);
					}
				} else if (Globals.gameType.Equals ("disc_target")) {
					float r2 = UnityEngine.Random.value;
					int treeToDistract = treeToActivate==1 ? 0 : 1;

					gos[treeToActivate].GetComponent<WaterTreeScript>().SetCorrect(true);
					gos[treeToDistract].GetComponent<WaterTreeScript>().SetCorrect(false);

					if (Globals.varyOrientation) {
						gos [treeToActivate].GetComponent<WaterTreeScript> ().SetColors(new Color(1, 1, 1), new Color(0, 0, 0));
						if (r2 < 0.5) {
							gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (4, 1);
						} else {
							gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (1, 4);
						}
						hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						gos [2].GetComponent<WaterTreeScript> ().SetShader (hfreq, vfreq);

						gos [treeToDistract].GetComponent<WaterTreeScript> ().SetColors (Globals.distColor1, Globals.distColor2);
						gos [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
					} else {
						gos [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (4, 4);
						if (r2 < 0.5) {
							gos [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (4, 1);
						} else {
							gos [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (1, 4);
						}
					}
					Debug.Log("Ori: [0, 0.5, 1] - " + r2);
				}
				locx = gos[treeToActivate].transform.position.x;
				hfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
				vfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();
			} else if (gos.Length == 4) {
				float thresh0 = 0.25F;
				float thresh1 = 0.5F;
				float thresh2 = 0.75F;

				if (Globals.biasCorrection && Globals.numberOfTrials > 1) {
					// Turn on bias correction after testing that logic works!
					// Bias correction algo #1
					float tf0 = Globals.GetTurnBias(20, 0);
					float tf1 = Globals.GetTurnBias(20, 1);
					float tf2 = Globals.GetTurnBias(20, 2);
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

				Debug.Log ("random: " + r + " --- range: [0, " + thresh0 + ", " + thresh1 + ", " + thresh2 + ", 1]");

				treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : r < thresh2 ? 2 : 3;

				locx = gos[treeToActivate].transform.position.x;
				hfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
				vfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();
				SetupTreeActivation (gos, treeToActivate, 4);
			}
        } else if (Globals.gameType.Equals("det_blind")) {
			if (gos.Length == 3) {
				double thresh0 = 0.333D;
				double thresh1 = 0.666D;

				if (Globals.biasCorrection && Globals.numberOfTrials > 1) {
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
				}

				Debug.Log ("STIMLOC: [0, " + thresh0 + ", " + thresh1 + ", 1] - " + r);
				treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : 2;
				SetupTreeActivation (gos, treeToActivate, 2);

				locx = gos [treeToActivate].transform.position.x;
				hfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
				vfreq = gos [treeToActivate].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
			} else if (gos.Length == 2) { // For training lesioned animals who have not been previously trained
				float rThresh0 = 0.5F;
				if (Globals.presoRatio > 0) { // Works for 2-choice only, YN and 2AFC
					rThresh0 = (float)Globals.presoRatio / (Globals.presoRatio + 1);
				}
				if (Globals.biasCorrection && Globals.numberOfTrials > 1) {
					rThresh0 = 1 - Globals.GetTurnBias(20, 0);  // varies the boundary based on history of mouse turns
				}
				Debug.Log("Loc: [0, " + rThresh0 + ", 1] - " + r);
				treeToActivate = r < rThresh0 ? 0 : 1;
				SetupTreeActivation (gos, treeToActivate, 1);

				locx = gos[treeToActivate].transform.position.x;
				hfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
				vfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();
			}
        } else if (Globals.gameType.Equals("discrimination")) {
            // Randomize orientations
			float rThresh0 = 0.5F;
			if (Globals.numberOfTrials > 1) {  // Add bias correction option if needed later
				rThresh0 = 1 - Globals.GetTurnBias (20, 0);
			}
            if (r < rThresh0) {
                gos[0].GetComponent<WaterTreeScript>().SetShader(Globals.rewardedHFreq, Globals.rewardedVFreq);
                gos[1].GetComponent<WaterTreeScript>().SetShader(Globals.distractorHFreq, Globals.distractorVFreq);
                locx = gos[0].transform.position.x;
                hfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
                vfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(true);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(false);
            } else {
				gos[0].GetComponent<WaterTreeScript>().SetShader(Globals.distractorHFreq, Globals.distractorVFreq);
                gos[1].GetComponent<WaterTreeScript>().SetShader(Globals.rewardedHFreq, Globals.rewardedVFreq);
                locx = gos[1].transform.position.x;
                hfreq = gos[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
                vfreq = gos[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(false);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(true);
            }
            Debug.Log("[0, " + rThresh0 + ", 1] - " + r);
        } else if (Globals.gameType.Equals("match") || Globals.gameType.Equals("nonmatch")) {
            // First, pick an orientation at random for the central tree
            float targetHFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
            float targetVFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderVFreq();

			if (r < 0.5)  { // Switch target to opposite of previous
                gos[2].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
            }
            // Second, randomly pick which side the matching orientation is on
            float rSide = UnityEngine.Random.value;
            targetHFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
            targetVFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderVFreq();
            // Try to balance location of match based on the turning bias
			float rThresh0 = 0.5F;
			if (Globals.numberOfTrials > 1) {  // Add bias correction option if needed later
				rThresh0 = 1 - Globals.GetTurnBias (20, 0);
			}
			if (rSide < rThresh0)  { // Set the left tree to be the rewarded side
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(true);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(false);
                locx = gos[0].transform.position.x;
                if (Globals.gameType.Equals("match")) {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    hfreq = targetHFreq;
                    vfreq = targetVFreq;
                } else {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    hfreq = targetVFreq;
                    vfreq = targetHFreq;
                }
			} else { // Set the right tree to match
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(false);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(true);
                locx = gos[1].transform.position.x;
                if (Globals.gameType.Equals("match")) {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    hfreq = targetHFreq;
                    vfreq = targetVFreq;
                } else {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    hfreq = targetVFreq;
                    vfreq = targetHFreq;
                }
            }
            Debug.Log("[0, " + rThresh0 + ", 1] - " + rSide);
        }

		// NO bias correction with FOV location yet, but may need to add later
		if (Globals.perim && (((gos.Length == 3 || gos.Length == 2) && locx != Globals.worldXCenter) || gos.Length == 4)) {  // perimetry is enabled, so pick from the set of random windows to use
			int rFOV = UnityEngine.Random.Range (0, Globals.fovsForPerimScaleInclusive [Globals.perimScale]);
			Debug.Log ("FOV: " + rFOV);
			Globals.SetOccluders (locx, rFOV);
		} else {
			Globals.SetOccluders(locx);
			Debug.Log ("no dynamic occlusion");
		}

		// Optogenetics
		if (Globals.optoSide != -1) {  // A side for optogenetics was specified
			float rOpto = UnityEngine.Random.value;
			if (rOpto < Globals.optoFraction) {
				udpSender.GetComponent<UDPSend>().OptoTurnOn(Globals.optoSide);
				Globals.optoOn = 1; // Used for logging
			}
		}

        Globals.targetLoc.Add(locx);
        Globals.targetHFreq.Add(hfreq);
        Globals.targetVFreq.Add(vfreq);

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

		Globals.hasNotTurned = true;

        Globals.trialStartTime.Add(DateTime.Now.TimeOfDay);
		// Update again after the pause, as the world might have changed between trials
		this.lastAccuracyText.text = "Last 20 accuracy (in this world): " + Math.Round(Globals.GetLastAccuracy(20) * 100) + "%";
        this.state = "Running";
	}

    private void SetupTreeActivation(GameObject[] gos, int treeToActivate, int maxTrees)
    {
        for (int i = 0; i < maxTrees; i++)  // In the 3-tree case, never deactivate the 3rd tree
        {
            gos[i].SetActive(true);
            if (i == treeToActivate)
                gos[i].GetComponent<WaterTreeScript>().Show();
            else
                gos[i].GetComponent<WaterTreeScript>().Hide();
        }
    }

    private void GameOver()
    {
		// Disable opto lights, if on
		udpSender.GetComponent<UDPSend>().OptoTurnOffAll();

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
    }

    private IEnumerator CheckForQ()
    {
        Debug.Log("Waiting for Q");
        yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Q));
        Debug.Log("quitting!");
		this.udpSender.close();
        Globals.WriteStatsFile();
		bool wroteData = Globals.WriteStatsToGoogleSheet();
		if (wroteData) {
			Application.Quit ();
		} else {
			this.fadeToBlackText.text = "Data not saved in sheets, so manually enter";
			this.state = "NotSavedToSheets";
			yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Q));
			Application.Quit ();
		}
    }

    private void MovePlayer()
    {
		
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
				this.last5Mouse1Y.Enqueue (Globals.sphereInput.mouse1Y);
				this.last5Mouse2Y.Enqueue (Globals.sphereInput.mouse1X);
			}

		
			// transform sphere data into unity movement
			//if (this.frameCounter - this.previousFrameCounter > 1)
			//print("lost packets: " + this.frameCounter + "/" + this.previousFrameCounter);
			this.previousFrameCounter = this.frameCounter;

			this.player.transform.Rotate(Vector3.up, Mathf.Rad2Deg * (this.last5Mouse1Y.Average()) / this.rawRotationDivider);
            
			Vector3 rel = this.player.transform.forward * (this.last5Mouse2Y.Average () / this.rawSpeedDivider);
			//this.player.transform.position = this.player.transform.position + rel;
			this.characterController.Move (rel);
			this.udpSender.SendMousePos (this.player.transform.position);
			this.udpSender.SendMouseRot (this.player.transform.rotation.eulerAngles.y);

			//Debug.Log (this.last5Mouse2Y.Average ());
			//Debug.Log (Time.time * 1000);

			// Send UDP msg out
			//this.udpSender.SendPlayerState(this.player.transform.position, this.player.transform.rotation.eulerAngles.y, Globals.playerInWaterTree, Globals.playerInDryTree);
		} else {
			//Debug.Log ("no new data");
		}
    }

    public void FlushWater()
    {
        this.udpSender.FlushWater();
    }

	private void updateTrialsText() {
		this.numberOfTrialsText.text = "Trial: #" + Globals.numberOfTrials.ToString ();
	}

	private void updateRewardAmountText() {
		this.rewardAmountText.text = "Reward: " + Math.Round(Globals.rewardAmountSoFar).ToString() + " of " + Math.Round(Globals.totalRewardSize) + " ul";
	}

	private void updateCorrectTurnsText() {
		this.numberOfCorrectTurnsText.text = "Correct: " +
			Globals.numCorrectTurns.ToString ()
			+ " (" +
			Mathf.Round(((float)Globals.numCorrectTurns / ((float)Globals.numberOfTrials -1)) * 100).ToString() + "%" 
			+ Globals.GetTreeAccuracy() + ")";
	}

}