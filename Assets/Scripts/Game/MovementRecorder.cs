using UnityEngine;
using System.Collections;
using System.IO;

public class MovementRecorder : MonoBehaviour {

    private StreamWriter outfile, rewardFile;
    private string replayFileName;
    private bool fileSet;
	private int licks, rewards, lastLick, lastReward;

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

    public void SetReplayFileName(string s)
    {
        this.replayFileName = s;
    }

    public string GetReplayFileName()
    {
        return this.replayFileName;
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