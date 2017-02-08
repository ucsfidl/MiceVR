using UnityEngine;
using System.Collections;

public class IntroScript : MonoBehaviour {

	public void LoadGenerator()
    {
        Application.LoadLevel("IntroGenerator");
    }

    public void LoadGame()
    {
        Application.LoadLevel("IntroGame");
    }

    public void LoadReplay()
    {
        Application.LoadLevel("IntroReplay");
    }

	public void LoadConfiguration()
	{
		Application.LoadLevel("config");
	}

    public void Exit()
    {
        Application.Quit();
    }
}
