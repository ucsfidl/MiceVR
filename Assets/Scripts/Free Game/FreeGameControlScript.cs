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
	private bool waitedOneFrame = false;  // When mouse hits tree, need to wait a few frames before it turns black, and then pause the game

	private Vector3 startingPos;
	private Quaternion startingRot;
	private Vector3 prevPos;

	private int centralViewVisible;

	private DateTime lastRewardTime;
	private int minRewardInterval = 1000;

	private bool correctTrial = true;

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
		FreeGlobals.numberOfTrials = 1;  // Start on first trial
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
            if (Input.GetKeyUp(KeyCode.U))
            {
				if (FreeGlobals.gameType.Equals ("nosepoke") || FreeGlobals.gameType.Equals ("free_det")) {			
					int dur = FreeGlobals.freeRewardDur [0];
					ard.sendReward (FreeGlobals.freeRewardSite[0], dur);
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add(rSize);
					FreeGlobals.rewardAmountSoFar += rSize;
				}
            }
			else if (Input.GetKeyUp(KeyCode.Y))
			{
				if (FreeGlobals.gameType.Equals ("free_det")) {			
					int dur = FreeGlobals.freeRewardDur [2];
					ard.sendReward (FreeGlobals.freeRewardSite[1], dur);
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add(rSize);
					FreeGlobals.rewardAmountSoFar += rSize;
				}
			}
			else if (Input.GetKeyUp(KeyCode.I))
			{
				if (FreeGlobals.gameType.Equals ("free_det")) {			
					int dur = FreeGlobals.freeRewardDur [3];
					ard.sendReward (FreeGlobals.freeRewardSite[2], dur);
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add(rSize);
					FreeGlobals.rewardAmountSoFar += rSize;
				}
			}
			else if (Input.GetKeyUp(KeyCode.T))
            {
                // Mouse is stuck so teleport to beginning
                TeleportToBeginning();
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
			if (rs == FreeGlobals.freeRewardSite [0] || rs == FreeGlobals.freeRewardSite[1] || rs == FreeGlobals.freeRewardSite[2]) {
				if (DateTime.Now.Subtract (lastRewardTime).TotalMilliseconds > minRewardInterval) {				
					int dur = FreeGlobals.rewardDur;
					ard.sendReward (rs, dur);
					lastRewardTime = DateTime.Now;
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add(rSize);
					FreeGlobals.rewardAmountSoFar += rSize;
				}
			}
		} else if (FreeGlobals.gameType.Equals ("free_det")) {
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
			switch (FreeGlobals.freeState) 
			{

			case "loaded":  // Mouse has not yet poked his nose in
				if (rs == FreeGlobals.freeRewardSite [0]) {
					int dur = FreeGlobals.freeRewardDur [rs/2];
					ard.sendReward (rs, dur);
					lastRewardTime = DateTime.Now;
					float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
					FreeGlobals.sizeOfRewardGiven.Add (rSize);
					FreeGlobals.rewardAmountSoFar += rSize;

					float r = UnityEngine.Random.value;
					float rThresh0 = 1 - FreeGlobals.GetTurnBias(20, 0);  // varies the boundary based on history of mouse turns
					Debug.Log("[0, " + rThresh0 + ", 1] - " + r);
					int treeToActivate = r < rThresh0 ? 0 : 1;
						
					SetupTreeActivation (gos, treeToActivate, 2);
					FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
					FreeGlobals.targetHFreq.Add(gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq());
					FreeGlobals.targetVFreq.Add(gos[treeToActivate].gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());

					FreeGlobals.freeState = "nosepoke";
					FreeGlobals.trialStartTime.Add(DateTime.Now.TimeOfDay);
				}
				break;
				
			case "nosepoke":  // Mouse has poked his nose in, so only reward him if he goes to the correct lickport
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
					FreeGlobals.freeState = "loaded";

					if (correctTrial) {
						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
					}
					FreeGlobals.firstTurn.Add (gos [rs / 2 - 1].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [rs / 2 - 1].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());

					FreeGlobals.numberOfTrials++;
					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);
					FreeGlobals.WriteToLogFiles ();

					correctTrial = true;
				} else if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2])  {
					correctTrial = false;
					Debug.Log ("incorrect");
				}
				break;
			} 
		} else if (FreeGlobals.gameType.Equals ("free_det_const")) {  // constrained - forced to learn, won't get reward otherwise
			int rs = ard.CheckForMouseAction ();
			GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
			switch (FreeGlobals.freeState) 
			{

			case "loaded":  // Mouse has not yet poked his nose in
				if (rs == FreeGlobals.freeRewardSite [0]) {
					float r = UnityEngine.Random.value;
					float rThresh0 = 1 - FreeGlobals.GetTurnBias(20, 0);  // varies the boundary based on history of mouse turns
					Debug.Log("[0, " + rThresh0 + ", 1] - " + r);
					int treeToActivate = r < rThresh0 ? 0 : 1;

					SetupTreeActivation (gos, treeToActivate, 2);
					FreeGlobals.targetLoc.Add (gos [treeToActivate].transform.position.x);
					FreeGlobals.targetHFreq.Add(gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq());
					FreeGlobals.targetVFreq.Add(gos[treeToActivate].gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());

					FreeGlobals.freeState = "stim_on";
					FreeGlobals.trialStartTime.Add(DateTime.Now.TimeOfDay);
				}
				break;

			case "stim_on":  // Mouse has poked his nose in, so only reward him if he goes to the correct lickport
				if (rs == FreeGlobals.freeRewardSite [1] || rs == FreeGlobals.freeRewardSite [2]) { // licked at 1 of 2 lick ports
					FreeGlobals.firstTurn.Add (gos [rs / 2 - 1].transform.position.x);
					FreeGlobals.firstTurnHFreq.Add (gos [rs / 2 - 1].GetComponent<WaterTreeScript> ().GetShaderHFreq ());
					FreeGlobals.firstTurnVFreq.Add (gos [rs / 2 - 1].gameObject.GetComponent<WaterTreeScript> ().GetShaderVFreq ());

					FreeGlobals.numberOfTrials++;
					FreeGlobals.trialEndTime.Add (DateTime.Now.TimeOfDay);
					FreeGlobals.WriteToLogFiles ();

					SetupTreeActivation (gos, -1, 2); // Hide all trees 
					FreeGlobals.freeState = "loaded";

					if ((FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [0].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [1]) || // left tree is on and the mouse licked the lickport there
					    (FreeGlobals.targetLoc [FreeGlobals.targetLoc.Count - 1].Equals (gos [1].transform.position.x) &&
					    rs == FreeGlobals.freeRewardSite [2])) { // Right tree is on and the mouse licked the lickport there

						int dur = FreeGlobals.freeRewardDur [rs / 2];
						ard.sendReward (rs, dur);
						float rSize = FreeGlobals.rewardSize / FreeGlobals.rewardDur * dur;
						FreeGlobals.sizeOfRewardGiven.Add (rSize);
						FreeGlobals.rewardAmountSoFar += rSize;

						FreeGlobals.numCorrectTurns++;
						Debug.Log ("correct");
					}
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
				Mathf.Round(((float)FreeGlobals.numCorrectTurns / ((float)FreeGlobals.numberOfTrials - 1)) * 100).ToString() + "%" 
				+ FreeGlobals.GetTreeAccuracy() + ")";
			this.lastAccuracyText.text = "Last 20 accuracy: " + Math.Round(FreeGlobals.GetLastAccuracy(20) * 100) + "%";
		}
		//this.frameCounter++;
		//Debug.Log ("screen updated");

        TimeSpan te = DateTime.Now.Subtract(FreeGlobals.gameStartTime);
        this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}", te.Hours * 60 + te.Minutes, te.Seconds);
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
                Mathf.Round(((float)FreeGlobals.numCorrectTurns / ((float)FreeGlobals.numberOfTrials - 1)) * 100).ToString() + "%" 
                + FreeGlobals.GetTreeAccuracy() + ")";
            this.lastAccuracyText.text = "Last 20 accuracy: " + Math.Round(FreeGlobals.GetLastAccuracy(20) * 100) + "%";
        }

        // NB Hack to get screen to go black before pausing for trialDelay

        if (waitedOneFrame) {
			System.Threading.Thread.Sleep (FreeGlobals.trialDelay * 1000);
			waitedOneFrame = false;

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
            // Append to stats file here
            /* NB: removed as we want the mouse to run for a certain number of rewards, not trials?
			if (this.runNumber > this.numberOfRuns)
				this.state = "GameOver";
			else
				this.state = "Respawn";
				*/
        }
        else {
			waitedOneFrame = true;
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
        TeleportToBeginning();
        //this.state = "StartGame";

		// Randomly decide which of the 2 trees is visible, only if the scenario has only 2 trees.
		GameObject[] gos = GameObject.FindGameObjectsWithTag("water");

        float locx = gos[0].transform.position.x;
        float hfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
        float vfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderVFreq();

        int treeToActivate = 0;
        float r = UnityEngine.Random.value;
        if (FreeGlobals.gameType.Equals("detection") || FreeGlobals.gameType.Equals("det_target"))
        {
            if (gos.Length == 1)  // Linear track
            {
                if (FreeGlobals.varyOrientation)
                {
                    if (r > 0.5)  // Half the time, swap the orientation of the tree
                    {
                        gos[0].GetComponent<WaterTreeScript>().SetShader(vfreq, hfreq);
                        hfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
                        vfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
                    }
                }
            }
            else if (gos.Length == 2 || FreeGlobals.gameType.Equals("det_target"))
            {
                float rThresh0 = 1 - FreeGlobals.GetTurnBias(20, 0);  // varies the boundary based on history of mouse turns
                Debug.Log("[0, " + rThresh0 + ", 1] - " + r);
                treeToActivate = r < rThresh0 ? 0 : 1;
                SetupTreeActivation(gos, treeToActivate, 2);
                locx = gos[treeToActivate].transform.position.x;
                hfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
                vfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();
            }
        } 
        else if (FreeGlobals.gameType.Equals("det_blind"))
        {
            float tb0 = FreeGlobals.GetTurnBias(20, 0);
            float tb1 = FreeGlobals.GetTurnBias(20, 1);

            float t0 = 1 - tb0;
            float t1 = 1 - tb1;
            float t2 = tb0 + tb1;

            float thresh0 = t0 / (t0 + t1 + t2);
            float thresh1 = t1 / (t0 + t1 + t2) + thresh0;

            Debug.Log("[0, " + thresh0 + ", " + thresh1 + ", 1] - " + r);
            treeToActivate = r < thresh0 ? 0 : r < thresh1 ? 1 : 2;
            SetupTreeActivation(gos, treeToActivate, 2);
            locx = gos[treeToActivate].transform.position.x;
            hfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
            vfreq = gos[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();
        }
        else if (FreeGlobals.gameType.Equals("discrimination"))
        {
            // Randomize orientations
            float rThresh0 = 1 - FreeGlobals.GetTurnBias(20, 0);
            if (r < rThresh0) 
            {
                gos[0].GetComponent<WaterTreeScript>().SetShader(FreeGlobals.rewardedHFreq, FreeGlobals.rewardedVFreq);
                gos[1].GetComponent<WaterTreeScript>().SetShader(FreeGlobals.rewardedVFreq, FreeGlobals.rewardedHFreq);
                locx = gos[0].transform.position.x;
                hfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
                vfreq = gos[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(true);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(false);
            }
            else
            {
                gos[0].GetComponent<WaterTreeScript>().SetShader(FreeGlobals.rewardedVFreq, FreeGlobals.rewardedHFreq);
                gos[1].GetComponent<WaterTreeScript>().SetShader(FreeGlobals.rewardedHFreq, FreeGlobals.rewardedVFreq);
                locx = gos[1].transform.position.x;
                hfreq = gos[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
                vfreq = gos[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(false);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(true);
            }
            Debug.Log("[0, " + rThresh0 + ", 1] - " + r);
        }
        else if (FreeGlobals.gameType.Equals("match") || FreeGlobals.gameType.Equals("nonmatch"))
        {
            // First, pick an orientation at random for the central tree
            float targetHFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
            float targetVFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderVFreq();

            if (r < 0.5)  // Switch target to opposite of previous
            {
                gos[2].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
            }
            // Second, randomly pick which side the matching orientation is on
            float rSide = UnityEngine.Random.value;
            targetHFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
            targetVFreq = gos[2].GetComponent<WaterTreeScript>().GetShaderVFreq();
            // Try to balance location of match based on the turning bias
            float rThresh0 = 1 - FreeGlobals.GetTurnBias(20, 0);  // recent bias to the left side
            if (rSide < rThresh0)  // Set the left tree to be the rewarded side
            {
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(true);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(false);
                locx = gos[0].transform.position.x;
                if (FreeGlobals.gameType.Equals("match"))
                {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    hfreq = targetHFreq;
                    vfreq = targetVFreq;
                }
                else
                {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    hfreq = targetVFreq;
                    vfreq = targetHFreq;
                }
            }
            else // Set the right tree to match
            {
                gos[0].GetComponent<WaterTreeScript>().SetCorrect(false);
                gos[1].GetComponent<WaterTreeScript>().SetCorrect(true);
                locx = gos[1].transform.position.x;
                if (FreeGlobals.gameType.Equals("match"))
                {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    hfreq = targetHFreq;
                    vfreq = targetVFreq;
                }
                else
                {
                    gos[0].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    gos[1].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    hfreq = targetVFreq;
                    vfreq = targetHFreq;
                }
            }
            Debug.Log("[0, " + rThresh0 + ", 1] - " + rSide);

        }

        OccludeTree(locx);

        FreeGlobals.targetLoc.Add(locx);
        FreeGlobals.targetHFreq.Add(hfreq);
        FreeGlobals.targetVFreq.Add(vfreq);

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