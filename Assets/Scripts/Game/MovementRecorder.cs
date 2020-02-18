using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System.Collections.Generic;

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

	private List<Camera> cameras;


    public Text errorText;

	// Use this for initialization
	void Start () {
        this.fileSet = false;
        this.replayFileName = "";
		licks = 0;
		rewards = 0;
		lastLick = -1;
		lastReward = -1;
		cameras = new List<Camera> ();
		cameras.Add(GameObject.Find ("CameraL").GetComponent<Camera> ());
		cameras.Add (GameObject.Find ("FirstPersonCharacter").GetComponent<Camera> ());
		cameras.Add(GameObject.Find ("CameraR").GetComponent<Camera> ());
	}
	
	// Update is called once per frame
	void Update () {
		if (this.fileSet) {  //  A new filename is specified on each trial
			string mouseLocation = this.transform.position.x + "," +
			                       this.transform.position.y + "," +
						    	   this.transform.position.z;
			string mouseHeading = this.transform.rotation.eulerAngles.y.ToString("n0");

			string target1Left = "";
			string target1Right = "";
			string target2Left = "";
			string target2Right = "";

			Vector3 targetLoc = Globals.targetLoc [Globals.targetLoc.Count - 1];
			GameObject targetGO = Globals.GetTreeGameObjectFromXPos (targetLoc.x);
			GameObject crown = targetGO.GetComponent<WaterTreeScript> ().crown;

			List<Vector3?> screenPointsL = GetMinMaxScreenPointsOnCam (cameras [0], targetLoc, crown);
			List<Vector3?> screenPointsC = GetMinMaxScreenPointsOnCam (cameras [1], targetLoc, crown);
			List<Vector3?> screenPointsR = GetMinMaxScreenPointsOnCam (cameras [2], targetLoc, crown);

			target1Left = GetOneTargetBoundary (screenPointsL, screenPointsC, screenPointsR, 0, cameras[0].pixelWidth).ToString("0");
			target1Right = GetOneTargetBoundary (screenPointsL, screenPointsC, screenPointsR, 1, cameras[0].pixelWidth).ToString("0");

			int worldID = Globals.worldID [Globals.worldID.Count - 1];  // This may not work until GameControlScript has added the new world - ok for now
			string gameType = Globals.GetGameType (worldID);
			if (gameType.Equals ("det_blind")) {
				GameObject[] gos = Globals.GetTrees ();
				GameObject straightTargetGO = gos[2];  // straight tree is always the 3rd target - UPDATE if this changes

				targetLoc = straightTargetGO.transform.position;
				crown = straightTargetGO.GetComponent<WaterTreeScript> ().crown;

				screenPointsL = GetMinMaxScreenPointsOnCam (cameras [0], targetLoc, crown);
				screenPointsC = GetMinMaxScreenPointsOnCam (cameras [1], targetLoc, crown);
				screenPointsR = GetMinMaxScreenPointsOnCam (cameras [2], targetLoc, crown);

				target2Left = GetOneTargetBoundary (screenPointsL, screenPointsC, screenPointsR, 0, cameras[0].pixelWidth).ToString("0");
				target2Right = GetOneTargetBoundary (screenPointsL, screenPointsC, screenPointsR, 1, cameras[0].pixelWidth).ToString("0");
			}

			outfile.Write(mouseLocation + ";" + mouseHeading + ";" + target1Left + "," + target1Right + ";" + target2Left + "," + target2Right + System.Environment.NewLine);
			print (mouseLocation + ";" + mouseHeading + ";" + target1Left + "," + target1Right + ";" + target2Left + "," + target2Right + System.Environment.NewLine);

			// UNCOMMENT if lick detector is reliable
			//rewardFile.Write ("Rewards:" + rewards + ";Licks:" + licks + ";" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute + "-" + System.DateTime.Now.Second
			//	+System.Environment.NewLine);
        }
	}

	private float GetOneTargetBoundary(List<Vector3?> L, List<Vector3?> C, List<Vector3?> R, int idx, int screenWidth) {
		float res;
		if (idx == 0) {  // looking for left boundary
			if (L[idx] != null && ((Vector3)L [idx]).x >= 0 && ((Vector3)L [idx]).x <= screenWidth  && ((Vector3)L[idx]).z >= 0) { // Found left angular boundary of target on left screen
				res = GetAngleFromScreenPoint (((Vector3)L [0]).x, -90, screenWidth);
			} else if (C[idx] != null && ((Vector3)C [idx]).x >= screenWidth && ((Vector3)C [idx]).x <= 2*screenWidth && ((Vector3)C[idx]).z >= 0) {
				res = GetAngleFromScreenPoint (((Vector3)C [0]).x - screenWidth, -30, screenWidth);
			} else if (R[idx] != null && ((Vector3)R [idx]).x >= 2*screenWidth && ((Vector3)R [idx]).x <= 3*screenWidth && ((Vector3)R[idx]).z >= 0) {
				res = GetAngleFromScreenPoint (((Vector3)R [0]).x - 2*screenWidth, 30, screenWidth);
			} else {
				res = -91;  // indicates left border of target is off-screen
			}
		} else {  // Looking for right boundary of target
			if (R[idx] != null && ((Vector3)R [idx]).x >= 2*screenWidth && ((Vector3)R [idx]).x <= 3*screenWidth && ((Vector3)R[idx]).z >= 0) { // Found right angular boundary of target
				res = GetAngleFromScreenPoint (((Vector3)R [idx]).x - 2*screenWidth, 30, screenWidth);
				//print ("Right right boundary found = " + ((Vector3)R [idx]).x + "->" + res);
			} else if (C[idx] != null && ((Vector3)C [idx]).x >= screenWidth && ((Vector3)C [idx]).x <= 2*screenWidth && ((Vector3)C[idx]).z >= 0) {
				res = GetAngleFromScreenPoint (((Vector3)C [idx]).x - screenWidth, -30, screenWidth);
				//print ("Center right boundary found = " + ((Vector3)C [idx]).x + "->" + res);
			} else if (L[idx] != null && ((Vector3)L [idx]).x >= 0 && ((Vector3)L [idx]).x <= screenWidth  && ((Vector3)L[idx]).z >= 0) {
				res = GetAngleFromScreenPoint (((Vector3)L [idx]).x, -90, screenWidth);
				//print ("Left right boundary found = " + ((Vector3)L [idx]).x + "->" + res);
			} else {
				res = 91;  // indicates left border of target is off-screen
			}
		}
		return res;
	}

	private float GetAngleFromScreenPoint(float x, float zero, int screenWidth) {
		//print (screenWidth);
		return zero + (60 * x/screenWidth);
	}

	public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
		return Quaternion.Euler(angles) * (point - pivot) + pivot;
	}

	private float DegreeToRadian(float angleInDeg) {
		return Mathf.PI * angleInDeg / 180;
	}

	private List<Vector3?> GetMinMaxScreenPointsOnCam(Camera cam, Vector3 targetLoc, GameObject crown) {
		// Iterate through all 3 cameras/screens, and keep those coords which are between 0 and 1 for each screen
		List<Vector3?> screenPoints = new List<Vector3?> (2);
		screenPoints.Add (null);
		screenPoints.Add (null);

		// x and z and the changes for finding the 4 courners of the object-aligned bounding box, initially without any rotation
		Vector3 absScale = Globals.GetAbsoluteScale(crown.transform);
		float x = absScale.x / 2;
		float z = absScale.z / 2;
		float hyp = Mathf.Sqrt (x * x + z * z);  // hypotenuse of the triangle made by moving x to the right and z back

		// (1) Find on screen point of UPPER RIGHT corner of target's bounding box (looking down on the target from the sky)
		Vector3 targetCorner = new Vector3(targetLoc.x, targetLoc.y, targetLoc.z);
		//print (targetCorner + " - " + x + " - " + z);
		float theta1 = Mathf.Atan(z/x); // get the non-rotated angle to the upper right corner of the bounding box
		float theta2 = theta1 - DegreeToRadian(crown.transform.rotation.eulerAngles.y);  // only handle rotations about the vertical axis, which is all we do in the game at the moment to the target
		targetCorner.x = targetCorner.x + hyp*Mathf.Cos(theta2);
		targetCorner.z = targetCorner.z + hyp*Mathf.Sin(theta2);
		UpdateMinMaxScreenPointsIfAppropriate (cam, screenPoints, targetCorner);
		//print ("Target corner #1 = " + targetCorner + " - " + cam.WorldToScreenPoint(targetCorner));

		// (2) Find on screen point of LOWER RIGHT corner of target's bounding box
		targetCorner = new Vector3(targetLoc.x, targetLoc.y, targetLoc.z);  // reset location of corner to center
		theta1 = Mathf.Atan(-z/x);
		theta2 = theta1 - DegreeToRadian (crown.transform.rotation.eulerAngles.y);
		targetCorner.x = targetCorner.x + hyp*Mathf.Cos(theta2);
		targetCorner.z = targetCorner.z + hyp*Mathf.Sin(theta2);
		UpdateMinMaxScreenPointsIfAppropriate (cam, screenPoints, targetCorner);
		//print ("Target corner #2 = " + targetCorner + " - " + cam.WorldToScreenPoint(targetCorner));

		// (3) Find on screen point of UPPER LEFT corner of target's bounding box
		targetCorner = new Vector3(targetLoc.x, targetLoc.y, targetLoc.z);  // reset location of corner to center
		theta1 = Mathf.Atan(z/-x);
		theta2 = theta1 - DegreeToRadian (crown.transform.rotation.eulerAngles.y);
		targetCorner.x = targetCorner.x - hyp*Mathf.Cos(theta2);
		targetCorner.z = targetCorner.z - hyp*Mathf.Sin(theta2);
		UpdateMinMaxScreenPointsIfAppropriate (cam, screenPoints, targetCorner);
		//print ("Target corner #3 = " + targetCorner + " - " + cam.WorldToScreenPoint(targetCorner));

		// (4) Find on screen point of LOWER LEFT corner of target's bounding box
		targetCorner = new Vector3(targetLoc.x, targetLoc.y, targetLoc.z);  // reset location of corner to center
		theta1 = Mathf.Atan(-z/-x);
		theta2 = theta1 - DegreeToRadian (crown.transform.rotation.eulerAngles.y);
		targetCorner.x = targetCorner.x - hyp*Mathf.Cos(theta2);
		targetCorner.z = targetCorner.z - hyp*Mathf.Sin(theta2);
		UpdateMinMaxScreenPointsIfAppropriate (cam, screenPoints, targetCorner);
		//print ("Target corner #4 = " + targetCorner + " - " + cam.WorldToScreenPoint(targetCorner));

		/*
		if (screenPoints [0] != null)
			print (((Vector3)screenPoints [0]).ToString ("n2"));
		else
			print ("screenPoints[0] = null");
		if (screenPoints[1] != null)
			print (((Vector3)screenPoints[1]).ToString("n2"));
		else
			print ("screenPoints[1] = null");
		*/
		return screenPoints;
	}

	private void UpdateMinMaxScreenPointsIfAppropriate(Camera cam, List<Vector3?> screenPoints, Vector3 targetCorner) {
		Vector3 tempScreenPoint = cam.WorldToScreenPoint (targetCorner);
		//Debug.Log (tempScreenPoint);
		if (tempScreenPoint.z > 0) {
			if (screenPoints [0] == null) {
				screenPoints [0] = tempScreenPoint;
			} else if (tempScreenPoint.x < ((Vector3)screenPoints[0]).x) {
				Vector3 spCopy = (Vector3)screenPoints [0];
				spCopy.x = tempScreenPoint.x;
				spCopy.y = tempScreenPoint.y;
				spCopy.z = tempScreenPoint.z;
				screenPoints[0] = spCopy;
				//Debug.Log ("updated min");
			}
			if (screenPoints [1] == null) {
				screenPoints [1] = tempScreenPoint;
			} else if (tempScreenPoint.x > ((Vector3)screenPoints [1]).x) {
				Vector3 spCopy = (Vector3)screenPoints [1];
				spCopy.x = tempScreenPoint.x;
				spCopy.y = tempScreenPoint.y;
				spCopy.z = tempScreenPoint.z;
				screenPoints[1] = spCopy;
				//Debug.Log ("updated max");
			}

		}
	}


	public void logReward (bool reward, bool lick) {
		if(reward)
			rewards+=1;
		if(lick)
			licks+=1;
	}

    public void SetMouseName(string s) {
        this.mouseName = s;
		Globals.mouseName = this.mouseName;
        MakeReplayName();
    }

    public void SetMaxReward(string s) {
        this.maxReward = s;
        float.TryParse(this.maxReward, out Globals.totalRewardSize);
        Debug.Log(Globals.totalRewardSize);
    }

    public void SetDayName(string s) {
        this.dayName = s;
		Globals.trainingDayNumber = this.dayName;
        MakeReplayName();
    }

	public void SetSessionName (string s) {
		this.sessionName = s;
		Globals.scenarioSessionNumber = this.sessionName;
		MakeReplayName();
	}

	public void SetDegCentrallyVisible(string s) {
		int deg = 30;
		if (!s.Equals ("")) {
			this.degCentrallyVisible = s;
			int.TryParse (this.degCentrallyVisible, out deg);
		}
		Globals.SetCentrallyVisible(deg);
	}

	public void SetVisibleNasalBoundary(string s) {
		float deg = Globals.defaultVisibleNasalBoundary;
		if (!s.Equals ("")) {
			float.TryParse (s, out deg);
		}
		Globals.visibleNasalBoundary = deg;
		Globals.defaultVisibleNasalBoundary = deg;
	}

	public void SetVisibleTemporalBoundary(string s) {
		float deg = Globals.defaultVisibleTemporalBoundary;
		if (!s.Equals ("")) {
			float.TryParse (s, out deg);
		}
		Globals.visibleTemporalBoundary = deg;
		Globals.defaultVisibleTemporalBoundary = deg;
	}

	public void SetVisibleHighBoundary(string s) {
		float deg = Globals.defaultVisibleHighBoundary;
		if (!s.Equals ("")) {
			float.TryParse (s, out deg);
		}
		Globals.visibleHighBoundary = deg;
		Globals.defaultVisibleHighBoundary = deg;
	}

	public void SetVisibleLowBoundary(string s)	{
		float deg = Globals.defaultVisibleLowBoundary;
		if (!s.Equals ("")) {
			float.TryParse (s, out deg);
		}
		Globals.visibleLowBoundary = deg;
		Globals.defaultVisibleLowBoundary = deg;
	}

    public void SetScenarioName(string s) {
        if (s.EndsWith(".xml"))
            this.scenarioName = s.Substring(0, s.Length - 4);
        else
            this.scenarioName = s;
		Globals.scenarioName = this.scenarioName;
        MakeReplayName();
    }
    
    public void SetReplayFileName(string s) {
        this.replayFileName = s;
    }

    public string GetReplayFileName() {
        return this.replayFileName;
    }

    private void MakeReplayName() {
        this.replayFileName = this.mouseName + "-D" + this.dayName + "-" + this.scenarioName + "-S" + this.sessionName;
        if (File.Exists(PlayerPrefs.GetString("replayFolder") + "/" + this.replayFileName + "_actions.txt"))
            this.errorText.text = "ERROR: File for this mouse already exists!  Results will be overwritten if you proceed.";
        else
            this.errorText.text = "";
    }

    public void SetFileSet(bool b) {
        this.fileSet = b;
    }

    public void SetRun(int run) {
        string fn = this.replayFileName;
		fn += "-" + System.DateTime.Now.Year + "-" + System.DateTime.Now.Month + "-" + System.DateTime.Now.Day +
			"-" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute + "-" + System.DateTime.Now.Second;
		if (outfile != null) {
			outfile.Close ();
			rewardFile.Close ();
		}
        if( this.replayFileName.IndexOf('.') > 0 ) {
            string[] tmp = this.replayFileName.Split('.');
            tmp[0] += "-" + run;
            fn = tmp[0] + "." + tmp[1];
        } else {
            fn += "-" + run;
        }

        outfile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + fn);
		rewardFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/rew_" + fn);
    }

}