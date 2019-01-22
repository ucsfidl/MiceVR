﻿using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net.Sockets;
using System.Net;
using GoogleSheetsToUnity;
using System.Collections.Generic;
using UnityEngine.UI;

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

	public static bool hasNotTurned;
	public static List<TimeSpan> trialStartTime = new List<TimeSpan>(); // Holds DateTime object of starttime
	public static List<TimeSpan> trialEndTime = new List<TimeSpan>(); // Holds DateTime object of endtime
	public static List<float> targetLoc = new List<float>(); // X coord of tree placed in this list
	public static List<float> targetHFreq = new List<float>();  // Orientation of target
	public static List<float> targetVFreq = new List<float>();  // Orientation of target
	public static List<float> targetAngle = new List<float>();  // Angle of target
	public static List<float> distractorAngle = new List<float>();  // Angle of distractor
	public static List<float> firstTurn = new List<float>(); // X coord of tree the mouse hit or would have hit is placed in this list
	public static List<float> firstTurnHFreq = new List<float>();  // Orientation of the tree the mouse chose
	public static List<float> firstTurnVFreq = new List<float>();  // Orientation of the tree the mouse chose
	public static List<float> firstTurnAngle = new List<float>();  // Angle of target
	public static List<int> worldID = new List<int>();  // Which world, in a multi-world scenario, was shown on this trial
    public static int numCorrectTurns;

	public static int trialDelay;

	public static float centralViewVisibleShift; // Set in gameconfig file

    public static int rewardDur;  // duration in ms of a single drop
    public static float rewardSize;  // what the above duration results in in ul
    public static float totalRewardSize = 1000;  // total amount the mouse can be given, in ul
	public static ArrayList sizeOfRewardGiven = new ArrayList(); // in ul

    public static string gameType = "detection";  // Default: detection - The type of game, as specified in the scenario file and detected by Loader
    public static string gameTurnControl = "yaw"; // Default: yaw - Whether to use YAW or ROLL for game turning control on the ball
    public static bool varyOrientation = false;
	public static float rewardedHFreq;
	public static float rewardedVFreq;
	public static float rewardedAngle;
	public static float distractorHFreq;
	public static float distractorVFreq;
	public static List<float> distractorAngles = new List<float> ();

	public static bool randomPhase = false;

	public static Color distColor1;
	public static Color distColor2;

    public static MovementRecorder mRecorder = GameObject.Find("FPSController").GetComponent<MovementRecorder>();
    public static DateTime gameStartTime;
	public static DateTime gameEndTime = new DateTime();

	public static bool biasCorrection = true;  // default to true if nothing entered in scenario file
	public static float probeLocX = float.NaN;
	public static bool perim = false;  // default to false, as perimetry is only running in special circumstances
	public static int perimScale;  // 0 = full scale, 1 = 40x40 deg, 2 positions; 2=40x20 deg, 3 positions; 3=20x20 deg, 6 position; 4=10x10 deg, 12 of 24 positions.  Note a larger number also includes the scales smaller than itself.
									// v1 perim scale: 0 = full scale, 1 = 40x40 deg, 2 positions; 2=20x20 deg, 6 position; 3=10x10 deg, 12 of 24 positions
	public static bool perimRange = true;  // True = all sizes larger than the specified will also be presented, while false means only the size specified will be presented

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

	public static float NULL = 1000000;

	public static bool treesBelowGround = false;  // New position of trees so they project down into the ground, to only present stimuli in the lower portion of the visual field (+20 to -30 degrees)

	public static int blockSize = -1;  // Indicates the number of trials over which the ratio of stimuli presentation is guaranteed to hit a target; -1 indicates no blockSize specified, so flip a coin on each trial
	public static int[] precompTrialBlock;
	public static bool presoFracSpecified = false;
	public static float probReward = 1;
	public static int[] precompOptoBlock;  // Indicates optogenetic state on each trial - used to limit light exposure instead of alternating each trial - used if optoAlternation is set to false in the scenario

	public static float speedAdjustment = 1;  // Used to adjust speed on a per scenario basis instead of a per-rig basis, so I can have multiple speed mice running at the same time

	public static bool correctionTrialsEnabled = true;  // As of 1/19/19, correction trials are on by default, just like bias correction
	public static int lastTrialWasIncorrect = 0;  // Indicates that the current trial will be a correction trial, if correction trials are enabled
	public static List<int> correctionTrialMarks = new List<int>();  // For each trial, holds a bool which tracks whether that trial was a correction trial or not

	public static int numNonCorrectionTrials;  // Initialized in code to 1
	public static int numCorrectionTrials = 0;  // Used to count number of correction trials, which is recorded in the stats file

	public struct fov {
		public float nasalBound;
		public float tempBound;
		public float highBound;
		public float lowBound;
	}

	public static fov[] fovs;  // For automatic perimetry
	public static int[] fovsForPerimScaleInclusive = new int[] {1, 3, 6, 12, 24}; // v1 {1, 3, 9, 21}

	// Support for different worlds on each trial
	public struct Tree {
		public bool water;
		public Vector3 pos;
		public float deg_LS;
		public float angle_LS;
		public bool texture;
		public int restrictToCamera;
		public float vFreq;
		public float hFreq;
		public float rewardSize;
		public float rewardMulti;
		public bool respawn;
		public Vector3 rot;
		public Vector3 scale;
		public int rank;
		public string materialName;
		public string type;
		public float presoFrac;
		public float opacity;
	}
	public static GameObject treeParent = GameObject.Find("Trees");

	public struct Wall {
		public Vector3 pos;
		public Vector3 rot;
		public Vector3 scale;
	}
	public static GameObject wallParent = GameObject.Find("Walls");

	public struct World {
		public List<Tree> trees;
		public List<Wall> walls;
		public string gameType;

		// Keep track of actual reward rate at each stimulus location
		public List<int> numRewardsAtStimLoc;
		public List<int> numTurnsToStimLoc;
	}

	public static List<World> worlds;  // For SDT where there are different worlds per level that vary trial-by-trial
	public static bool alternateWorlds = false;  // if set, worlds are presented in a fixed sequence (as found in the file) - if unset, worlds are presented at random

	public static GameObject waterTreePrefab = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/WaterTree"));
	public static GameObject waterTreeCricketsPrefab = (GameObject)GameObject.Instantiate (Resources.Load ("Prefabs/WaterTreeCrickets"));
	public static GameObject waterTreeCricketsWidePrefab = (GameObject)GameObject.Instantiate (Resources.Load ("Prefabs/WaterTreeCricketsWide"));
	public static GameObject waterTreeFlatPrefab = (GameObject)GameObject.Instantiate (Resources.Load ("Prefabs/WaterTreeFlat"));
	public static GameObject dryTreePrefab = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/DryTree"));
	public static GameObject wallPrefab = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Wall"));
	public static GameObject waterAngularTreePrefab = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/waterAngularTree"));
	public static GameObject dryAngularTreePrefab = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/dryAngularTree"));
	public static bool waterTraining;
	public static string waterTextureFile_LS, dryTextureFile_LS;
	public static bool waterTop_LS, waterBot_LS, dryTop_LS, dryBot_LS, waterDouble_LS, waterSpherical_LS, dryDouble_LS, drySpherical_LS;

	public static float presoRatio;  // By default, do dynamic training, varying the reward size to break choice or motor biases

	public static int optoSide = -1;       // 0 = left, 1 = right, 2 = both
	public static float optoFraction;  // fraction of trials in which the light will randomly be on
	public static bool optoAlternation = false;  // Used to force the light to be on on every other trial
	public static int optoState = -1;     // 0 = left, 1 = right, 2 = both
	public const int optoOff = -1;   //  Value used elsewhere to indicate optoOff - never changed

	public static int probeIdx = -1;  // This is the tree index of the rarest tree, also considered the probe tree
	public static bool probeLastOpto = false;  // Used to force the light on on every other probe trial

	public static bool treeMarkers = false;  // Forces drawing a black circle underneath where the trees normally are
	public static bool probabilisticWhiteNoiseWhenNoReward = false;

	public static void AddTreeToWorld(int worldNum, bool water, Vector3 pos, float deg_LS, float angle_LS, bool texture, int restrictToCamera, float vFreq, float hFreq, float rewardSize, float rewardMulti, 
		bool respawn, Vector3 rot, Vector3 scale, int rank, string materialName, string type, float presoFrac, float opacity) {
		World w = GetWorld (worldNum);

		Tree t = new Tree();
		t.water = water;
		t.pos = pos;
		if (deg_LS != NULL) {
			t.deg_LS = deg_LS;
		}
		if (angle_LS != NULL) {
			t.angle_LS = angle_LS;
		}
		t.texture = texture;
		if (restrictToCamera != NULL) {
			t.restrictToCamera = restrictToCamera;
		} else {
			t.restrictToCamera = -1;  // -1 means unrestricted
		}
		if (vFreq != NULL) {
			t.vFreq = vFreq;
		}
		if (hFreq != NULL) {
			t.hFreq = hFreq;
		}
		if (rewardSize != NULL) {
			t.rewardSize = rewardSize;
		}
		if (rewardMulti != NULL) {
			t.rewardMulti = rewardMulti;
		}
		t.respawn = respawn;
		t.scale = scale;
		t.rot = rot;
		if (rank != NULL) { 
			t.rank = rank;  // Used when getting trees to order them
		} else {
			t.rank = w.trees.Count;
		}
		t.materialName = materialName;
		t.type = type;
		t.presoFrac = presoFrac;
		t.opacity = opacity;

		w.trees.Add (t);

		w.numRewardsAtStimLoc.Add (0);
		w.numTurnsToStimLoc.Add (0);

		AddWorldToWorldList (w);
	}

	public static void AddWorldToWorldList(World w) {
		int idx = worlds.IndexOf (w);
		if (idx != -1) {
			worlds [idx] = w;
		} else {
			worlds.Add (w);
		}
	}

	public static void AddWallToWorld(int worldNum, Vector3 v, Vector3 rot, Vector3 scale) {
		World w = GetWorld (worldNum);

		Wall wall;
		wall.pos = v;
		wall.rot = rot;
		wall.scale = scale;
		w.walls.Add (wall);

		AddWorldToWorldList (w);
	}

	public static void AddGameTypeToWorld(int worldNum, string gameType) {
		World w = GetWorld (worldNum);
		w.gameType = gameType;
		AddWorldToWorldList (w);
	}

	public static string GetGameType(int worldNum) {
		World w = GetWorld (worldNum);
		return w.gameType;
	}

	private static World GetWorld(int worldNum) {
		if (worlds == null) {
			worlds = new List<World>();
		}

		World w = new World();  // creates empty world
		if (worlds.Count > worldNum) {
			w = worlds [worldNum];  // overwrite with an existing world
		} else {  // Init one world object
			w.trees = new List<Tree> ();
			w.walls = new List<Wall> ();
			w.numRewardsAtStimLoc = new List<int> ();
			w.numTurnsToStimLoc = new List<int> ();
		}

		return w;
	}

	public static World GetCurrentWorld() {
		return GetWorld (worldID [worldID.Count - 1]);
	}

	public static GameObject[] GetTrees() {
		GameObject[] gos = new GameObject[treeParent.transform.childCount];
		for (int i = 0; i < treeParent.transform.childCount; i++) {
			gos [i] = treeParent.transform.GetChild (i).gameObject;
		}
		return gos;
	}
		


	// Helper that gets all trees across all worlds for this level, removing duplicates (multiple trees at the exact same location)
	// New design is that each world has its own accuracy stats, which is displayed when that world is rendered onscreen
	// So this function is DEPRECATED and no longer used.  Keeping it around in case I need it later for some reason.
	public static List<Tree> GetAllTrees() {
		List<Tree> allTrees = new List<Tree> ();
		bool stillNew = true;
		if (worlds != null) {
			foreach (World w in worlds) {
				List<Tree> treeCandidates = w.trees;
				foreach (Tree tNew in treeCandidates) {
					foreach (Tree tOld in allTrees) {
						if (tNew.pos.x == tOld.pos.x) {
							stillNew = false;  // Tree is already in the list, so don't add it
							break;
						}
					}
					if (stillNew) {  // Manage if the trees are read it out of rank order
						while (allTrees.Count < tNew.rank) {
							allTrees.Add (new Tree ());
						}
						if (allTrees.Count != tNew.rank) {
							allTrees.RemoveAt (tNew.rank);
						}
						allTrees.Insert (tNew.rank, tNew);
					}
				}
			}
		}
		return allTrees;
	}

	public static GameObject[] GetWalls() {
		GameObject[] gos = new GameObject[wallParent.transform.childCount];
		for (int i = 0; i < wallParent.transform.childCount; i++) {
			gos [i] = wallParent.transform.GetChild (i).gameObject;
		}
		return gos;
	}

	// Clear the world so that it can be re-rendered
	public static void ClearWorld() {
		for (int i = 0; i < treeParent.transform.childCount; i++) {
			GameObject.Destroy(treeParent.transform.GetChild (i).gameObject);
		}
		treeParent.transform.DetachChildren();

		for (int i = 0; i < wallParent.transform.childCount; i++) {
			GameObject.Destroy(wallParent.transform.GetChild (i).gameObject);
		}
		wallParent.transform.DetachChildren();
	}

	// Re-render the world, which will occur on each trial
	public static int RenderWorld(int worldNum) {
		if (worldNum == -1) {  // choose a random world to create
			worldNum = UnityEngine.Random.Range(0, worlds.Count);
		}
		//Debug.Log ("world #" + worldNum);

		// Now, actually render the new world
		if (Globals.treesBelowGround) {
			GameObject.FindObjectOfType<Terrain>().enabled = false; // Disable the terrain, so the trees can be seen below ground
		}

		World w = worlds [worldNum];
		GameObject go;
		foreach (Tree t in w.trees) {
			if (t.water) {
				if (t.deg_LS != null) {
					if (t.materialName.Equals ("crickets")) {
						go = GameObject.Instantiate (waterTreeCricketsPrefab, t.pos, Quaternion.identity);
					} else if (t.materialName.Equals ("cricketsWide")) {
						go = GameObject.Instantiate (waterTreeCricketsWidePrefab, t.pos, Quaternion.identity);
					} else if (t.type.Equals("flat")) {
						go = GameObject.Instantiate (waterTreeFlatPrefab, t.pos, Quaternion.identity);
					} else {  // default is the grating on a cylinder
						go = GameObject.Instantiate (waterTreePrefab, t.pos, Quaternion.identity);
					}
					go.GetComponent<WaterTreeScript> ().SetShaderRotation (t.deg_LS);
					go.GetComponent<WaterTreeScript> ().SetForTraining (waterTraining);
					go.GetComponent<WaterTreeScript> ().SetPresoFrac (t.presoFrac);
					go.transform.eulerAngles = t.rot;
					go.transform.localScale += t.scale;
					go.transform.parent = treeParent.transform;
					go.isStatic = true;
					go.GetComponent<WaterTreeScript> ().SetOpacity (t.opacity);
					//go.SetActive (false);
					// Implements field restriction of a tree to just one side screen
					if (t.restrictToCamera != -1) {
						if (t.pos.x == 20000) {
							Debug.Log (t.restrictToCamera);
						}
						if (t.restrictToCamera == 0) {
							go.layer = LayerMask.NameToLayer ("Left Visible Only");
							foreach (Transform tr in go.transform) {
								tr.gameObject.layer = LayerMask.NameToLayer ("Left Visible Only");
								tr.gameObject.AddComponent<SetRenderQueue> ();
							}
						} else if (t.restrictToCamera == 2) {
							go.layer = LayerMask.NameToLayer ("Right Visible Only");
							foreach (Transform tr in go.transform) {
								tr.gameObject.layer = LayerMask.NameToLayer ("Right Visible Only");
								tr.gameObject.AddComponent<SetRenderQueue> ();
							}
						}
					}

					if (t.hFreq != null && t.vFreq != null) {
						go.GetComponent<WaterTreeScript>().SetShader(t.hFreq, t.vFreq, t.deg_LS);
					}

					if (t.rewardSize != 0) {  // 0 is the default reward value if nothing entered in scenario file; RewardSize trumps rewardMulti if both are set
						go.GetComponent<WaterTreeScript>().SetRewardSize(t.rewardSize);
						go.GetComponent<WaterTreeScript> ().SetRewardMulti (0);
					} else { // If no reward size is explicitly specified in the scenario file, then check to see if a reward multiplier is specified
						go.GetComponent<WaterTreeScript>().SetRewardSize(Globals.rewardSize);
						go.GetComponent<WaterTreeScript> ().SetRewardMulti (t.rewardMulti);
					}

					go.GetComponent<WaterTreeScript>().SetRespawn(t.respawn);
				} else if (t.texture) {
					go = GameObject.Instantiate(waterTreePrefab, t.pos, Quaternion.identity);
					go.GetComponent<WaterTreeScript>().ChangeTexture(LoadPNG(waterTextureFile_LS));
					go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
					go.transform.parent = treeParent.transform;
					go.isStatic = true;
					go.SetActive(false);
				} else if (t.angle_LS != null) {   
					if (waterBot_LS) {
						go = GameObject.Instantiate(waterAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("single");
						go.GetComponent<AngularTreeScript>().ChangeBottomRing(t.angle_LS);
						go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					} else if (waterTop_LS) {
						go = GameObject.Instantiate(waterAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("single");
						go.GetComponent<AngularTreeScript>().ChangeTopRing(t.angle_LS);
						go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					} else if (waterDouble_LS) {
						go = GameObject.Instantiate(waterAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("double");
						go.GetComponent<AngularTreeScript>().ChangeBottomRing(t.angle_LS);
						go.GetComponent<AngularTreeScript>().ChangeTopRing(t.angle_LS);
						go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					} else if (waterSpherical_LS) {
						go = GameObject.Instantiate(waterAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("spherical");
						go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(t.angle_LS));
						go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					}
				}
			} else {
				if (t.deg_LS != null) {
					go = GameObject.Instantiate(dryTreePrefab, t.pos, Quaternion.identity);
					go.GetComponent<DryTreeScript>().SetShaderRotation(t.deg_LS);
					go.transform.parent = treeParent.transform;
					go.isStatic = true;
					go.SetActive(false);
				} else if (t.texture) {
					go = GameObject.Instantiate(dryTreePrefab, t.pos, Quaternion.identity);
					go.GetComponent<DryTreeScript>().ChangeTexture(LoadPNG(dryTextureFile_LS));
					go.transform.parent = treeParent.transform;
					go.isStatic = true;
					go.SetActive(false);
				} else if (t.angle_LS != null) {
					if (dryBot_LS) {
						go = GameObject.Instantiate(dryAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("single");
						go.GetComponent<AngularTreeScript>().ChangeBottomRing(t.angle_LS);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					} else if (dryTop_LS) {
						go = GameObject.Instantiate(dryAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("single");
						go.GetComponent<AngularTreeScript>().ChangeTopRing(t.angle_LS);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					} else if (dryDouble_LS) {
						go = GameObject.Instantiate(dryAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("double");
						go.GetComponent<AngularTreeScript>().ChangeBottomRing(t.angle_LS);
						go.GetComponent<AngularTreeScript>().ChangeTopRing(t.angle_LS);
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					} else if (drySpherical_LS) {
						go = GameObject.Instantiate(dryAngularTreePrefab, t.pos, Quaternion.identity);
						go.GetComponent<AngularTreeScript>().ShapeShift("spherical");
						go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(t.angle_LS));
						go.transform.parent = treeParent.transform;
						go.isStatic = true;
						go.SetActive(false);
					}
				}
			}
		}

		foreach (Wall wall in w.walls) {
			go = GameObject.Instantiate(wallPrefab, wall.pos, Quaternion.identity);
			go.transform.eulerAngles = wall.rot;
			go.transform.localScale += wall.scale;
			go.isStatic = true;
			go.transform.parent = wallParent.transform;
			/*
			Color c = go.GetComponent<MeshRenderer> ().material.color;
			Debug.Log (c.a);
			c.a = 0.3F;
			go.GetComponent<MeshRenderer> ().material.color = c;
			Debug.Log (go.GetComponent<MeshRenderer> ().material.color.a);
			*/
		}

		return worldNum;
	}

	// Initializes fovs used for automated perimetry.  There are 4 scales, and the stimuli are presented that fill (or half-fill, for the smallest) scale
	// TODO: don't do linear calculations, but trigometric ones, which are accurate
	public static void InitFOVs() {
		fovs = new fov[fovsForPerimScaleInclusive[fovsForPerimScaleInclusive.Length-1]];

		// FULL FIELD
		int curPos = 0;
		fovs [curPos].nasalBound = defaultVisibleNasalBoundary;
		fovs [curPos].tempBound = defaultVisibleTemporalBoundary;
		fovs [curPos].lowBound = defaultVisibleLowBoundary;
		fovs [curPos].highBound = defaultVisibleHighBoundary;

		// 40x40 : YELLOW in diagram, except shifted medially 10 degrees
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

		// 40x20 : black pen in diagram, added in v2 of perimetry because perim1 was doable but perim2 was difficult for many mice - this is an intermediate level
		for (int i = 0; i < 3; i++) {
			curPos = curPos + 1;
			fovs [curPos].nasalBound = 10 + i % 3 * 20;
			fovs [curPos].tempBound = fovs [curPos].nasalBound + 20;
			fovs [curPos].lowBound = 5;
			fovs [curPos].highBound = fovs [curPos].lowBound + 40;
		}

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
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("actionFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", false);
        // Write file header
        turnsFile.WriteLine("#TrialStartTime\tTrialEndTime\tTrialEndFrame\tTrialDur\tTargetLocation\tTargetHFreq\tTargetVFreq\tNasalBound\tTemporalBound\tHighBound\tLowBound\tTurnLocation\tTurnHFreq\tTurnVFreq\tRewardSize(ul)\tWorldNum\tOptoState\tTargetAngle\tTurnAngle\tDistractorAngle\tCorrectionTrial");
        turnsFile.Close();
    }

    public static void WriteToLogFiles() {
        StreamWriter turnsFile = new StreamWriter(PlayerPrefs.GetString("actionFolder") + "/" + mRecorder.GetReplayFileName() + "_actions.txt", true);
        // Write out turn decisions over time - easy to import into Excel and analyze

        Debug.Log(trialStartTime[trialStartTime.Count - 1] + "\t" +
                    trialEndTime[trialEndTime.Count - 1] + "\t" +
					currFrame + "\t" + 
                    (trialEndTime[trialEndTime.Count - 1]).Subtract(trialStartTime[trialStartTime.Count - 1]) + "\t" +
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
                    (float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]) + "\t" + 
					worldID[worldID.Count - 1] + "\t" + 
					optoState + "\t" + 
					targetAngle[targetAngle.Count - 1] + "\t" + 
					firstTurnAngle[firstTurnAngle.Count - 1] + "\t" +
					distractorAngle[distractorAngle.Count - 1] + "\t" +
					correctionTrialMarks[correctionTrialMarks.Count - 1]);

        turnsFile.WriteLine(trialStartTime[trialStartTime.Count - 1] + "\t" +
                    trialEndTime[trialEndTime.Count - 1] + "\t" +
					currFrame + "\t" + 
                    (trialEndTime[trialEndTime.Count - 1]).Subtract(trialStartTime[trialStartTime.Count - 1]) + "\t" +
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
					(float)System.Convert.ToDouble(sizeOfRewardGiven[sizeOfRewardGiven.Count - 1]) + "\t" + 
					worldID[worldID.Count - 1] + "\t" + 
					optoState + "\t" + 
					targetAngle[targetAngle.Count - 1] + "\t" + 
					firstTurnAngle[firstTurnAngle.Count - 1] + "\t" +
					distractorAngle[distractorAngle.Count - 1] + "\t" +
					correctionTrialMarks[correctionTrialMarks.Count - 1]);
		
        turnsFile.Close();
        WriteStatsFile();
    }

    public static void WriteStatsFile()
    {
        StreamWriter statsFile = new StreamWriter(PlayerPrefs.GetString("actionFolder") + "/" + mRecorder.GetReplayFileName() + "_stats.txt");
        statsFile.WriteLine("<document>");
        statsFile.WriteLine("\t<stats>");
		statsFile.WriteLine("\t\t<accuracy>" + Math.Round((float)numCorrectTurns / ((float)numNonCorrectionTrials - 1) * 100) + "%" + GetTreeAccuracy(false) + "</accuracy>  <!-- nonCorrection trials only -->");
        // TODO - Fix off by one error when mouse finishes game!
        statsFile.WriteLine("\t\t<numEarnedRewards>" + numCorrectTurns + "</numEarnedRewards>");
        statsFile.WriteLine("\t\t<numUnearnedRewards>" + numberOfUnearnedRewards + "</numUnearnedRewards>");
		statsFile.WriteLine("\t\t<numNonCorrectionTrials>" + (numNonCorrectionTrials - 1) + "</numNonCorrectionTrials>");
		statsFile.WriteLine("\t\t<numCorrectionTrials>" + numCorrectionTrials + "</numCorrectionTrials>");
		statsFile.WriteLine("\t\t<numAllTrials>" + (numNonCorrectionTrials - 1 + numCorrectionTrials) + "</numAllTrials>");

		TimeSpan te;
		if (gameEndTime == DateTime.MinValue) // Game is not over
			te = DateTime.Now.Subtract (gameStartTime);
		else
			te = gameEndTime.Subtract(gameStartTime);
		statsFile.WriteLine("\t\t<timeElapsed>" + string.Format("{0:D2}:{1:D2}", te.Hours * 60 + te.Minutes, te.Seconds) + "</timeElapsed>");

        float totalEarnedRewardSize = 0;
        for (int i = 0; i < sizeOfRewardGiven.Count; i++)
        {
            totalEarnedRewardSize += (float)System.Convert.ToDouble(sizeOfRewardGiven[i]);
        }

        statsFile.WriteLine("\t\t<totalRewardSizeReceived>" + (totalEarnedRewardSize + (float)Globals.numberOfUnearnedRewards * rewardSize).ToString() + "</trials>");
		statsFile.WriteLine ("\t\t<rewardRates>" + PrintRewardRates () + "</rewardRates>");
        statsFile.WriteLine("\t</stats>");
        statsFile.WriteLine("</document>");
        statsFile.Close();
    }

	// Write the data to Google Sheets so that the experimenter does not need to memorize and type in results, which is prone to error
	public static bool WriteStatsToGoogleSheet() {
		bool tryAgain = true;
		while (tryAgain) {
			try {
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
								{ "trials", (numNonCorrectionTrials - 1).ToString () },
								{ "tmin", string.Format ("{0:N1}", (numNonCorrectionTrials - 1) / numMinElapsed) },
								{ "accuracy", Math.Round ((float)numCorrectTurns / ((float)numNonCorrectionTrials - 1) * 100) + "%" },
								{ "earned", Math.Round (totalEarnedRewardSize).ToString () },
								{ "uball", Math.Round ((float)numberOfUnearnedRewards * rewardSize).ToString () },
								{ "results", Math.Round ((float)numCorrectTurns / ((float)numNonCorrectionTrials - 1) * 100) + GetTreeAccuracy (false) },
								{ "totalh2o", "=V" + (row + 1) + "+X" + (row + 1) }
							}
							);

							break;
						}
					}
					Debug.Log ("wrote to worksheet " + mouseName);
					return true;
				}
			} catch (SocketException se) {
				Debug.Log ("Socket exception thrown in Google Sheets writing - try to connect again!");
			} catch (WebException we) {
				Debug.Log ("Web exception thrown in Google Sheets writing - try to connect again!");
			}
		}
		return true; // dummy line for the compiler - the code should never get here!
	}

	// Now supports multiple worlds
	public static string PrintRewardRates() {
		string output = "";

		// Iterate through each world, get the tree positions for each world (the x pos functions as its ID), and then get the relevant stats
		foreach (World w in worlds) {
			for (int i = 0; i < w.trees.Count; i++) {
				if (i > 0) {
					output += "/";
				}
				if (w.numTurnsToStimLoc [i] > 0) {
					output += Math.Round ((float)w.numRewardsAtStimLoc [i] / (float)w.numTurnsToStimLoc [i], 2);
				} else {
					output += "NaN";
				}
			}
			output += "//";
		}

		return output;
	}

	// Now supports treeAccuracy per world or for all worlds
	public static string GetTreeAccuracy(bool currWorldOnly) {
		string output = " // ";

		for (int i = 0; i < worlds.Count; i++) {
			if (currWorldOnly && worldID [worldID.Count - 1] != i) {
				continue;
			}
			World w = worlds [i];
			int tCount = w.trees.Count;
			float[] locs = new float[tCount];
			int[] numCorrTrials = new int[tCount]; 
			int[] numTrials = new int[tCount]; 
			for (int j = 0; j < tCount; j++) {
				locs [j] = w.trees[j].pos.x;
			}


			// With all locations in hand, calculate accuracy for each one, then print it out
			int idx;
			for (int j = 0; j < Globals.firstTurn.Count; j++) {
				idx = Array.IndexOf(locs, Globals.targetLoc[j]);
				// Added support for ignoring correction trials
				// Check for world-matching again here, as results from different worlds will be intermingled in the in-memory logs
				if (i == worldID[j] && (!correctionTrialsEnabled || (correctionTrialsEnabled && correctionTrialMarks[j] == 0))) {
					numTrials [idx]++;
					//Debug.Log("this-world trial");
					if (Globals.targetLoc [j] == Globals.firstTurn [j]) {
						numCorrTrials [idx]++;
						//Debug.Log ("correct trial");
					}
				}
			}

			for (int j = 0; j < numTrials.Length; j++) {
				output += Math.Round((float)numCorrTrials[j] / numTrials[j] * 100);
				if (j < numTrials.Length - 1)
					output += "/";
			}

			if (i < worlds.Count -1)
				output += " // ";
		}

		return output;
    }
		
    // This bias correction algorithm can be used to match Harvey et al publication, where probability continuously varies based on mouse history on last 20 trials
    // Previous attempt at streak elimination didn't really work... Saw mouse go left 100 times or so! And most mice exhibited a bias, even though they may not have before...
	// If there are multiple worlds in this scenario, then only return the turn bias for that world, returning chance if this world has not been displayed yet.  histLen covers just the current world type, not all worlds.
    public static float GetTurnBias(int histLen, int treeIndex) {
		GameObject[] gos = GetTrees();
		int currWorldID = worldID [worldID.Count - 1];
		List<int> validTrials = new List<int> ();

		Debug.Log ("currworld=" + currWorldID);

		// First, collect trials that correspond to this world AND were not correction trials, until you either run out or have collected histLen
		for (int i = firstTurn.Count-1; i >= 0; i--) {  // must be firstTurn.Count!
			if (worldID [i] == currWorldID && (!correctionTrialsEnabled || (correctionTrialsEnabled && correctionTrialMarks[i] == 0))) {
				validTrials.Add(i);
			}
			if (validTrials.Count == histLen) {
				break;
			}
		}

		// If no trial history found, return the uniform distribution as the prior
		if (validTrials.Count == 0) {
			Debug.Log ("no trial history found");
			return (1F / gos.Length);
		}

		// Check and see if treeIndex is a valid option; if not, return 0 - gives more flexibility when calling, e.g. from WaterTreeScript OnTriggerEnter
		if (treeIndex >= gos.Length) {
			return 0F;
		}

		// Now, with valid trials in hand, calculate the turn bias
		int turn0 = 0;
		foreach (int idx in validTrials) {
			Debug.Log (firstTurn [idx]);
			if (firstTurn [idx] == gos [treeIndex].transform.position.x) {
				turn0++;
			}
		}
		Debug.Log (turn0);
		Debug.Log (validTrials.Count);
		return (float)turn0 / validTrials.Count;
    }

	public static bool CurrentWorldHasAlreadyAppeared() {
		bool worldSeen = false;
		if (worldID.Count > 0) {
			int currWorldID = worldID [worldID.Count - 1];
			for (int i = 0; i < worldID.Count - 1; i++) {
				if (worldID [i] == currWorldID) {
					worldSeen = true;
					break;
				}
			}
		}
		return worldSeen;
	}

	public static bool CurrentlyCorrectionTrial() {
		if (correctionTrialsEnabled && lastTrialWasIncorrect == 1)
			return true;
		return false;
	}

	// Modified to support multi-worlds in a single scenario
    public static float GetLastAccuracy(int n) {
		int currWorld = worldID [worldID.Count - 1];
		List<int> validTrials = new List<int> ();

		// First, collect trials that correspond to this world AND were not correction trials, until you either run out or have collected n
		for (int i = firstTurn.Count-1; i >= 0; i--) {
			//Debug.Log (correctionTrialMarks [i]);
			if (worldID [i] == currWorld && (!correctionTrialsEnabled || (correctionTrialsEnabled && correctionTrialMarks[i] == 0))) {
				validTrials.Add(i);
			}
			if (validTrials.Count == n) {
				break;
			}
		}
			
		int curTrial;
		int corr = 0;
		for (int i = 0; i < validTrials.Count; i++) {
			curTrial = (int)validTrials [i];
			if (firstTurn [curTrial] == targetLoc [curTrial])
				corr++;
		}

		return (float)corr / validTrials.Count;
    }

	// Calculate tree view block value: 0 is full occlusion in the central screen = 120 degrees
	// 0.9 is full visibility with occluder pushed all the way to the screen
	public static void SetCentrallyVisible(int deg) {
		Globals.centralViewVisibleShift = (float)(deg * 0.58/120);  // 0.45/120
		Globals.inputDeg = deg;
	}
		
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
		GameObject tolt = GameObject.Find("TreeOccluderLT");     // Left visible only layer
		GameObject tolmt = GameObject.Find("TreeOccluderLMT");   // Left visible only layer
		GameObject tolmn = GameObject.Find("TreeOccluderLMN");   // Left visible only layer
		GameObject tolb = GameObject.Find("TreeOccluderLB");     // Left visible only layer
		GameObject toct = GameObject.Find("TreeOccluderCT");     // Center visible only layer
		GameObject tocmt = GameObject.Find("TreeOccluderCMT");   // Center visible only layer
		GameObject tocmn = GameObject.Find("TreeOccluderCMN");   // Center visible only layer
		GameObject tocb = GameObject.Find("TreeOccluderCB");     // Center visible only layer
		GameObject tort = GameObject.Find("TreeOccluderRT");     // Right visible only layer
		GameObject tormt = GameObject.Find("TreeOccluderRMT");   // Right visible only layer
		GameObject tormn = GameObject.Find("TreeOccluderRMN");   // Right visible only layer
		GameObject torb = GameObject.Find("TreeOccluderRB");     // Right visible only layer

		// Local vars to store calculations to reuse
		// I don't think I need these anymore, as this was when I thought the stimulus window would be centered around the horizon.  Now it can be offset, so something else needs to be done
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

	public static Texture2D LoadPNG(string filePath) {
		Texture2D tex = null;
		byte[] fileData;

		if (File.Exists(filePath)) {
			fileData = File.ReadAllBytes(filePath);
			tex = new Texture2D(128, 128);
			tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
		}
		return tex;
	}

	public static float SphereAngleRemap(float f) {
		float OldMax = 45;
		float OldMin = 0;
		float NewMax = 10;
		float NewMin = 0;
		float OldValue = f;

		float OldRange = (OldMax - OldMin);
		float NewRange = (NewMax - NewMin);
		float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
		return NewValue;
	}

	// Assumes get info for current world - add param if need more control
	public static float GetActualRewardRate(float xPos) {
		int idx = GetIdxOfStimLoc (xPos);
		World w = GetWorld (worldID [worldID.Count - 1]);

		if (w.numTurnsToStimLoc[idx] > 0)
			return (float)w.numRewardsAtStimLoc [idx] / (float)w.numTurnsToStimLoc [idx];
		else
			return 0;
	}
		
	public static float GetNumRewardsAtStimLoc(float xPos) {
		int idx = GetIdxOfStimLoc (xPos);
		World w = GetCurrentWorld ();

		return w.numRewardsAtStimLoc [idx];
	}

	public static float GetNumTurnsToStimLoc(float xPos) {
		int idx = GetIdxOfStimLoc (xPos);
		World w = GetCurrentWorld ();

		return w.numTurnsToStimLoc [idx];
	}

	public static void IncrementRewardAtStimLoc(float xPos) {
		int idx = GetIdxOfStimLoc (xPos);
		World w = GetCurrentWorld ();

		w.numRewardsAtStimLoc [idx] = w.numRewardsAtStimLoc [idx] + 1;
	}

	public static void IncrementTurnToStimLoc(float xPos) {
		int idx = GetIdxOfStimLoc (xPos);
		World w = GetCurrentWorld ();

		w.numTurnsToStimLoc [idx] = w.numTurnsToStimLoc [idx] + 1;
	}

	public static int GetIdxOfStimLoc(float xPos) {
		GameObject[] gos = GetTrees ();
		int idx = -1;
		// Find the index for this tree
		for (int i = 0; i < gos.Length; i++) {
			if (gos [i].gameObject.transform.position.x == xPos) {
				idx = i;
				break;
			}
		}
		return idx;
	}

}