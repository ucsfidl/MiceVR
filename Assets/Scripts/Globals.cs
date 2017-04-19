﻿using UnityEngine;
using System.Collections;
using System.IO;
using System;

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
	public static ArrayList firstTurn = new ArrayList(); // X coord of tree the mouse hit or would have hit is placed in this list
	public static int numCorrectTurns;
	public static int trialsSinceMouseStreakEliminated = 3;

	public static int trialDelay;
	public static int numberOfTrials;

	public static float centralViewVisibleShift;

    public static int rewardDur;  // duration in ms of a single drop
    public static float rewardSize;  // what the above duration results in in ul
    public static float totalRewardSize;  // in ul
    public static ArrayList sizeOfRewardGiven = new ArrayList(); // in ul

    public static string gameType;  // Default: detection - The type of game, as specified in the scenario file and detected by Loader
    public static string gameTurnControl; // Default: yaw - Whether to use YAW or ROLL for game turning control on the ball

    public static MovementRecorder mRecorder = GameObject.Find("FPSController").GetComponent<MovementRecorder>();
    public static DateTime gameStartTime;

    // This function writes out all the statistics to a single file, currently when the game ends.
    public static void InitLogFiles()
    {
        // overwrite any existing file
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", false);
        // Write file header
        Debug.Log("#TrialStartTime\tTrialEndTime\tTrialDur\tTargetLocation\tTurnLocation\tRewardSize(ul)");
        turnsFile.WriteLine("#TrialStartTime\tTrialEndTime\tTrialDur\tTargetLocation\tTurnLocation\tRewardSize(ul)");
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
                    firstTurn[firstTurn.Count - 1] + "\t" +
                    (float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]));

        turnsFile.WriteLine(trialStartTime[trialStartTime.Count - 1] + "\t" +
                            trialEndTime[trialEndTime.Count - 1] + "\t" +
                            ((TimeSpan)trialEndTime[trialEndTime.Count - 1]).Subtract((TimeSpan)trialStartTime[trialStartTime.Count - 1]) + "\t" +
                            targetLoc[targetLoc.Count - 1] + "\t" +
                            firstTurn[firstTurn.Count - 1] + "\t" +
                            (float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]));
        turnsFile.Close();
        WriteStatsFile();
    }

    public static void WriteStatsFile()
    {
        StreamWriter statsFile = new StreamWriter(PlayerPrefs.GetString("replayFolder") + "/" + mRecorder.GetReplayFileName() + "_stats.txt");
        statsFile.WriteLine("<document>");
        statsFile.WriteLine("\t<stats>");
        statsFile.WriteLine("\t\t<accuracy>" + Math.Round((float)numCorrectTurns / ((float)numberOfTrials)) + "%" + GetTreeAccuracy() + "</accuracy>");
        statsFile.WriteLine("\t\t<earnedRewards>" + numCorrectTurns + "</earnedRewards>");
        statsFile.WriteLine("\t\t<unearnedRewards>" + numberOfUnearnedRewards + "</unearnedRewards>");
        statsFile.WriteLine("\t\t<trials>" + (numberOfTrials - 1) + "</trials>");

        TimeSpan te = DateTime.Now.Subtract(gameStartTime);
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
            output += " / " + Math.Round((float)numCorrTrials[i] / numTrials[i] * 100) + "%";
        }
        return output;
    }


}