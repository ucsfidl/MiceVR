using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;


public class MovementRecorder : MonoBehaviour {

    private StreamWriter outfile, rewardFile;
    private string mouseName;
    private string maxReward;  // in ul
    private string dayName;
    private string scenarioName;
    private string sessionName;
	private string degCentrallyVisible;
    private string replayFileName;
    private bool fileSet;
	private int licks, rewards, lastLick, lastReward;

    public Text errorText;

	// Use this for initialization
	void Start () {
        this.fileSet = false;
        this.replayFileName = "";
		licks = 0;
		rewards = 0;
		lastLick = -1;
		lastReward = -1;
	}
	
	// Update is called once per frame
	void Update () {
        if (this.fileSet)
        {
            outfile.Write(this.transform.position.x + "," +
                this.transform.position.y + "," +
                this.transform.position.z + ";" +
                this.transform.rotation.eulerAngles.y +
                System.Environment.NewLine);
			
				rewardFile.Write ("Rewards:" + rewards + ";Licks:" + licks + ";" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute + "-" + System.DateTime.Now.Second
					+System.Environment.NewLine);
        }
	}

	public void logReward (bool reward, bool lick) {
		if(reward)
			rewards+=1;
		if(lick)
			licks+=1;
	}

    public void SetMouseName(string s)
    {
        this.mouseName = s;
        MakeReplayName();
    }

    public void SetMaxReward(string s)
    {
        this.maxReward = s;
        float.TryParse(this.maxReward, out Globals.totalRewardSize);
        Debug.Log(Globals.totalRewardSize);
    }

    public void SetDayName(string s)
    {
        this.dayName = s;
        MakeReplayName();
    }

	public void SetSessionName (string s)
	{
		this.sessionName = s;
		MakeReplayName();
	}

	public void SetDegCentrallyVisible(string s)
	{
		int deg = 30;
		if (!s.Equals ("")) {
			this.degCentrallyVisible = s;
			int.TryParse (this.degCentrallyVisible, out deg);
		}
		Globals.SetCentrallyVisible(deg);
		Debug.Log(Globals.centralViewVisibleShift);
	}

    public void SetScenarioName(string s)
    {
        if (s.EndsWith(".xml"))
            this.scenarioName = s.Substring(0, s.Length - 4);
        else
            this.scenarioName = s;
        //Debug.Log(this.scenarioName);
        MakeReplayName();
    }

    
    public void SetReplayFileName(string s)
    {
        this.replayFileName = s;
    }

    public string GetReplayFileName()
    {
        return this.replayFileName;
    }

    private void MakeReplayName()
    {
        this.replayFileName = this.mouseName + "-D" + this.dayName + "-" + this.scenarioName + "-S" + this.sessionName;
        if (File.Exists(PlayerPrefs.GetString("replayFolder") + "/" + this.replayFileName + "_actions.txt"))
            this.errorText.text = "ERROR: File for this mouse already exists!  Results will be overwritten if you proceed.";
        else
            this.errorText.text = "";
    }

    public void SetFileSet(bool b)
    {
        this.fileSet = b;
    }

    public void SetRun(int run)
    {
        string fn = this.replayFileName;
		fn += "-" + System.DateTime.Now.Year + "-" + System.DateTime.Now.Month + "-" + System.DateTime.Now.Day +
			"-" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute + "-" + System.DateTime.Now.Second;
		if (outfile != null) {
			outfile.Close ();
			rewardFile.Close ();
		}
        if( this.replayFileName.IndexOf('.') > 0 )
        {
            string[] tmp = this.replayFileName.Split('.');
            tmp[0] += "-" + run;
            fn = tmp[0] + "." + tmp[1];
        }
        else
        {
            fn += "-" + run;
        }

        outfile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + fn);
		rewardFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/rew_" + fn);
    }

}