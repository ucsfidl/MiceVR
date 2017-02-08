using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ConfigScript : MonoBehaviour {

    public Text t;

    private string configFolder, scenarioFolder, replayFolder, textureFolder;

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    public void SetConfigFolder( string s )
    {
        this.configFolder = s;
    }

    public void SetScenarioFolder(string s)
    {
        this.scenarioFolder = s;
    }

    public void SetReplayFolder(string s)
    {
        this.replayFolder = s;
    }

    public void SetTextureFolder(string s)
    {
        this.textureFolder = s;
    }

    public void LoadValues()
    {
        PlayerPrefs.SetString("configFolder", this.configFolder);
        PlayerPrefs.SetString("scenarioFolder", this.scenarioFolder);
        PlayerPrefs.SetString("replayFolder", this.replayFolder);
        PlayerPrefs.SetString("textureFolder", this.textureFolder);

        t.text = "Done ! - Press ESC to Quit";
    }
}
