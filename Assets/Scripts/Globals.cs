using UnityEngine;
using System.Collections;

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
    public static int numberOfRewards;
    public static bool timeoutState;
	public static int numberOfDryTrees;

	// NB edit: globals to keep track of successful turns in the T maze
	public static bool hasNotTurned;
	public static ArrayList targetLoc = new ArrayList(); // X coord of tree placed in this list
	public static ArrayList firstTurn = new ArrayList(); // X coord of tree the mouse hit or would have hit is placed in this list
	public static int numCorrectTurns;
	public static int trialsSinceMouseStreakEliminated = 3;

	public static int trialDelay;
	public static int numberOfTrials;
}