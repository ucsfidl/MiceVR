using UnityEngine;
using System.Collections;
using System.IO;
using System;
using GoogleSheetsToUnity;
using System.Collections.Generic;

public static class Globals
{
    public static bool newData;
    public static SphereInput sphereInput;
    public static System.Collections.Generic.Queue<float> lastMouse1X, lastMouse1Y, lastMouse2X, lastMouse2Y;
    public static bool playerInWaterTree;
    public static bool playerInDryTree;
    public static int udpSyncPort;
    public static int udpMouseStatePort;
    public static int udpInWaterTreePort;
    public static int udpInDryTreePort;
    public static string waterTexture;
    public static string dryTexture;
    public static bool timeoutState;
	public static int numberOfDryTrees;

    public static int numberOfEarnedRewards;
    public static int numberOfUnearnedRewards;
    public static float rewardAmountSoFar;

	// NB edit: globals to keep track of successful turns in the T maze
	public static bool hasNotTurned;
    public static ArrayList trialStartTime = new ArrayList(); // Holds DateTime object of starttime
    public static ArrayList trialEndTime = new ArrayList(); // Holds DateTime object of endtime
	public static ArrayList targetLoc = new ArrayList(); // X coord of tree placed in this list
    public static ArrayList targetHFreq = new ArrayList();  // Orientation of target
    public static ArrayList targetVFreq = new ArrayList();  // Orientation of target
    public static ArrayList firstTurn = new ArrayList(); // X coord of tree the mouse hit or would have hit is placed in this list
    public static ArrayList firstTurnHFreq = new ArrayList();  // Orientation of the tree the mouse chose
    public static ArrayList firstTurnVFreq = new ArrayList();  // Orientation of the tree the mouse chose
    public static int numCorrectTurns;
	public static int trialsSinceMouseStreakEliminated = 3;

	public static int trialDelay;
	public static int numberOfTrials;

	public static float centralViewVisibleShift; // Set in gameconfig file

    public static int rewardDur;  // duration in ms of a single drop
    public static float rewardSize;  // what the above duration results in in ul
    public static float totalRewardSize = 1000;  // total amount the mouse can be given, in ul
    public static ArrayList sizeOfRewardGiven = new ArrayList(); // in ul

    public static string gameType;  // Default: detection - The type of game, as specified in the scenario file and detected by Loader
    public static string gameTurnControl; // Default: yaw - Whether to use YAW or ROLL for game turning control on the ball
    public static bool varyOrientation;
	public static float rewardedHFreq;
	public static float rewardedVFreq;
	public static float distractorHFreq;
	public static float distractorVFreq;


	public static Color distColor1;
	public static Color distColor2;

    public static MovementRecorder mRecorder = GameObject.Find("FPSController").GetComponent<MovementRecorder>();
    public static DateTime gameStartTime;
	public static DateTime gameEndTime = new DateTime();

	public static bool biasCorrection = true;  // default to true if nothing entered in scenario file
	public static float probeLocX = float.NaN;
	public static bool perim = false;  // default to false, as perimetry is only running in special circumstances
	public static int perimScale;  // 0 = full scale, 1 = 40x40 deg, 2=20x20 deg, 3 = 10x10 deg.  Note a larger number also includes the scales smaller than itself.

	public static string mouseName = "";
	public static string scenarioName = "";
	public static string trainingDayNumber = "";
	public static string scenarioSessionNumber = "";

	public static int inputDeg;

	public static int numMice = 2;  // Number of optical mice detectors: default is 2, 1 is used if only yaw is used for rotation

	public static float monitorPositiveElevation;
	public static float monitorNegativeElevation;
	public static float monitorAzimuth;
	public static float fovNasalAzimuth;  // FOV of monitors, as opposed to limits of tree visibility (see below)
	public static float fovTemporalAzimuth;

	public static float occluderXScale;
	public static float occluderYScale;

	// These are initial values, but if the user provides any values, these get overwritten during runtime
	public static float defaultVisibleNasalBoundary = 0;
	public static float defaultVisibleTemporalBoundary = 90;
	public static float defaultVisibleHighBoundary = 50;
	public static float defaultVisibleLowBoundary = -30;

	public static float visibleNasalBoundary = defaultVisibleNasalBoundary;
	public static float visibleTemporalBoundary = defaultVisibleTemporalBoundary;
	public static float visibleHighBoundary = defaultVisibleHighBoundary;
	public static float visibleLowBoundary = defaultVisibleLowBoundary;

	public static float worldXCenter = 20000;  // used to discriminate trees placed on the left vs the right

	public static string vrGoogleSheetsName;

	public static int numCameras = 0;
	public static int currFrame = 1;

	public struct fov {
		public float nasalBound;
		public float tempBound;
		public float highBound;
		public float lowBound;
	}

	public static fov[] fovs;  // For automatic perimetry
	public static int[] fovsForPerimScaleInclusive = new int[] {1, 3, 9, 21};

	// Initializes fovs used for automated perimetry.  There are 4 scales, and the stimuli are presented that fill (or half-fill, for the smallest) scale
	// TODO: don't do linear calculations, but trigometric ones, which are accurate
	public static void InitFOVs() {
		fovs = new fov[21];

		// FULL FIELD
		int curPos = 0;
		fovs [curPos].nasalBound = defaultVisibleNasalBoundary;
		fovs [curPos].tempBound = defaultVisibleTemporalBoundary;
		fovs [curPos].lowBound = defaultVisibleLowBoundary;
		fovs [curPos].highBound = defaultVisibleHighBoundary;

		// 40x40 : YELLOW in diagram, except shifted to the left 10 degrees
		curPos = curPos + 1;
		fovs [curPos].nasalBound = 0;
		fovs [curPos].tempBound = fovs [curPos].nasalBound + 40;
		fovs [curPos].lowBound = 5;
		fovs [curPos].highBound = fovs [curPos].lowBound + 40;

		// 40x40 : RED in diagram
		curPos = curPos + 1;
		fovs [curPos].nasalBound = 30;
		fovs [curPos].tempBound = fovs [curPos].nasalBound + 40;
		fovs [curPos].lowBound = 5;
		fovs [curPos].highBound = fovs [curPos].lowBound + 40;

		// 20x20 : GREEN in diagram
		for (int i = 0; i < 6; i++) {
			curPos = curPos + 1;
			fovs [curPos].nasalBound = 10 + i % 3 * 20;
			fovs [curPos].tempBound = fovs [curPos].nasalBound + 20;
			fovs [curPos].lowBound = 5 + i / 3 * 20;
			fovs [curPos].highBound = fovs [curPos].lowBound + 20;
		}

		// 10x10 : Shaded in diagram
		for (int i = 0; i < 12; i++) { // Initialize the small stimulus set
			curPos = curPos + 1;
			if (i / 3 % 2 == 0) { // even row
				fovs[curPos].nasalBound = 20 + i % 3  * 20;
			} else {
				fovs [curPos].nasalBound = 10 + i % 3 * 20;
			}
			fovs [curPos].tempBound = fovs [curPos].nasalBound + 10;
			fovs [curPos].lowBound = 5 + i / 3 * 10;
			fovs [curPos].highBound = fovs [curPos].lowBound + 10;
		}
	}

    // This function writes out all the statistics to a single file, currently when the game ends.
    public static void InitLogFiles() {
        // overwrite any existing file
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", false);
        // Write file header
        turnsFile.WriteLine("#TrialStartTime\tTrialEndTime\tTrialEndFrame\tTrialDur\tTargetLocation\tTargetHFreq\tTargetVFreq\tNasalBound\tTemporalBound\tHighBound\tLowBound\tTurnLocation\tTurnHFreq\tTurnVFreq\tRewardSize(ul)");
        turnsFile.Close();
    }

    public static void WriteToLogFiles() {
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", true);
        // Write out turn decisions over time - easy to import into Excel and analyze

        Debug.Log(trialStartTime[trialStartTime.Count - 1] + "\t" +
                    trialEndTime[trialEndTime.Count - 1] + "\t" +
					currFrame + "\t" + 
                    ((TimeSpan)trialEndTime[trialEndTime.Count - 1]).Subtract((TimeSpan)trialStartTime[trialStartTime.Count - 1]) + "\t" +
                    targetLoc[targetLoc.Count - 1] + "\t" +
                    targetHFreq[targetHFreq.Count - 1] + "\t" +
                    targetVFreq[targetVFreq.Count - 1] + "\t" +
					visibleNasalBoundary + "\t" +
					visibleTemporalBoundary + "\t" + 
					visibleHighBoundary + "\t" +
					visibleLowBoundary + "\t" + 
                    firstTurn[firstTurn.Count - 1] + "\t" +
                    firstTurnHFreq[firstTurnHFreq.Count - 1] + "\t" +
                    firstTurnVFreq[firstTurnVFreq.Count - 1] + "\t" +
                    (float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]));

        turnsFile.WriteLine(trialStartTime[trialStartTime.Count - 1] + "\t" +
                    trialEndTime[trialEndTime.Count - 1] + "\t" +
					currFrame + "\t" + 
                    ((TimeSpan)trialEndTime[trialEndTime.Count - 1]).Subtract((TimeSpan)trialStartTime[trialStartTime.Count - 1]) + "\t" +
                    targetLoc[targetLoc.Count - 1] + "\t" +
                    targetHFreq[targetHFreq.Count - 1] + "\t" +
                    targetVFreq[targetVFreq.Count - 1] + "\t" +
					visibleNasalBoundary + "\t" +
					visibleTemporalBoundary + "\t" + 
					visibleHighBoundary + "\t" +
					visibleLowBoundary + "\t" + 
                    firstTurn[firstTurn.Count - 1] + "\t" +
                    firstTurnHFreq[firstTurnHFreq.Count - 1] + "\t" +
                    firstTurnVFreq[firstTurnVFreq.Count - 1] + "\t" +
                    (float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]));
        turnsFile.Close();
        WriteStatsFile();
    }

    public static void WriteStatsFile()
    {
        StreamWriter statsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_stats.txt");
        statsFile.WriteLine("<document>");
        statsFile.WriteLine("\t<stats>");
        statsFile.WriteLine("\t\t<accuracy>" + Math.Round((float)numCorrectTurns / ((float)numberOfTrials - 1) * 100) + "%" + GetTreeAccuracy() + "</accuracy>");
        // TODO - Fix off by one error when mouse finishes game!
        statsFile.WriteLine("\t\t<numEarnedRewards>" + numCorrectTurns + "</numEarnedRewards>");
        statsFile.WriteLine("\t\t<numUnearnedRewards>" + numberOfUnearnedRewards + "</numUnearnedRewards>");
        statsFile.WriteLine("\t\t<trials>" + (numberOfTrials - 1) + "</trials>");

        TimeSpan te = gameEndTime.Subtract(gameStartTime);
        statsFile.WriteLine("\t\t<timeElapsed>" + string.Format("{0:D2}:{1:D2}", te.Hours * 60 + te.Minutes, te.Seconds) + "</timeElapsed>");

        float totalEarnedRewardSize = 0;
        for (int i = 0; i < sizeOfRewardGiven.Count; i++)
        {
            totalEarnedRewardSize += (float)System.Convert.ToDouble(sizeOfRewardGiven[i]);
        }

        statsFile.WriteLine("\t\t<totalRewardSizeReceived>" + (totalEarnedRewardSize + (float)Globals.numberOfUnearnedRewards * rewardSize).ToString() + "</trials>");
        statsFile.WriteLine("\t</stats>");
        statsFile.WriteLine("</document>");
        statsFile.Close();
    }

	// Write the data to Google Sheets so that the experimenter does not need to memorize and type in results, which is prone to error
	public static bool WriteStatsToGoogleSheet() {
		SpreadSheetManager manager = new SpreadSheetManager();
		GS2U_SpreadSheet spreadsheet = manager.LoadSpreadSheet (vrGoogleSheetsName);
		GS2U_Worksheet worksheet = spreadsheet.LoadWorkSheet(mouseName);
		if (worksheet == null) {
			Debug.Log ("Data not saved to Sheets, as worksheet named '" + mouseName + "' was NOT found");
			return false;
		} else {
			WorksheetData data = worksheet.LoadAllWorksheetInformation ();

			// Helper vars
			TimeSpan te = gameEndTime.Subtract (gameStartTime);
			float numMinElapsed = te.Hours * 60 + te.Minutes + (int)Math.Round ((double)te.Seconds / 60);
			if (numMinElapsed == 0)
				numMinElapsed = 1;

			float totalEarnedRewardSize = 0;
			for (int i = 0; i < sizeOfRewardGiven.Count; i++) {
				totalEarnedRewardSize += (float)System.Convert.ToDouble (sizeOfRewardGiven [i]);
			}

			// Iterate the file
			for (int i = 0; i < data.rows.Count; i++) {
				if (data.rows [i].cells [12].value.Equals ("") && data.rows [i].cells [13].value.Equals ("")) {
					int row = i + 1;
					// Add the date (L), duration, rewards, trials, earned, unearned on ball, and total stats to the Google Sheet
					worksheet.ModifyRowData (row, new Dictionary<string, string> {
						{ "date", DateTime.Today.ToString ("d") },
						{ "durm", numMinElapsed.ToString() },
						{ "rewards", numCorrectTurns.ToString () },
						{ "rmin", string.Format ("{0:N1}", (numCorrectTurns / numMinElapsed)) },
						{ "trials", (numberOfTrials - 1).ToString () },
						{ "tmin", string.Format ("{0:N1}", (numberOfTrials - 1) / numMinElapsed) },
						{ "accuracy", Math.Round ((float)numCorrectTurns / ((float)numberOfTrials - 1) * 100) + "%" },
						{ "earned", Math.Round (totalEarnedRewardSize).ToString () },
						{ "uball", Math.Round ((float)numberOfUnearnedRewards * rewardSize).ToString () },
						{ "results", Math.Round ((float)numCorrectTurns / ((float)numberOfTrials - 1) * 100) + GetTreeAccuracy () },
						{ "totalh2o", "=SUM(T" + (row + 1) + ":V" + (row + 1) + ")" }
					}
					);

					break;
				}
			}
			Debug.Log ("wrote to worksheet " + mouseName);
			return true;
		}
	}

    public static string GetTreeAccuracy()
    {
        GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
        float[] locs = new float[gos.Length];
        int[] numCorrTrials = new int[gos.Length];
        int[] numTrials = new int[gos.Length];
        for (int i = 0; i < gos.Length; i++)
        {
            locs[i] = gos[i].transform.position.x;
        }
        //With all locations in hand, calculate accuracy for each one, then print it out

        int idx;
        for (int i = 0; i < Globals.firstTurn.Count; i++)
        {
            //Debug.Log((float)System.Convert.ToDouble(Globals.targetLoc[i]));
            idx = Array.IndexOf(locs, (float)System.Convert.ToDouble(Globals.targetLoc[i]));
            numTrials[idx]++;
            if (Globals.targetLoc[i].Equals(Globals.firstTurn[i]))
                numCorrTrials[idx]++;
        }

        string output = "";
        for (int i = 0; i < numTrials.Length; i++)
        {
            output += "/" + Math.Round((float)numCorrTrials[i] / numTrials[i] * 100)	;
        }
        return output;
    }

    // Redo bias correction to match Harvey et al publication, where probability continuously varies based on mouse history on last 20 trials
    // Previous attempt at streak elimination didn't really work... Saw mouse go left 100 times or so! And most mice exhibited a bias, even though they may not have before...
    public static float GetTurnBias(int histLen, int treeIndex)
    {
        GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
        int len = Globals.firstTurn.Count;
        int turn0 = 0;
        int start;
        int end;
        int numTrials;
        if (len >= histLen) {
            end = len;
            start = len - histLen;
        } else {
            start = 0;
            end = len;
        }
        numTrials = end - start;
		// TODO: Do I even need this if set?
		if (gos.Length == 2) {
			for (int i = start; i < end; i++) {
				if (Globals.firstTurn [i].Equals (gos [treeIndex].transform.position.x))
					turn0++;
			}
		} else if (gos.Length == 3) {
			for (int i = start; i < end; i++) {
				if (Globals.firstTurn [i].Equals (gos [treeIndex].transform.position.x))
					turn0++;
			}
		} else if (gos.Length == 4) {
			for (int i = start; i < end; i++) {
				if (Globals.firstTurn [i].Equals (gos [treeIndex].transform.position.x))
					turn0++;
			}
		}

        return (float)turn0 / numTrials;
    }

    public static float GetLastAccuracy(int n)
    {
        int len = firstTurn.Count;
        int start;
        int end;
        int numTrials;
        if (len >= n)
        {
            end = len;
            start = len - n;
        }
        else
        {
            start = 0;
            end = len;
        }
        numTrials = end - start;
        int corr = 0;
        for (int i = start; i < end; i++)
        {
            if (firstTurn[i].Equals(targetLoc[i]))
                corr++;
        }
        return (float)corr / numTrials;
    }

	// Calculate tree view block value: 0 is full occlusion in the central screen = 120 degrees
	// 0.9 is full visibility with occluder pushed all the way to the screen
	public static void SetCentrallyVisible(int deg) {
		Globals.centralViewVisibleShift = (float)(deg * 0.58/120);  // 0.45/120
		Globals.inputDeg = deg;
	}

	/*
	private void updateTrialsText() {
		this.numberOfTrialsText.text = "Trial: #" + Globals.numberOfTrials.ToString ();
	}

	private void updateRewardAmountText() {
		this.rewardAmountText.text = "Reward: " + Math.Round(Globals.rewardAmountSoFar).ToString() + " of " + Math.Round(Globals.totalRewardSize);
	}

	private void updateCorrectTurnsText() {
		this.numberOfCorrectTurnsText.text = "Correct: " +
			Globals.numCorrectTurns.ToString ()
			+ " (" +
			Mathf.Round(((float)Globals.numCorrectTurns / ((float)Globals.numberOfTrials)) * 100).ToString() + "%" 
			+ FreeGlobals.GetTreeAccuracy() + ")";
	}
	*/

	// Old function, which did not pass the extra arguments
	// Just set the values to be logged first
	public static void SetOccluders(float locx) {
		visibleNasalBoundary = defaultVisibleNasalBoundary;
		visibleTemporalBoundary = defaultVisibleTemporalBoundary;
		visibleHighBoundary = defaultVisibleHighBoundary;
		visibleLowBoundary = defaultVisibleLowBoundary;
		SetOccluders (locx, visibleNasalBoundary, visibleTemporalBoundary, visibleHighBoundary, visibleLowBoundary);
	}

	// For picking the occluder by index from the set above
	public static void SetOccluders(float locx, int idx) {
		Debug.Log ("FOV: " + fovs [idx].nasalBound + ", " + fovs [idx].tempBound + ", " + fovs [idx].highBound + ", " + fovs[idx].lowBound);
		visibleNasalBoundary = fovs [idx].nasalBound;
		visibleTemporalBoundary = fovs [idx].tempBound;
		visibleHighBoundary = fovs [idx].highBound;
		visibleLowBoundary = fovs [idx].lowBound;
		SetOccluders (locx, visibleNasalBoundary, visibleTemporalBoundary, visibleHighBoundary, visibleLowBoundary);
	}

	public static void SetOccluders(float locx, float nasalBound, float tempBound, float highBound, float lowBound) {
		GameObject tolt = GameObject.Find("TreeOccluderLT");
		GameObject tolmt = GameObject.Find("TreeOccluderLMT");
		GameObject tolmn = GameObject.Find("TreeOccluderLMN");
		GameObject tolb = GameObject.Find("TreeOccluderLB");
		GameObject toct = GameObject.Find("TreeOccluderCT");
		GameObject tocmt = GameObject.Find("TreeOccluderCMT");
		GameObject tocmn = GameObject.Find("TreeOccluderCMN");
		GameObject tocb = GameObject.Find("TreeOccluderCB");
		GameObject tort = GameObject.Find("TreeOccluderRT");
		GameObject tormt = GameObject.Find("TreeOccluderRMT");
		GameObject tormn = GameObject.Find("TreeOccluderRMN");
		GameObject torb = GameObject.Find("TreeOccluderRB");

		// Local vars to store calculations to reuse
		// I don't think I need these anymore, as this was when I thought the stimulus window would be centered around the horizon.  Now it can be offset, so something else needs to be done
		float ysp = monitorPositiveElevation / (monitorPositiveElevation - monitorNegativeElevation) * occluderYScale;  // OccluderYScale is height of occluder if it will fill the full height of the screen
		float ysn = monitorNegativeElevation / (monitorNegativeElevation - monitorPositiveElevation) * occluderYScale;
		float totalElevation = monitorPositiveElevation - monitorNegativeElevation;
		Vector3 newPos;

		// PULL BACK CURTAINS
		// ==================
		// First, set the x and y scale values and positions of each occluder based on parameters found in the gameconfig file.
		// There are 4 occluders on the left screen, 4 occluders on the center screen, and 4 occluders on the right screen.
		// Occluders are setup so that based on the user values, all one needs to do is shift the position of each occluder to create the intended visible window.
		// Initially, the occluders are off the screen on the temporal side if nasal or temporal, and above or below if top or bottom, respectively.
		// After sized each curtain, shift all of the curtains away so everything is visible.  Depending on the user inputs, we will shift them back to occlude trees.  
		// If the user left all params blank at the start of the session, then trees will simply be restricted to the corresponding hemifield separated by the vertical midline.

		// LEFT SCREEN FOR LEFT TREES
		tolt.transform.localScale = new Vector3(occluderXScale, occluderYScale, 1);
		tolt.transform.localPosition = new Vector3 (0, occluderYScale, 0.5F);
		tolb.transform.localScale = new Vector3(occluderXScale, occluderYScale, 1);
		tolb.transform.localPosition = new Vector3 (0, -occluderYScale, 0.5F);
		tolmt.transform.localScale = new Vector3(occluderXScale, occluderYScale, 1);
		tolmt.transform.localPosition = new Vector3 (-occluderXScale, 0, 0.5F);
		tolmn.transform.localScale = tolmt.transform.localScale;
		tolmn.transform.localPosition = tolmt.transform.localPosition;

		// CENTER SCREEN FOR BOTH TREES
		toct.transform.localScale = new Vector3 (occluderXScale, occluderYScale, 1);
		toct.transform.localPosition = new Vector3 (0, occluderYScale, 0.5F);
		tocb.transform.localScale = new Vector3 (occluderXScale, occluderYScale, 1);
		tocb.transform.localPosition = new Vector3 (0, -occluderYScale, 0.5F);
		if (locx < worldXCenter) {  // Tree is on left side, so shift center curtains to the right of center
			tocmt.transform.localScale = new Vector3 (occluderXScale, occluderYScale, 1);
			tocmt.transform.localPosition = new Vector3 (occluderXScale / 2, 0, 0.5F);
		} else {  // Tree is on the right side, so shift center curtain to the left of center
			tocmt.transform.localScale = new Vector3 (occluderXScale, occluderYScale, 1);
			tocmt.transform.localPosition = new Vector3 (-occluderXScale / 2, 0, 0.5F);
		}
		tocmn.transform.localScale = tocmt.transform.localScale;
		tocmn.transform.localPosition = tocmt.transform.localPosition;

		// RIGHT SCREEN FOR RIGHT TREES
		tort.transform.localScale = new Vector3(occluderXScale, occluderYScale, 1);
		tort.transform.localPosition = new Vector3 (0, occluderYScale, 0.5F);
		torb.transform.localScale = new Vector3(occluderXScale, occluderYScale, 1);
		torb.transform.localPosition = new Vector3 (0, -occluderYScale, 0.5F);	
		tormt.transform.localScale = new Vector3(occluderXScale, occluderYScale, 1);
		tormt.transform.localPosition = new Vector3 (occluderXScale, 0, 0.5F);
		tormn.transform.localScale = tormt.transform.localScale;
		tormn.transform.localPosition = tormt.transform.localPosition;

		// Now that all the occluders are setup properly, just shift their positions to get to the intended visible window
		// First, shift the top curtains
		if (highBound < monitorPositiveElevation) {
			newPos = new Vector3(0, -1 * (monitorNegativeElevation - highBound) * (occluderYScale / totalElevation), 0.5F);
			tolt.transform.localPosition = newPos;
			toct.transform.localPosition = newPos;
			tort.transform.localPosition = newPos;
		}

		// Second, shift the bottom curtains
		if (lowBound > monitorNegativeElevation) {
			newPos = new Vector3(0, -1 * (monitorPositiveElevation - lowBound) * (occluderYScale / totalElevation), 0.5F);
			tolb.transform.localPosition = newPos;
			tocb.transform.localPosition = newPos;
			torb.transform.localPosition = newPos;
		}
	
		// Third, shift the curtains to enforce a nasal border
		if (nasalBound > fovNasalAzimuth) {
			if (nasalBound > monitorAzimuth / 2) {
				// Move central occluder all the way temporal
				tocmn.transform.localPosition = new Vector3 (0, 0, 0.5F);
				float margin = nasalBound - monitorAzimuth / 2;
				if (locx < worldXCenter) {
					tolmn.transform.localPosition = new Vector3 (occluderXScale - (margin / monitorAzimuth * occluderXScale), 0, 0.5F);
				} else {
					tormn.transform.localPosition = new Vector3 (-occluderXScale + (margin / monitorAzimuth * occluderXScale), 0, 0.5F);
				}
			} else {
				if (locx < worldXCenter) {
					tocmn.transform.localPosition = new Vector3 ((1 - nasalBound/(monitorAzimuth/2)) * (occluderXScale/2), 0, 0.5F);
				} else {
					tocmn.transform.localPosition = new Vector3 (-(1 - nasalBound/(monitorAzimuth/2)) * (occluderXScale/2), 0, 0.5F);
				}
			}
		}

		// Fourth and finally, shift the curtains to enforce a temporal border
		if (tempBound < fovTemporalAzimuth) {
			if (tempBound < fovTemporalAzimuth - monitorAzimuth) { // boundary spans more than the side monitor
				if (locx < worldXCenter) {  // Tree is on the left
					tolmt.transform.localPosition = new Vector3 (0, 0, 0.5F);
					tocmt.transform.localPosition = new Vector3 (-occluderXScale + ((monitorAzimuth / 2 - tempBound) / monitorAzimuth) * occluderXScale, 0, 0.5F);
				} else {
					tormt.transform.localPosition = new Vector3 (0, 0, 0.5F);
					tocmt.transform.localPosition = new Vector3 (occluderXScale - ((monitorAzimuth / 2 - tempBound) / monitorAzimuth) * occluderXScale, 0, 0.5F);
				}
			} else {  // boundary is restricted to the side monitor
				if (locx < worldXCenter) {
					tolmt.transform.localPosition = new Vector3 (-occluderXScale + ((fovTemporalAzimuth - tempBound) / monitorAzimuth) * occluderXScale, 0, 0.5F);
				} else {
					tormt.transform.localPosition = new Vector3 (occluderXScale - ((fovTemporalAzimuth - tempBound) / monitorAzimuth) * occluderXScale, 0, 0.5F);
				}
			}
		}
	
	}

}