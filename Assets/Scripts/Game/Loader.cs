using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine.UI;
using GoogleSheetsToUnity;

public class Loader : MonoBehaviour {
    public Text errorText;
    public MovementRecorder movementRecorderScript;
    public float dragSpeed = 10f, scrollSpeed = 20f;

    private int waterTrees, dryTrees;
    private float waterMu, waterSigma, waterMinRotationGaussian, waterMaxRotationGaussian;
    private float dryMu, drySigma, dryMinRotationGaussian, dryMaxRotationGaussian;
    private float waterMinRotationUniform, waterMaxRotationUniform, dryMinRotationUniform, dryMaxRotationUniform;
    private int dryUniformSteps, waterUniformSteps;
    private bool waterUniform, waterGaussian, dryUniform, dryGaussian;
    private GameObject spawnedWall, spawnedWaterTree, spawnedDryTree;
    private Vector3 acceleration;
    private bool placingWall, placingWaterTree, placingDryTree;
    private string loadScenarioFile, saveScenarioFile;
    private List<GameObject> waterTreesQueue, dryTreesQueue;

    private Texture2D waterTexture, dryTexture;
    private string waterTextureFile, dryTextureFile;
    private bool waterFixed, dryFixed;
    public Image waterImage, dryImage;

    public bool scenarioLoaded;

    public bool sceneEditing;

    private bool spawnWaterTexture, spawnWaterPattern, spawnDryTexture, spawnDryPattern;
    private float spawnWaterDegree, spawnDryDegree;

    private Texture2D spawnWaterTextureTexture, spawnDryTextureTexture;
    private string spawnWaterTextureFile, spawnDryTextureFile;
    private Image spawnWaterImage, spawnDryImage;

    public bool spawnWaterAngular, spawnWaterAngularBot, spawnWaterAngularTop, spawnDryAngular, spawnDryAngularBot, spawnDryAngularTop;
    public bool waterAngular, dryAngular;
    private float spawnWaterAngularAngle, spawnDryAngularAngle;

    public bool waterAngularTop, waterAngularBot, dryAngularTop, dryAngularBot;
    private float waterAngularAngle, dryAngularAngle;

    private int start, end, inc;
    private bool placed;
    private List<GameObject> treeList;

    public Text activationTimeText, totalTimeText;
    System.DateTime startTime, endTime, generationTime;

    private bool ended, printed, firstRun;

    private Vector2 gridCenter;
    private int gridWidth, gridHeight;

    public GameObject wallButton, waterTreeButton, dryTreeButton;
    private Color buttonColor;

    public Text statusText;

    private bool waterDoubleAngular, dryDoubleAngular, waterSpherical, drySpherical, waterTextured, dryTextured, waterGradient, dryGradient;
    private float waterFixedFloat, dryFixedFloat;

	private bool finishedReadingScenario = false;
	    
    void Start() {
        start = 0;
        inc = 1000;
        end = inc;

        treeList = new List<GameObject>();
    }

    void Update() {
		if (finishedReadingScenario) {
			this.scenarioLoaded = true;
		}
	}

	public void LoadSettingsFromSheets() {
		// Needed to remove focus from the input fields, to fix a bug
		GameObject.Find ("EventSystem").GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (null);

		// First, get each setting from the Sheet
		SpreadSheetManager manager = new SpreadSheetManager();
		GS2U_SpreadSheet spreadsheet = manager.LoadSpreadSheet (Globals.vrGoogleSheetsName);
		GS2U_Worksheet worksheet = spreadsheet.LoadWorkSheet(Globals.mouseName);
		bool foundNewLevel = false;
		if (worksheet == null) {
			GameObject.Find ("ErrorText").GetComponent<Text> ().text = "Worksheet '" + Globals.mouseName + "' NOT found!";
		} else {
			Debug.Log ("loaded worksheet " + Globals.mouseName);
			WorksheetData data = worksheet.LoadAllWorksheetInformation ();
			Debug.Log (data.rows.Count + " number of rows found");

			for (int i = 0; i < data.rows.Count; i++) {
				//Debug.Log ("Examining row " + i);

				// Find the first row with a blank date and duration, and read the settings from that line - this was buggy as sometimes the date column isn't filled in.
				// Instead, find the first row that has a scenario but no date
				// Note that the Sheets object only captures all rows up until the first empty row!  I have tested and confirmed this.
				if (!data.rows [i].cells [1].value.Equals ("") && data.rows [i].cells [12].value.Equals ("")) {
					Debug.Log ("Criteria met on row " + (i + 2));
					Debug.Log (data.rows [i].cells [1].cellColumTitle + " value is " + data.rows [i].cells [1].value);
					Debug.Log (data.rows [i].cells [12].cellColumTitle + " value is " + data.rows [i].cells [12].value);
					RowData rData = data.rows [i];
					foundNewLevel = true;

					for (int j = 0; j < rData.cells.Count; j++) {
						switch (j) {
						case 0:
							{
								GameObject.Find ("DayOnBallInput").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 1:
							{
								GameObject.Find ("ScenarioInput").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 2:
							{
								GameObject.Find ("ScenarioSessionInput").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 3:
							{
								GameObject.Find ("VisibleNasalBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 4:
							{
								GameObject.Find ("VisibleTemporalBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 5:
							{
								GameObject.Find ("VisibleHighBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 6:
							{
								GameObject.Find ("VisibleLowBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case 10:
							{
								GameObject.Find ("MaxReward").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						}
					}
					break;
				}
			}

			if (!foundNewLevel) { // If no new level entered, just redo yesterday's level
				Debug.Log("did not find a new level");
				for (int i = data.rows.Count-1; i >= 0 ; i--) {  // Start from the bottom and work our way back up
					Debug.Log("Examining row to duplicate: " + i);
					if (!data.rows [i].cells [1].value.Equals ("") && !data.rows [i].cells [12].value.Equals ("")) {
						Debug.Log ("Duplicate row identified on row " + (i + 2));
						break;
					}

				}
			}
		}
	}

	// Read and load the scenario text file into memory
	// NEW: Supports the <include> directive to essentially make a templating system.  Each <include>FILE</include> is read in as an XML doc and searched for certain xml nodes
	public void LoadScenario() {
		// Needed to remove focus from the button, to fix a bug
		GameObject.Find ("EventSystem").GetComponent<UnityEngine.EventSystems.EventSystem> ().SetSelectedGameObject (null);

		if (File.Exists(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile)) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(File.ReadAllText(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile, ASCIIEncoding.ASCII)); // load the file.

			// For each includeWorld, add to the <worlds> node in the root parent xml document opened above
			XmlNode xn_worlds = xmlDoc.GetElementsByTagName("worlds").Item(0);  // There is only 1 worlds element
			XmlNodeList includeFileNames = xmlDoc.GetElementsByTagName ("includeWorld");
			foreach (XmlNode xn in includeFileNames) {
				XmlDocument tmpDoc = new XmlDocument (); 
				tmpDoc.LoadXml (File.ReadAllText (PlayerPrefs.GetString ("scenarioFolder") + "/" + xn.InnerXml, ASCIIEncoding.ASCII));
				XmlNodeList xnlWorld = tmpDoc.GetElementsByTagName ("world");
				foreach (XmlNode w in xnlWorld) {
					XmlNode forRoot = xmlDoc.ImportNode (w, true);
					xn_worlds.AppendChild(forRoot);
				}
			}

            XmlNodeList waterConfigList = xmlDoc.SelectNodes("document/config/waterConfig");
            foreach (XmlNode xn in waterConfigList) {
                string waterTrainingXML = xn["training"].InnerText;

                if (waterTrainingXML == "True") {
					Globals.waterTraining = true;
                } else {
                    Globals.waterTraining = false;
                }

                string waterDistType = xn["distType"].InnerText;
                if (waterDistType == "f" && xn["waterTex"] != null) {
                    Globals.waterTextureFile_LS = xn["waterTex"].InnerText;
                }

                if (xn["waterAngular"] != null) {
                    string angularPos = xn["waterAngular"].InnerText;

                    if (angularPos == "top") {
						Globals.waterTop_LS = true;
						Globals.waterBot_LS = false;
						Globals.waterDouble_LS = false;
						Globals.waterSpherical_LS = false;
                    } else if (angularPos == "bot") {
						Globals.waterBot_LS = true;
						Globals.waterTop_LS = false;
						Globals.waterDouble_LS = false;
						Globals.waterSpherical_LS = false;
                    } else if (angularPos == "double") {
						Globals.waterDouble_LS = true;
						Globals.waterTop_LS = false;
						Globals.waterBot_LS = false;
						Globals.waterSpherical_LS = false;
                    } else if (angularPos == "spherical") {
						Globals.waterSpherical_LS = true;
						Globals.waterDouble_LS = false;
						Globals.waterTop_LS = false;
						Globals.waterBot_LS = false;
                    }
                }
            }

            XmlNodeList dryConfigList = xmlDoc.SelectNodes("document/config/dryConfig");
            foreach (XmlNode xn in dryConfigList) {
                string dryDistType = xn["distType"].InnerText;
                if (dryDistType == "f" && xn["dryTex"] != null) {
                    Globals.dryTextureFile_LS = xn["dryTex"].InnerText;
                }

                if (xn["dryAngular"] != null) {
                    string angularPos = xn["dryAngular"].InnerText;
                    if (angularPos == "top") {
						Globals.dryTop_LS = true;
						Globals.dryBot_LS = false;
						Globals.dryDouble_LS = false;
						Globals.drySpherical_LS = false;
                    } else if (angularPos == "bot") {
						Globals.dryBot_LS = true;
						Globals.dryTop_LS = false;
						Globals.dryDouble_LS = false;
						Globals.drySpherical_LS = false;
                    } else if (angularPos == "double") {
						Globals.dryDouble_LS = true;
						Globals.dryBot_LS = false;
						Globals.dryTop_LS = false;
						Globals.drySpherical_LS = false;
                    } else if (angularPos == "spherical") {
						Globals.drySpherical_LS = true;
						Globals.dryTop_LS = false;
						Globals.dryBot_LS = false;
                        Globals.dryDouble_LS = false;
                    }
                }  
            }

            XmlNodeList gameConfigList = xmlDoc.SelectNodes("document/config/gameConfig");
			List<float> worldPresoFracs = new List<float>();  // local variable to store world preso fracs for use later in this method
			foreach (XmlNode xn in gameConfigList) {
                if (xn["gameType"] != null) {
                    Globals.gameType = xn["gameType"].InnerText;
                }

                if (xn["gameTurnControl"] != null) {
                    string gameTurnControlXML = xn["gameTurnControl"].InnerText;
                    if (gameTurnControlXML.Equals("roll"))
                        Globals.gameTurnControl = gameTurnControlXML;
                }

                if (xn["varyOrientation"] != null) {
                    string varyOrientationXML = xn["varyOrientation"].InnerText;
                    if (varyOrientationXML.Equals("true"))
                        Globals.varyOrientation = true;
                }

                if (xn["rewardedHFreq"] != null) {
                    float.TryParse(xn["rewardedHFreq"].InnerText, out Globals.rewardedHFreq);
                }

                if (xn["rewardedVFreq"] != null) {
                    float.TryParse(xn["rewardedVFreq"].InnerText, out Globals.rewardedVFreq);
                }
				if (xn["rewardedAngle"] != null) {
					float.TryParse(xn["rewardedAngle"].InnerText, out Globals.rewardedAngle);
				}

				if (xn ["distractorHFreq"] != null) {
					float.TryParse(xn["distractorHFreq"].InnerText, out Globals.distractorHFreq);
				}
				if (xn ["distractorVFreq"] != null) {
					float.TryParse(xn["distractorVFreq"].InnerText, out Globals.distractorVFreq);
				}
				if (xn["distractorAngle"] != null) {
					string[] strArr = xn ["distractorAngle"].InnerText.Split (';');
					foreach (string str in strArr) {
						float f;
						float.TryParse (str, out f);
						Globals.distractorAngles.Add (f);
					}
					//float.TryParse (xn["distractorAngle"].InnerText.Split (';') [0], out x);
					//float.TryParse(xn["distractorAngle"].InnerText, out Globals.distractorAngle);
				}

				if (xn["distractorIntensity1"] != null) {
					float i1;
					float.TryParse(xn["distractorIntensity1"].InnerText, out i1);
					Globals.distColor1 = new Color (i1, i1, i1);
				}

				if (xn["distractorIntensity2"] != null) {
					float i2;
					float.TryParse(xn["distractorIntensity2"].InnerText, out i2);
					Globals.distColor2 = new Color (i2, i2, i2);
				}

				if (xn["biasCorrection"] != null) {
					string biasCorrXML = xn["biasCorrection"].InnerText;
					if (biasCorrXML.Equals("false"))
						Globals.biasCorrection = false;
					else
						Globals.biasCorrection = true;
				}

				if (xn["probeLocX"] != null) {
					float.TryParse(xn["probeLocX"].InnerText, out Globals.probeLocX);
				}

				if (xn["perim"] != null) {
					string perimXML = xn["perim"].InnerText;
					if (perimXML.Equals("true"))
						Globals.perim = true;
					else  // Default state is false - perimetry is not running
						Globals.perim = false;
				}
				if (xn["perimScale"] != null) {
					int.TryParse(xn["perimScale"].InnerText, out Globals.perimScale);
				}
				if (xn["perimRange"] != null) {
					string perimRangeXML = xn["perimRange"].InnerText;
					if (perimRangeXML.Equals("false"))
						Globals.perimRange = false;
					else  // Default state is true - range is on
						Globals.perim = true;
				}

				if (xn ["presoRatio"] != null) {
					float.TryParse (xn ["presoRatio"].InnerText, out Globals.presoRatio);
				}
				if (xn ["probeIdx"] != null) {
					string[] probeIdx = xn ["probeIdx"].InnerText.Split(';');
					for (int i = 0; i < probeIdx.Length; i++) {
						int tmp;
						int.TryParse (probeIdx [i], out tmp);
						Globals.probeIdx.Add(tmp);
					}
				}

				if (xn ["probeWorldIdx"] != null) {
					string[] probeWorldIdx = xn ["probeWorldIdx"].InnerText.Split(';');
					for (int i = 0; i < probeWorldIdx.Length; i++) {
						int tmp;
						int.TryParse (probeWorldIdx [i], out tmp);
						Globals.probeWorldIdx.Add(tmp);
					}
				}

				if (xn ["optoSide"] != null) {
					string optoSideXML = xn ["optoSide"].InnerText;
					if (optoSideXML.Equals ("L"))
						Globals.optoSide = Globals.optoL;
					else if (optoSideXML.Equals ("R"))
						Globals.optoSide = Globals.optoR;
					else if (optoSideXML.Equals ("LR") || optoSideXML.Equals("LandR"))
						Globals.optoSide = Globals.optoLandR;
					else if (optoSideXML.Equals ("LorR"))
						Globals.optoSide = Globals.optoLorR;
				}
				if (xn ["optoFraction"] != null) {
					float.TryParse(xn["optoFraction"].InnerText, out Globals.optoFraction);
				}
				if (xn ["optoAlternation"] != null) {
					string optoAltXML = xn["optoAlternation"].InnerText;
					if (optoAltXML.Equals("true"))
						Globals.optoAlternation = true;
					else
						Globals.optoAlternation = false;
				}
				if (xn ["treesBelowGround"] != null) {
					string treesBelowGroundXML = xn["treesBelowGround"].InnerText;
					if (treesBelowGroundXML.Equals("true"))
						Globals.treesBelowGround = true;
					else
						Globals.treesBelowGround = false;
				}
				if (xn ["randomPhase"] != null) {
					string randomPhaseXML = xn["randomPhase"].InnerText;
					if (randomPhaseXML.Equals ("true")) {
						Globals.randomPhase = true;
					} else {
						Globals.randomPhase = false;
					}
				}
				if (xn ["blockSize"] != null) {
					int.TryParse(xn["blockSize"].InnerText, out Globals.blockSize);
				}

				if (xn["presoFracSpecified"] != null) {
					string presoFracSpecifiedXML = xn["presoFracSpecified"].InnerText;
					if (presoFracSpecifiedXML.Equals("true"))
						Globals.presoFracSpecified = true;
					else
						Globals.presoFracSpecified = false;
				}

				if (xn ["probReward"] != null) {  // Specifies probability of reward - normally 1, but can be less than 1 to make mice resilient to errors during blindsight
					float.TryParse(xn["probReward"].InnerText, out Globals.probReward);
				}
				if (xn ["treeMarkers"] != null) {
					string xml = xn["treeMarkers"].InnerText;
					if (xml.Equals("true"))
						Globals.treeMarkers = true;
					else
						Globals.treeMarkers = false;
				}
				if (xn ["speedAdjustment"] != null) {
					float.TryParse(xn["speedAdjustment"].InnerText, out Globals.speedAdjustment);
				}

				if (xn ["rewardDur"] != null) {  // Used by SDT task where reward size is varied, but if the stock reward size is used (60ms or 4ul), than a 2-4x multiplier is too much and the water drop falls onto the ball...
					// Update the rewardSize, assuming linearity, which is a poor approximation
					int newRewardDur = int.Parse(xn["rewardDur"].InnerText);
					Globals.rewardSize = Globals.rewardSize / Globals.rewardDur * newRewardDur;
					Globals.rewardDur = newRewardDur;
				}

				if (xn ["probabilisticWhiteNoiseWhenNoReward"] != null) {
					string xml = xn["probabilisticWhiteNoiseWhenNoReward"].InnerText;
					if (xml.Equals("true"))
						Globals.probabilisticWhiteNoiseWhenNoReward = true;
					else
						Globals.probabilisticWhiteNoiseWhenNoReward = false;
				}

				if (xn ["correctionTrials"] != null) {
					string xml = xn["correctionTrials"].InnerText;
					if (xml.Equals("true"))
						Globals.correctionTrialsEnabled = true;
					else
						Globals.correctionTrialsEnabled = false;
				}

				if (xn ["lightOnDuringITI"] != null) {
					string xml = xn["lightOnDuringITI"].InnerText;
					if (xml.Equals("true"))
						Globals.lightOnDuringITI = true;
					else
						Globals.lightOnDuringITI = false;
				}

				if (xn ["alternateWorlds"] != null) {
					string xml = xn["alternateWorlds"].InnerText;
					if (xml.Equals("true"))
						Globals.alternateWorlds = true;
					else
						Globals.alternateWorlds = false;
				}

				if (xn ["optoTrialsPerBlock"] != null) {
					int.TryParse(xn["optoTrialsPerBlock"].InnerText, out Globals.optoTrialsPerBlock);
				}

				if (xn ["adaptPos"] != null) {
					string xml = xn["adaptPos"].InnerText;
					if (xml.Equals("true"))
						Globals.adaptPos = true;
					else
						Globals.adaptPos = false;
				}
				if (xn ["adaptProCritTrialsPerMin"] != null) {
					float.TryParse(xn["adaptProCritTrialsPerMin"].InnerText, out Globals.adaptProCritTrialsPerMin);
				}
				if (xn ["adaptDemCritTrialsPerMin"] != null) {
					float.TryParse(xn["adaptDemCritTrialsPerMin"].InnerText, out Globals.adaptDemCritTrialsPerMin);
				}
				if (xn ["adaptCritDur"] != null) {
					float.TryParse(xn["adaptCritDur"].InnerText, out Globals.adaptCritDur);
				}
				if (xn ["adaptPosStartIdx"] != null) {
					int.TryParse(xn["adaptPosStartIdx"].InnerText, out Globals.adaptPosStartIdx);
				}
				if (xn ["adaptPosEndIdx"] != null) {
					int.TryParse(xn["adaptPosEndIdx"].InnerText, out Globals.adaptPosEndIdx);
				}

				if (xn ["catchFreq"] != null) {
					float.TryParse(xn["catchFreq"].InnerText, out Globals.catchFreq);
				}
				if (xn ["extinctFreq"] != null) {
					float.TryParse(xn["extinctFreq"].InnerText, out Globals.extinctFreq);
				}
				if (xn ["correctExtinction"] != null) {
					string xml = xn["correctExtinction"].InnerText;
					if (xml.Equals("true"))
						Globals.correctExtinction = true;
					else
						Globals.correctExtinction = false;
				}

				if (xn ["worldBlockSize"] != null) {
					int.TryParse(xn["worldBlockSize"].InnerText, out Globals.worldBlockSize);
				}
				if (xn ["worldPresoFracs"] != null) {
					string[] worldPresoFracsStr = xn["worldPresoFracs"].InnerText.Split (';');
					for (int i = 0; i < worldPresoFracsStr.Length; i++) {
						float tmp;
						float.TryParse (worldPresoFracsStr [i], out tmp);
						worldPresoFracs.Add (tmp);
					}
				}
			}

			XmlNodeList worldList = xmlDoc.GetElementsByTagName("world");
			int worldIdx = 0;
			foreach (XmlElement world in worldList) {
				string gameTypeStr = Globals.gameType;  // For backward compatibility with old scenarios which did not specify the gameType on each world
				float worldPresoFrac = 0;

				XmlNodeList gameTypeNodes = world.GetElementsByTagName ("gameType");
				if (gameTypeNodes.Count == 1) {
					gameTypeStr = gameTypeNodes.Item(0).InnerText;
					//Debug.Log (gameTypeStr);
				}
				if (worldPresoFracs.Count >= worldIdx + 1) {
					worldPresoFrac = worldPresoFracs [worldIdx];
				}
				Globals.AddScalarsToWorld (worldIdx, gameTypeStr, worldPresoFrac);
				//Debug.Log (gameTypeStr);

				XmlNodeList probeIdxNodes = world.GetElementsByTagName ("probeIdx");
				if (probeIdxNodes.Count == 1) {
					string[] probeIdx = probeIdxNodes.Item (0).InnerText.Split (';');
					for (int i = 0; i < probeIdx.Length; i++) {
						int tmp;
						int.TryParse (probeIdx [i], out tmp);
						Globals.AddProbeIdxToWorld (worldIdx, tmp);
					}
				} else { // For backward compatibility with old scenarios which did not specify the probeIdx at the world level
					foreach (int i in Globals.probeIdx) {
						Globals.AddProbeIdxToWorld (worldIdx, i);
					}
				}

				XmlNodeList hiddenIdxNodes = world.GetElementsByTagName ("hiddenIdx");
				if (hiddenIdxNodes.Count == 1) {
					string[] hiddenIdx = hiddenIdxNodes.Item (0).InnerText.Split (';');
					for (int i = 0; i < hiddenIdx.Length; i++) {
						int tmp;
						int.TryParse (hiddenIdx [i], out tmp);
						Globals.AddHiddenIdxToWorld (worldIdx, tmp);
					}
				}

				XmlNodeList revealZPosNodes = world.GetElementsByTagName ("revealZPos");
				if (revealZPosNodes.Count == 1) {
					string[] revealZPosIdx = revealZPosNodes.Item (0).InnerText.Split (';');
					for (int i = 0; i < revealZPosIdx.Length; i++) {
						float tmp;
						float.TryParse (revealZPosIdx [i], out tmp);
						Globals.AddRevealZPosToWorld (worldIdx, tmp);
					}
				}

				//Debug.Log (worldIdx);
				//Debug.Log (Globals.worlds.Count);

				XmlNodeList treeList = world.GetElementsByTagName("t"); // array of the tree nodes
				foreach (XmlNode node in treeList) {
					bool water = false;
					List<Vector3> posList = new List<Vector3>();
					Vector3 treeRotation = Vector3.zero;
					Vector3 treeScale = Vector3.zero;
					Color color1 = Globals.color1;
					Color color2 = Globals.color2;
					XmlNodeList treeAttributes = node.ChildNodes;

					GameObject go;

					bool gradient = false;
					bool angular = false;
					bool texture = false;
					bool respawn = true;
					float deg_LS = Globals.NULL;
					float angle_LS = Globals.NULL;
					int restrictToCamera = (int)Globals.NULL;
					float vFreq = Globals.NULL;
					float hFreq = Globals.NULL;
					float rewardSize = Globals.NULL;
					float rewardMulti = Globals.NULL;
					int rank = (int)Globals.NULL;
					string materialName = "gradient";
					string type = "";
					float presoFrac = -1;
					float opacity = 1;

					foreach (XmlNode val in treeAttributes) {
						if (val.Name == "w") {
							water = (val.InnerText == "1") ? true : false;
						} else if (val.Name == "pos") {
							float x, y, z;
							string[] lines = val.InnerText.Split ('\n');
							foreach (string l in lines) {
								if (l.Split (';').Length == 3) {
									float.TryParse (l.Split (';') [0], out x);
									float.TryParse (l.Split (';') [1], out y);
									float.TryParse (l.Split (';') [2], out z);
									Vector3 pos = new Vector3 (Mathf.RoundToInt (x), Mathf.RoundToInt (y), Mathf.RoundToInt (z));
									posList.Add (pos);
								}
							}
						} else if (val.Name == "d") {
							float.TryParse (val.InnerText, out deg_LS);
							gradient = true;
						} else if (val.Name == "a") {
							float.TryParse (val.InnerText, out angle_LS);
							angular = true;
						} else if (val.Name == "tex") {
							texture = true;
						} else if (val.Name == "r") {
							int.TryParse (val.InnerText, out restrictToCamera);
						} else if (val.Name == "v") {
							float.TryParse (val.InnerText, out vFreq);
							if (hFreq == null)
								hFreq = 4;
						} else if (val.Name == "h") {
							float.TryParse (val.InnerText, out hFreq);
							if (vFreq == null)
								vFreq = 4;
						} else if (val.Name == "rewardSize") {
							float.TryParse (val.InnerText, out rewardSize);
						} else if (val.Name == "rewardMulti") {
							float.TryParse (val.InnerText, out rewardMulti);
						} else if (val.Name == "respawn") {
							respawn = (val.InnerText == "1") ? true : false;
						} else if (val.Name == "rot") {
							float x, y, z;
							float.TryParse (val.InnerText.Split (';') [0], out x);
							float.TryParse (val.InnerText.Split (';') [1], out y);
							float.TryParse (val.InnerText.Split (';') [2], out z);
							treeRotation = new Vector3 (x, y, z);
						} else if (val.Name == "scale") {
							float x, y, z;
							float.TryParse (val.InnerText.Split (';') [0], out x);
							float.TryParse (val.InnerText.Split (';') [1], out y);
							float.TryParse (val.InnerText.Split (';') [2], out z);
							treeScale = new Vector3 (x, y, z);
						} else if (val.Name == "rank") {
							int.TryParse (val.InnerText, out rank);
						} else if (val.Name == "materialName") {
							materialName = val.InnerText;
						} else if (val.Name == "type") {
							type = val.InnerText;
						} else if (val.Name == "presoFrac") {
							float.TryParse (val.InnerText, out presoFrac);
						} else if (val.Name == "opacity") {
							float.TryParse (val.InnerText, out opacity);
						} else if (val.Name == "color1") {
							float r, g, b;
							float.TryParse (val.InnerText.Split (';') [0], out r);
							float.TryParse (val.InnerText.Split (';') [1], out g);
							float.TryParse (val.InnerText.Split (';') [2], out b);
							color1 = new Color (r, g, b);
						} else if (val.Name == "color2") {
							float r, g, b;
							float.TryParse (val.InnerText.Split (';') [0], out r);
							float.TryParse (val.InnerText.Split (';') [1], out g);
							float.TryParse (val.InnerText.Split (';') [2], out b);
							color2 = new Color (r, g, b);
						}
					}

					int tIdx = Globals.AddTreeToWorld (worldIdx, water, posList, deg_LS, angle_LS, texture, restrictToCamera, vFreq, hFreq, rewardSize, rewardMulti, respawn, treeRotation, treeScale, rank, materialName, type, presoFrac, opacity, color1, color2);
					if (Globals.probeWorldIdx.Contains (worldIdx)) { // If worldIdx is specified in this list, ALL targets in this world are to be considered probes and not corrected
						Globals.AddProbeIdxToWorld (worldIdx, tIdx);
					}
				}

				XmlNodeList wallList = world.GetElementsByTagName("wall"); // array of the wall nodes
				foreach (XmlNode node in wallList) {
					Vector3 v = Vector3.zero;
					Vector3 wallRotation = Vector3.zero;
					Vector3 wallScale = Vector3.zero;

					XmlNodeList wallConfig = node.ChildNodes;

					GameObject go;

					foreach (XmlNode val in wallConfig) {
						if (val.Name == "pos") {
							float x, y, z;
							float.TryParse(val.InnerText.Split(';')[0], out x);
							float.TryParse(val.InnerText.Split(';')[1], out y);
							float.TryParse(val.InnerText.Split(';')[2], out z);

							v = new Vector3(x, y, z);
						}

						if (val.Name == "rot") {
							float x, y, z;

							float.TryParse(val.InnerText.Split(';')[0], out x);
							float.TryParse(val.InnerText.Split(';')[1], out y);
							float.TryParse(val.InnerText.Split(';')[2], out z);

							wallRotation = new Vector3(x, y, z);
						}

						if (val.Name == "scale") {
							float x, y, z;

							float.TryParse(val.InnerText.Split(';')[0], out x);
							float.TryParse(val.InnerText.Split(';')[1], out y);
							float.TryParse(val.InnerText.Split(';')[2], out z);

							wallScale = new Vector3(x, y, z);
						}
					}

					Globals.AddWallToWorld (worldIdx, v, wallRotation, wallScale);
				}

				worldIdx++;
			}

			this.errorText.text = "";

			//Debug.Log("finished");
			finishedReadingScenario = true;
        } else if (!File.Exists(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile)) {
            this.errorText.text = "ERROR: File does not exist";
        } else if (this.movementRecorderScript.GetReplayFileName() == "") {
            this.errorText.text = "ERROR : Replay file does not exist";
        }

        /*        
         if(this.movementRecorderScript.GetReplayFileName() == "") {
	         this.errorText.text = "ERROR: Replay file field empty";
         }
         */
    }

    public void SetLoadScenarioName(string s) {
        if (!s.EndsWith(".xml"))
            s = s + ".xml";
        this.loadScenarioFile = s;
    }
    
    private float map(float s, float a1, float a2, float b1, float b2) {
        return Mathf.Clamp(b1 + (s - a1) * (b2 - b1) / (a2 - a1), b1, b2);
    }

}