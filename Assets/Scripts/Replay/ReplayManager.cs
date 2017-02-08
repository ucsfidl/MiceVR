using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Ports;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour {

    public Text errorText;

    private StreamReader reader;
    private bool loaded, playing;
    private Queue moves;
    private object[] movesArray;
    private string replayFile, baseString;
    private int fileCounter;

	// Use this for initialization
	void Start () {
        this.fileCounter = 1;
	}
	
	// Update is called once per frame
	void Update () {
        if (this.playing && this.loaded)
        {
            string line = reader.ReadLine();
            if( line != null )
            {
                string[] tmp = line.Split(';');
                string[] vec = tmp[0].Split(',');
                this.transform.position = new Vector3(float.Parse(vec[0]), float.Parse(vec[1]), float.Parse(vec[2]));
                this.transform.eulerAngles = Vector3.up*System.Convert.ToSingle(tmp[1]);
            }
            else
            {
                if (this.baseString.IndexOf('.') > 0)
                {
                    string s = this.baseString;
                    string[] tmp = s.Split('.');
                    if (File.Exists(PlayerPrefs.GetString("replayFolder") + "/" + tmp[0] + "-" + (this.fileCounter + 1) + "." + tmp[1]))
                    {
                        this.fileCounter++;
                        this.reader = new StreamReader(PlayerPrefs.GetString("replayFolder") + "/" + tmp[0] + "-" + (this.fileCounter + 1) + "." + tmp[1]);
                    }
                    else
                    {
                        print("Done!");
                        Application.LoadLevel("finish");
                    }
                }
                else
                {
                    if (File.Exists(PlayerPrefs.GetString("replayFolder") + "/" + this.baseString + "-" + (this.fileCounter + 1)))
                    {
                        this.fileCounter++;
                        this.reader = new StreamReader(PlayerPrefs.GetString("replayFolder") + "/" + this.baseString + "-" + this.fileCounter);
                    }
                    else
                    {
                        print("Done!");
                        Application.LoadLevel("finish");
                    }

                }
            }
        }
	}

    public void Play()
    {
        this.playing = true;
    }

    public void Pause()
    {
        this.playing = false;
    }

    public void SetReplayFile(string s)
    {
        this.replayFile = s;
        if( this.replayFile.IndexOf('.') > 0)
        {
            string[] tmp = this.replayFile.Split('.');
            string[] tmp2 = tmp[0].Split('-');
            this.baseString = tmp2[0] + "." + tmp[1];
        }
        else
        {
            string[] tmp2 = this.replayFile.Split('-');
            this.baseString = tmp2[0];
        }
    }

    public void LoadReplay()
    {
        if (this.replayFile != "")
        {
            if (File.Exists(PlayerPrefs.GetString("replayFolder") + "/" + this.replayFile))
            {
                this.reader = new StreamReader(PlayerPrefs.GetString("replayFolder") + "/" + this.replayFile);
                this.loaded = true;
            }
            else
                StartCoroutine(ShowError("ERROR: File does not exist"));
        }
        else
            StartCoroutine(ShowError("ERROR: No replay file selected"));
    }

    public void StepForward()
    {
        this.playing = false;
        string line = reader.ReadLine();
        if (line != null)
        {
            string[] tmp = line.Split(';');
            string[] vec = tmp[0].Split(',');
            this.transform.position = new Vector3(float.Parse(vec[0]), float.Parse(vec[1]), float.Parse(vec[2]));
            this.transform.eulerAngles = Vector3.up * System.Convert.ToSingle(tmp[1]);
        }
    }

    public void StepBack()
    {

    }

    private IEnumerator ShowError(string s)
    {
        this.errorText.text = s;
        yield return new WaitForSeconds(2f);
        this.errorText.text = "";

    }
}
