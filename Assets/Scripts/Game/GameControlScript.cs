using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Text;
using System.Collections.Generic;

public class GameControlScript : MonoBehaviour
{
    public int runDuration;
    public int numberOfRuns;
    public GameObject player;
    public GameObject menuPanel;
    public Image fadeToBlack;
    public Text fadeToBlackText;
    public Text numberOfRewardsText;
	public Text numberOfDryTreesText;
    public UDPSend udpSender;
    public MovementRecorder movementRecorder;

	private float rawSpeedDivider;  // Normally 60f; previously 200f
	private float rawRotationDivider;  // Normally 500f; previously -4000f
    private int respawnAmplitude = 2000;
    private int runNumber;
    private float runTime;
    private long frameCounter, previousFrameCounter;
    private System.Collections.Generic.Queue<float> last5Mouse2Y, last5Mouse1Y;
    private string state;
    private bool firstFrameRun;
    private bool playerInWaterTree, playerInDryTree;
    private Loader scenarioLoader;
    private CharacterController characterController;
    private DebugControl debugControlScript;
    private bool timeoutState;


	private int smoothingWindow = 1;  // Amount to smoothen the player movement
	private int trialDelay = 2;  // seconds to wait before starting next trial
	private bool waitedOneFrame = false;  // When mouse hits tree, need to wait a few frames before it turns black, and then pause the game

	private Vector3 startingPos;
	private Quaternion startingRot;

    // Use this for initialization
    void Start()
    {
        this.frameCounter = this.previousFrameCounter = 0;
        this.runNumber = 1;
		this.last5Mouse1Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
		this.last5Mouse2Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
        this.state = "LoadScenario";
        this.firstFrameRun = false;
        this.scenarioLoader = GameObject.FindGameObjectWithTag("generator").GetComponent<Loader>();
        this.characterController = GameObject.Find("FPSController").GetComponent<CharacterController>();
        this.debugControlScript = GameObject.Find("FPSController").GetComponent<DebugControl>();
        Globals.numberOfRewards = 0;
		Globals.numberOfDryTrees = 0;
        this.timeoutState = false;

		this.startingPos = this.player.transform.position;
		this.startingRot = this.player.transform.rotation;

        init();
    }

    // Update is called once per frame
    void Update()
    {

		Debug.Log ("Framerate: " + 1.0f / Time.deltaTime);
        CatchKeyStrokes();

		
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
                ResetScenario();
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

        XmlNodeList udpConfigList = xmlDoc.SelectNodes("document/config");

        string _runDuration = "";
        string _numberOfRuns = "";
		string _rawSpeedDivider = "";
		string _rawRotationDivider = "";

        foreach (XmlNode xn in udpConfigList)
        {
			_runDuration = xn["runDuration"].InnerText;
			_numberOfRuns = xn["numberOfRuns"].InnerText;
			_rawSpeedDivider = xn["rawSpeedDivider"].InnerText;
			_rawRotationDivider = xn["rawRotationDivider"].InnerText;
        }

        int.TryParse(_runDuration, out this.runDuration);
        int.TryParse(_numberOfRuns, out this.numberOfRuns);
		float.TryParse(_rawSpeedDivider, out this.rawSpeedDivider);
		float.TryParse(_rawRotationDivider, out this.rawRotationDivider);

        // trying to avoid first drops of water
        this.udpSender.ForceStopSolenoid();
    }

    private void CatchKeyStrokes()
    {
        if (Input.GetKey(KeyCode.Escape))
            this.state = "GameOver";

        if (Input.GetKeyUp(KeyCode.U))
            //this.udpSender.SingleDrop();
			this.udpSender.SendWaterReward(1);
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
		Debug.Log ("In StartGame()");
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;
        this.fadeToBlackText.text = "Press SPACE to start";
		Debug.Log ("waiting for space bar");
        if (Input.GetKeyUp(KeyCode.Space))
        {
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
            this.state = "Running";
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
        if( this.firstFrameRun )
        {
            this.udpSender.SendRunSync();
            this.firstFrameRun = false;
        }

        if( Globals.playerInDryTree && !Globals.timeoutState )
        {
            this.state = "Timeout";
        }

		//Debug.Log ("starting move");
        MovePlayer();
		//Debug.Log ("move complete");
		if (this.udpSender.CheckReward ())
			this.movementRecorder.logReward(false, true);
		//this.movementRecorder.logReward(this.udpSender.CheckReward());
		//this.movementRecorder.logReward(true);
        this.numberOfRewardsText.text = "Number of rewards: " + Globals.numberOfRewards.ToString();
		this.numberOfDryTreesText.text = "Number of dry trees entered: " + Globals.numberOfDryTrees.ToString();
        //this.frameCounter++;
		//Debug.Log ("screen updated");
        if (Time.time - this.runTime >= this.runDuration)
        {
            // fadetoblack + respawn
            this.movementRecorder.SetFileSet(false);
            this.fadeToBlack.gameObject.SetActive(true);
            this.state = "Fading";
        }
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

	private void Pause()
	{
		if (waitedOneFrame) {
			System.Threading.Thread.Sleep (trialDelay * 1000);
			waitedOneFrame = false;
			if (this.runNumber > this.numberOfRuns)
				this.state = "GameOver";
			else
				this.state = "Respawn";
		} else {
			waitedOneFrame = true;
		}
	}

    /*
     * Reset all trees
     * */
    public void ResetScenario()
    {      
        this.runTime = Time.time;
        this.runNumber++;

        print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
        foreach (WaterTreeScript script in GameObject.Find("Trees").GetComponentsInChildren<WaterTreeScript>())
        {
            script.Refill();
        }
        print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
        this.debugControlScript.enabled = false;

		// NB edit (1 line)
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = Color.black;
		this.state = "Paused";

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
		Debug.Log ("Respawning");

		// NB edit - commented out to teleport mouse back to the beginning
        //int x = 20000 - Random.Range(-1 * this.respawnAmplitude, this.respawnAmplitude);
        //int z = 20000 - Random.Range(-1 * this.respawnAmplitude, this.respawnAmplitude);
        //float rot = Random.Range(0f, 360f);
        
		//this.player.transform.position = new Vector3(x, this.player.transform.position.y, z);
        //this.player.transform.Rotate(Vector3.up, rot);

		// NB edit - The code below comes from starting the game - ideally make a helper function?
		this.player.transform.position = this.startingPos;
		this.player.transform.rotation = this.startingRot;
        //this.state = "StartGame";

		// Randomly alternate which tree is visible
		float r = Random.value;
		Debug.Log ("random tree value = " + r.ToString ());
		int numWaterTrees = 0;
		// There should be a better way of counting trees, but I don't know it :(
		List<GameObject> gos = new List<GameObject>();
		foreach (WaterTreeScript script in GameObject.Find("Trees").GetComponentsInChildren<WaterTreeScript>(true)) {
			gos.Add(script.gameObject);
		}
		Debug.Log ("Num trees = " + gos.Count.ToString ());
		if (gos.Count == 2) {
			int treeToActivate;
			if (r < 0.5) {
				treeToActivate = 0;
			} else {
				treeToActivate = 1;
			}
			for (int i = 0; i < gos.Count; i++) {
				if (i == treeToActivate) {
					gos [i].SetActive (true);
					Debug.Log ("Activated tree id = " + i.ToString ());
				} else {
					gos [i].SetActive (false);
					Debug.Log ("Inactivated tree id = " + i.ToString ());
				}
			}
		}


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
		this.state = "Running";
	}
    
    private void GameOver()
    {
		Debug.Log ("In GameOver()");
        this.fadeToBlack.gameObject.SetActive(true);
        this.fadeToBlack.color = Color.black;
        this.fadeToBlackText.text = "GAME OVER MUSCULUS!";
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    private void MovePlayer()
    {
		
		if (Globals.newData) {
			Globals.newData = false;

			// Keep a buffer of the last 5 movement deltas to smoothen some movement
			if (this.last5Mouse2Y.Count == smoothingWindow)
				this.last5Mouse2Y.Dequeue ();
			if (this.last5Mouse1Y.Count == smoothingWindow)
				this.last5Mouse1Y.Dequeue ();

			this.last5Mouse1Y.Enqueue (Globals.sphereInput.mouse1X);  // nikhil changed to use yaw rather than roll
			this.last5Mouse2Y.Enqueue (Globals.sphereInput.mouse2Y);
		
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
}