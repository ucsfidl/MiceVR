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
		// First, get each setting from the Sheet
		SpreadSheetManager manager = new SpreadSheetManager();
		GS2U_SpreadSheet spreadsheet = manager.LoadSpreadSheet (Globals.vrGoogleSheetsName);
		GS2U_Worksheet worksheet = spreadsheet.LoadWorkSheet(Globals.mouseName);
		if (worksheet == null) {
			GameObject.Find ("ErrorText").GetComponent<Text> ().text = "Worksheet '" + Globals.mouseName + "' NOT found!";
		} else {
			Debug.Log ("loaded worksheet " + Globals.mouseName);
			WorksheetData data = worksheet.LoadAllWorksheetInformation ();

			for (int i = 0; i < data.rows.Count; i++) {
				//Debug.Log ("Examining row " + i);

				// Find the first row with blank date and duration, and read the settings from that line
				if (data.rows [i].cells [12].value.Equals ("") && data.rows [i].cells [13].value.Equals ("")) {
					Debug.Log ("Criteria met on row " + (i+2));
					Debug.Log (data.rows [i].cells [12].cellColumTitle + " value is " + data.rows [i].cells [12].value);
					Debug.Log (data.rows [i].cells [13].cellColumTitle + " value is " + data.rows [i].cells [13].value);
					RowData rData = data.rows[i];

					for (int j = 0; j < rData.cells.Count; j++) {
						switch (rData.cells [j].cellColumTitle) {
						case "day":
							{
								GameObject.Find ("DayOnBallInput").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "scenario":
							{
								GameObject.Find ("ScenarioInput").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "session":
							{
								GameObject.Find ("ScenarioSessionInput").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "n":
							{
								GameObject.Find ("VisibleNasalBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "t":
							{
								GameObject.Find ("VisibleTemporalBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "h":
							{
								GameObject.Find ("VisibleHighBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "l":
							{
								GameObject.Find ("VisibleLowBoundary").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						case "maxh2o":
							{
								GameObject.Find ("MaxReward").GetComponent<InputField> ().text = rData.cells [j].value;
								break;
							}
						}
					}
					break;
				}
			}

		}
	}

	// Read and load the scenario text file into memory
	public void LoadScenario()
	{
		// Clear trees that appear onscreen before level is loaded
		GameObject[] gos;
		gos = GameObject.FindGameObjectsWithTag("water");
		/* Needs UnityEditor, which does not compile
		foreach (GameObject go2 in gos) {
			if (PrefabUtility.GetPrefabParent (go2) != null && PrefabUtility.GetPrefabObject (go2) == null) { // Is a prefab
				Object.Destroy (go2);
			}
		}
		*/

        if (File.Exists(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile)) {

            XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
            xmlDoc.LoadXml(File.ReadAllText(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile, ASCIIEncoding.ASCII)); // load the file.

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

				if (xn ["distractorHFreq"] != null) {
					float.TryParse(xn["distractorHFreq"].InnerText, out Globals.distractorHFreq);
				}

				if (xn ["distractorVFreq"] != null) {
					float.TryParse(xn["distractorVFreq"].InnerText, out Globals.distractorVFreq);
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
					Debug.Log ("Found preso ratio " + Globals.presoRatio);
				}

				if (xn ["optoSide"] != null) {
					string optoSideXML = xn ["optoSide"].InnerText;
					if (optoSideXML.Equals ("L"))
						Globals.optoSide = 0;
					else if (optoSideXML.Equals ("R"))
						Globals.optoSide = 1;
					else if (optoSideXML.Equals ("LR"))
						Globals.optoSide = 2;
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


				if (xn ["rewardDur"] != null) {  // Used by SDT task where reward size is varied, but if the stock reward size is used (60ms or 4ul), than a 2-4x multiplier is too much and the water drop falls onto the ball...
					// Update the rewardSize, assuming linearity, which is a poor approximation
					int newRewardDur = int.Parse(xn["rewardDur"].InnerText);
					Globals.rewardSize = Globals.rewardSize / Globals.rewardDur * newRewardDur;
					Globals.rewardDur = newRewardDur;
				}
            }

			XmlNodeList worldList = xmlDoc.GetElementsByTagName("world");
			int worldNum = 0;
			foreach (XmlElement world in worldList) {
				XmlNodeList treeList = world.GetElementsByTagName("t"); // array of the tree nodes
				foreach (XmlNode node in treeList) {
					bool water = false;
					Vector3 v = Vector3.zero;
					Vector3 treeRotation = Vector3.zero;
					Vector3 treeScale = Vector3.zero;
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

					foreach (XmlNode val in treeAttributes) {
						if (val.Name == "w") {
							water = (val.InnerText == "1") ? true : false;
						} else if (val.Name == "pos") {
							float x, y, z;
							float.TryParse (val.InnerText.Split (';') [0], out x);
							float.TryParse (val.InnerText.Split (';') [1], out y);
							float.TryParse (val.InnerText.Split (';') [2], out z);
							v = new Vector3 (Mathf.RoundToInt (x), Mathf.RoundToInt (y), Mathf.RoundToInt (z));
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
						}
					}

					Globals.AddTreeToWorld (worldNum, water, v, deg_LS, angle_LS, texture, restrictToCamera, vFreq, hFreq, rewardSize, rewardMulti, respawn, treeRotation, treeScale, rank, materialName, type, presoFrac);
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

					Globals.AddWallToWorld (worldNum, v, wallRotation, wallScale);
				}

				worldNum++;
			}

			this.errorText.text = "";

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