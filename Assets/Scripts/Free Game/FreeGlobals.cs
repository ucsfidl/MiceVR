using UnityEngine;
using System.Collections;
using System.IO;
using System;
using GoogleSheetsToUnity;
using System.Collections.Generic;

public static class FreeGlobals
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
	public static ArrayList targetDeg = new ArrayList();    // Rotation of target
    public static ArrayList firstTurn = new ArrayList(); // X coord of tree the mouse hit or would have hit is placed in this list
    public static ArrayList firstTurnHFreq = new ArrayList();  // Orientation of the tree the mouse chose
    public static ArrayList firstTurnVFreq = new ArrayList();  // Orientation of the tree the mouse chose
	public static ArrayList firstTurnDeg = new ArrayList();    // Rotation of the tree the mouse chose
	public static ArrayList stimDur = new ArrayList();
	public static ArrayList stimReps = new ArrayList();
    public static int numCorrectTurns;
	public static int trialsSinceMouseStreakEliminated = 3;

	public static float trialDelay; 
	public static int numberOfTrials;

	public static float centralViewVisibleShift;

    public static int rewardDur;  // duration in ms of a single drop
    public static float rewardSize;  // what the above duration results in in ul
    public static float totalRewardSize = 1000;  // total amount the mouse can be given, in ul
    public static ArrayList sizeOfRewardGiven = new ArrayList(); // in ul

    public static string gameType;  // Default: detection - The type of game, as specified in the scenario file and detected by Loader
    public static string gameTurnControl; // Default: yaw - Whether to use YAW or ROLL for game turning control on the ball
    public static bool varyOrientation;
    public static float rewardedHFreq;
    public static float rewardedVFreq;
	public static Color rewardedColor1;
	public static Color rewardedColor2;
	public static float nonRewardedHFreq = -1;
	public static float nonRewardedVFreq;
	public static Color nonRewardedColor1;
	public static Color nonRewardedColor2;
	public static bool nonRewardedColorSwap = false;
	public static float oriPersistenceDur = -1;
	public static float greyPersistenceDur = -1;
	public static float luminanceDiff = -1;
	public static string flickerType = "stripes";
	public static string subType;

	public static string targetChange;

    public static FreeMovementRecorder mRecorder = GameObject.Find("FPSController").GetComponent<FreeMovementRecorder>();
    public static DateTime gameStartTime;
	public static DateTime gameEndTime = default(DateTime);

	// Rewards that are given for eack lickport, calibrated for each lickport to give a constant amount of reward across ports (to start)
	public static int[] freeRewardDur = {15, 30, 30, 38, 38, 38}; // prev 15, 30x
	public static int[] freeRewardSite = {0, 2, 4, 6, 8, 10};  // corresponds to nosePokeReward et al in Arduino code

	public static bool waterAtStart = false;
	public static bool stimPersists = true;
	public static float persistenceDur = -1;  // -1 indicates that the stim persists until a choice is made

	public static string freeState = "preload";

	// Parameters for delay match to sample
	public static float choiceDelay = 0;
	public static float startRewardDelay = 0;
	public static string rewardedOri = "none";
	public static int firstRewardDur = -1; // ms

	// Parameters for curvy vs. straight discrimination
	public static string rewardedLineType;
	public static string nonRewardedLineType = "curvy";  // for backwards compatibility with initial levels
	public static float rewardedAmplitude;
	public static float nonRewardedAmplitude;
	public static float rewardedNumCycles;
	public static bool randomPhase = true;

	// Parameters for discrimination task
	public static float numStim = 2;
	public static float numPorts = 2;
	public static float simulDisplay = 2;

	public static bool biasCorrection = true;
	public static float probeLocX = float.NaN;
	public static float probeLocX2 = float.NaN;

	public static string mouseName = "";
	public static string scenarioName = "";
	public static string trainingDayNumber = "";
	public static string scenarioSessionNumber = "";

	public static float worldXCenter = 20000;

	public static float defaultVisibleHighBoundary = 66;

	public static float visibleHighBoundary = defaultVisibleHighBoundary;

	public static float occluderYScale;
	public static float monitorPositiveElevation;

	public static string freeGoogleSheetsName;

    // This function writes out all the statistics to a single file, currently when the game ends.
    public static void InitLogFiles()
    {
        // overwrite any existing file
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", false);
        // Write file header
		Debug.Log("#TrialStartTime\tTrialEndTime\tTrialDur\tTargetLocation\tTargetHFreq\tTargetVFreq\tTurnLocation\tTurnHFreq\tTurnVFreq\tRewardSize(ul)\tStimDur(ms)\tStimReps");
		turnsFile.WriteLine("#TrialStartTime\tTrialEndTime\tTrialDur\tTargetLocation\tTargetHFreq\tTargetVFreq\tTurnLocation\tTurnHFreq\tTurnVFreq\tRewardSize(ul)\tStimDur(ms)\tStimReps");
        turnsFile.Close();
    }

    public static void WriteToLogFiles()
    {
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", true);
        // Write out turn decisions over time - easy to import into Excel and analyze

        Debug.Log(trialStartTime[trialStartTime.Count - 1] + "\t" +
                    trialEndTime[trialEndTime.Count - 1] + "\t" +
                    ((TimeSpan)trialEndTime[trialEndTime.Count - 1]).Subtract((TimeSpan)trialStartTime[trialStartTime.Count - 1]) + "\t" +
                    targetLoc[targetLoc.Count - 1] + "\t" +
                    targetHFreq[targetHFreq.Count - 1] + "\t" +
                    targetVFreq[targetVFreq.Count - 1] + "\t" +
					targetDeg[targetDeg.Count - 1] + "\t" +
                    firstTurn[firstTurn.Count - 1] + "\t" +
                    firstTurnHFreq[firstTurnHFreq.Count - 1] + "\t" +
                    firstTurnVFreq[firstTurnVFreq.Count - 1] + "\t" +
					firstTurnDeg[firstTurnDeg.Count - 1] + "\t" +
                    (float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]) + "\t" +
					stimDur[stimDur.Count - 1] + "\t" + 
					stimReps[stimReps.Count - 1]);

        turnsFile.WriteLine(trialStartTime[trialStartTime.Count - 1] + "\t" +
                    trialEndTime[trialEndTime.Count - 1] + "\t" +
                    ((TimeSpan)trialEndTime[trialEndTime.Count - 1]).Subtract((TimeSpan)trialStartTime[trialStartTime.Count - 1]) + "\t" +
                    targetLoc[targetLoc.Count - 1] + "\t" +
                    targetHFreq[targetHFreq.Count - 1] + "\t" +
                    targetVFreq[targetVFreq.Count - 1] + "\t" +
					targetDeg[targetDeg.Count - 1] + "\t" +
                    firstTurn[firstTurn.Count - 1] + "\t" +
                    firstTurnHFreq[firstTurnHFreq.Count - 1] + "\t" +
                    firstTurnVFreq[firstTurnVFreq.Count - 1] + "\t" +
					firstTurnDeg[firstTurnDeg.Count - 1] + "\t" +
					(float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]) + "\t" +
					stimDur[stimDur.Count - 1] + "\t" + 
					stimReps[stimReps.Count - 1]);
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

        statsFile.WriteLine("\t\t<totalRewardSizeReceived>" + (totalEarnedRewardSize + (float)numberOfUnearnedRewards * rewardSize).ToString() + "</trials>");
        statsFile.WriteLine("\t</stats>");
        statsFile.WriteLine("</document>");
        statsFile.Close();
    }

	// Write the data to Google Sheets so that the experimenter does not need to memorize and type in results, which is prone to error
	public static bool WriteStatsToGoogleSheet() {
		// COMMENTED OUT BECAUSE THIS NO LONGER WORKS, as v3 of the Sheets api has been deleted.  Update to work with v4 by copying over from Globals.cs
		/*
		SpreadSheetManager manager = new SpreadSheetManager();
		GS2U_SpreadSheet spreadsheet = manager.LoadSpreadSheet (freeGoogleSheetsName);
		GS2U_Worksheet worksheet = spreadsheet.LoadWorkSheet(mouseName);
		if (worksheet == null) {
			Debug.Log ("Data not saved to Sheets, as worksheet named '" + mouseName + "' was NOT found");
			return false;
		} else {
			Debug.Log ("loaded worksheet " + mouseName);
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

			for (int i = 0; i < data.rows.Count; i++) {
				if (data.rows [i].cells [9].value.Equals ("") && data.rows [i].cells [10].value.Equals ("")) {
					int row = i + 1;
					// Add the date (L), duration, rewards, trials, earned, unearned on ball, and total stats to the Google Sheet
					worksheet.ModifyRowData (row, new Dictionary<string, string> {
						{ "date", DateTime.Today.ToString ("d") },
						{ "durm", numMinElapsed.ToString() },
						{ "rewards", numCorrectTurns.ToString () },
						{ "rmin", string.Format ("{0:N1}", (numCorrectTurns / numMinElapsed)) },
						{ "trials", (numberOfTrials).ToString () },
						{ "tmin", string.Format ("{0:N1}", (numberOfTrials - 1) / numMinElapsed) },
						{ "accuracy", Math.Round ((float)numCorrectTurns / ((float)numberOfTrials) * 100) + "%" },
						{ "earned", Math.Round (totalEarnedRewardSize).ToString () },
						{ "ugame", Math.Round ((float)numberOfUnearnedRewards * rewardSize).ToString () },
						{ "results", Math.Round ((float)numCorrectTurns / ((float)numberOfTrials) * 100) + GetTreeAccuracy () },
						{ "totalh2o", "=SUM(Q" + (row + 1) + ":S" + (row + 1) + ")" }
					}
					);

					break;
				}
			}
			return true;
		}
		*/
		return true;
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
        for (int i = 0; i < firstTurn.Count; i++)
        {
            //Debug.Log((float)System.Convert.ToDouble(targetLoc[i]));
            idx = Array.IndexOf(locs, (float)System.Convert.ToDouble(targetLoc[i]));
            numTrials[idx]++;
            if (targetLoc[i].Equals(firstTurn[i]))
                numCorrTrials[idx]++;
        }

        string output = "";
        for (int i = 0; i < numTrials.Length; i++)
        {
            output += "/" + Math.Round((float)numCorrTrials[i] / numTrials[i] * 100);
        }
        return output;
    }

    // Redo bias correction to match Harvey et al publication, where probability continuously varies based on mouse history on last 20 trials
    // Previous attempt at streak elimination didn't really work... Saw mouse go left 100 times or so! And most mice exhibited a bias, even though they may not have before...
    public static float GetTurnBias(int histLen, int treeIndex)
    {
        GameObject[] gos = GameObject.FindGameObjectsWithTag("water");
        int len = firstTurn.Count;
        int turn0 = 0;
        int start;
        int end;
        int numTrials;
        if (len >= histLen)
        {
            end = len;
            start = len - histLen;
        }
        else
        {
            start = 0;
            end = len;
        }
        numTrials = end - start;
        for (int i = start; i < end; i++)
        {
            if (firstTurn[i].Equals(gos[treeIndex].transform.position.x))
                turn0++;
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

	public static void SetOccluders() // Set once at start of FreeMoving game, to clip the top of the field of view
	{
		GameObject tol = GameObject.Find("TreeOccluderL");
		GameObject toc = GameObject.Find("TreeOccluderC");
		GameObject tor = GameObject.Find("TreeOccluderR");

		float occluderYPos = visibleHighBoundary / monitorPositiveElevation * occluderYScale;
		Vector3 newPos = new Vector3(0, occluderYPos, 0.11F);
		tol.transform.localPosition = newPos;
		toc.transform.localPosition = newPos;
		tor.transform.localPosition = newPos;
	}

}