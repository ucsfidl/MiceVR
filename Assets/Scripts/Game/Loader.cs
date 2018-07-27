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

		/* 
		if (!this.placed && Globals.treeParent.transform.childCount > 0)
        {
            foreach (Transform tr in Globals.treeParent.transform)
            {
                treeList.Add(tr.gameObject);
            }
            this.placed = true;
        }

        if (start < treeList.Count)
        {
            if (end > treeList.Count)
            {
                end = treeList.Count;
            }

            float locx = treeList[0].transform.position.x;
            float hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
            float vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
            float r = Random.value;
            // NB edit
            // If there are only 2 or 3 trees, alternate which is visible, and leave the third as constant
            if (Globals.gameType.Equals("detection"))
            {
				if (end == 1 && Globals.varyOrientation) {  // 1-choice detection - vary the orientation of the first trial
					if (r > 0.5) {
						treeList[0].GetComponent<WaterTreeScript>().SetShader(vfreq, hfreq);
						hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
						vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
					}
					Debug.Log ("[0, 0.5, 1] - " + r);
				} else if (end == 2) { // 2-choice detection
					if (r < 0.5) {
						treeList [1].GetComponent<WaterTreeScript> ().Hide ();
						locx = treeList [0].transform.position.x;
						hfreq = treeList [0].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [0].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
					} else {
						treeList [0].GetComponent<WaterTreeScript> ().Hide ();
						locx = treeList [1].transform.position.x;
						hfreq = treeList [1].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [1].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
					}
					Debug.Log ("[0, 0.5, 1] - " + r);
				} else if (end == 4) { // 4-choice detection
					if (r < 0.25) {
						locx = treeList [0].transform.position.x;
						hfreq = treeList [0].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [0].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						treeList [1].GetComponent<WaterTreeScript> ().Hide ();
						treeList [2].GetComponent<WaterTreeScript> ().Hide ();
						treeList [3].GetComponent<WaterTreeScript> ().Hide ();
					} else if (r < 0.5) {
						locx = treeList [1].transform.position.x;
						hfreq = treeList [1].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [1].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						treeList [0].GetComponent<WaterTreeScript> ().Hide ();
						treeList [2].GetComponent<WaterTreeScript> ().Hide ();
						treeList [3].GetComponent<WaterTreeScript> ().Hide ();
					} else if (r < 0.75) {
						locx = treeList [2].transform.position.x;
						hfreq = treeList [2].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [2].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						treeList [0].GetComponent<WaterTreeScript> ().Hide ();
						treeList [1].GetComponent<WaterTreeScript> ().Hide ();
						treeList [3].GetComponent<WaterTreeScript> ().Hide ();
					} else {
						locx = treeList [3].transform.position.x;
						hfreq = treeList [3].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [3].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
						treeList [0].GetComponent<WaterTreeScript> ().Hide ();
						treeList [1].GetComponent<WaterTreeScript> ().Hide ();
						treeList [2].GetComponent<WaterTreeScript> ().Hide ();
					}
				}
            }
            else if (Globals.gameType.Equals("det_blind"))
            {
				if (treeList.Count == 3) {  // regular level
	                if (r < 0.333)
	                {
	                    treeList[1].GetComponent<WaterTreeScript>().Hide();
	                    locx = treeList[0].transform.position.x;
	                    hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
	                    vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
	                }
	                else if (r < 0.667)
	                {
	                    treeList[0].GetComponent<WaterTreeScript>().Hide();
	                    locx = treeList[1].transform.position.x;
	                    hfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
	                    vfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
	                }
	                else
	                {
	                    treeList[0].GetComponent<WaterTreeScript>().Hide();
	                    treeList[1].GetComponent<WaterTreeScript>().Hide();
	                    locx = treeList[2].transform.position.x;
	                    hfreq = treeList[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
	                    vfreq = treeList[2].GetComponent<WaterTreeScript>().GetShaderVFreq();
	                }
	                Debug.Log("[0, 0.333, 0.667, 1] - " + r);
				} else if (treeList.Count == 2) { // level for pre-training lesioned animals
					if (r < 0.5) {
						locx = treeList[0].transform.position.x;
						hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
						vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
					} else {
						treeList[0].GetComponent<WaterTreeScript>().Hide();
						locx = treeList[1].transform.position.x;
						hfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
						vfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
					}
					Debug.Log("[0, 0.5, 1] - " + r);
				}
            }
            else if (Globals.gameType.Equals("det_target"))
            {
				int treeToActivate = r < 0.5 ? 0 : 1;
				int treeToInactivate = r < 0.5 ? 1 : 0;

				treeList [treeToInactivate].GetComponent<WaterTreeScript> ().Hide ();
				locx = treeList[treeToActivate].transform.position.x;
				hfreq = treeList[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
				vfreq = treeList[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();

				if (Globals.varyOrientation) {
					float r2 = Random.value;
					if (r2 > 0.5) {
						treeList [treeToActivate].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
						treeList[2].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
						hfreq = treeList[treeToActivate].GetComponent<WaterTreeScript>().GetShaderHFreq();
						vfreq = treeList[treeToActivate].GetComponent<WaterTreeScript>().GetShaderVFreq();
					}
					Debug.Log("Ori: [0, 0.5, 1] - " + r2);
				}
				Debug.Log("Loc: [0, 0.5, 1] - " + r);
            }
			else if (Globals.gameType.Equals("disc_target"))
			{
				float r2 = Random.value;  // used to set orientation of target or distractor
				int treeToTarget = r < 0.5 ? 0 : 1;
				int treeToDistract = r < 0.5 ? 1 : 0;

				locx = treeList[treeToTarget].transform.position.x;
				hfreq = treeList[treeToTarget].GetComponent<WaterTreeScript>().GetShaderHFreq();
				vfreq = treeList[treeToTarget].GetComponent<WaterTreeScript>().GetShaderVFreq();

				treeList[treeToTarget].GetComponent<WaterTreeScript>().SetCorrect(true);
				treeList[treeToDistract].GetComponent<WaterTreeScript>().SetCorrect(false);

				if (Globals.varyOrientation) {
					if (r2 > 0.5) {
						treeList [treeToTarget].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
						hfreq = treeList [treeToTarget].GetComponent<WaterTreeScript> ().GetShaderHFreq ();
						vfreq = treeList [treeToTarget].GetComponent<WaterTreeScript> ().GetShaderVFreq ();
					}
					treeList [2].GetComponent<WaterTreeScript> ().SetShader (hfreq, vfreq);

					treeList [treeToDistract].GetComponent<WaterTreeScript> ().SetColors (Globals.distColor1, Globals.distColor2);
					treeList [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (vfreq, hfreq);
				} else {
					if (r2 < 0.5) {
						treeList [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (4, 1);
					} else {
						treeList [treeToDistract].GetComponent<WaterTreeScript> ().SetShader (1, 4);
					}
				}

				Debug.Log("Loc: [0, 0.5, 1] - " + r);
				Debug.Log("Ori: [0, 0.5, 1] - " + r2);
			}
            else if (Globals.gameType.Equals("discrimination"))
            {
                // Randomize orientations on first load
				if (r < 0.5) {  // Swap orientations between trees
					treeList [0].GetComponent<WaterTreeScript> ().SetShader (Globals.rewardedHFreq, Globals.rewardedVFreq);
					treeList [1].GetComponent<WaterTreeScript> ().SetShader (Globals.distractorHFreq, Globals.distractorVFreq);
					treeList[0].GetComponent<WaterTreeScript>().SetCorrect(true);
					treeList[1].GetComponent<WaterTreeScript>().SetCorrect(false);
					locx = treeList[0].transform.position.x;
					hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
					vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
				} else {
					treeList [0].GetComponent<WaterTreeScript> ().SetShader (Globals.distractorHFreq, Globals.distractorVFreq);
					treeList [1].GetComponent<WaterTreeScript> ().SetShader (Globals.rewardedHFreq, Globals.rewardedVFreq);
					treeList[0].GetComponent<WaterTreeScript>().SetCorrect(false);
					treeList[1].GetComponent<WaterTreeScript>().SetCorrect(true);
					locx = treeList[1].transform.position.x;
					hfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
					vfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
				}
            }
            else if (Globals.gameType.Equals("match") || Globals.gameType.Equals("nonmatch"))  // There are three trees - a central initial tree, and 1 on left and 1 on right
            {
                // First, pick an orientation at random for the central tree
                float targetHFreq = treeList[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
                float targetVFreq = treeList[2].GetComponent<WaterTreeScript>().GetShaderVFreq();

                if (r < 0.5)  // Switch target to opposite of initiation
                {
                    treeList[2].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                }
                // Second, randomly pick which side the matching orientation is on
                float rSide = Random.value;
                targetHFreq = treeList[2].GetComponent<WaterTreeScript>().GetShaderHFreq();
                targetVFreq = treeList[2].GetComponent<WaterTreeScript>().GetShaderVFreq();
                if (rSide < 0.5)  // Set the left tree to match
                {
                    treeList[0].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    treeList[1].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    if (Globals.gameType.Equals("match"))
                    {
                        treeList[0].GetComponent<WaterTreeScript>().SetCorrect(true);
                        treeList[1].GetComponent<WaterTreeScript>().SetCorrect(false);
                        locx = treeList[0].transform.position.x;
                        hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
                        vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
                    }
                    else
                    {
                        treeList[0].GetComponent<WaterTreeScript>().SetCorrect(false);
                        treeList[1].GetComponent<WaterTreeScript>().SetCorrect(true);
                        locx = treeList[1].transform.position.x;
                        hfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
                        vfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
                    }
                }
                else // Set the right tree to match
                {
                    treeList[0].GetComponent<WaterTreeScript>().SetShader(targetVFreq, targetHFreq);
                    treeList[1].GetComponent<WaterTreeScript>().SetShader(targetHFreq, targetVFreq);
                    if (Globals.gameType.Equals("match"))
                    {
                        treeList[0].GetComponent<WaterTreeScript>().SetCorrect(false);
                        treeList[1].GetComponent<WaterTreeScript>().SetCorrect(true);
                        locx = treeList[1].transform.position.x;
                        hfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderHFreq();
                        vfreq = treeList[1].GetComponent<WaterTreeScript>().GetShaderVFreq();
                    }
                    else
                    {
                        treeList[0].GetComponent<WaterTreeScript>().SetCorrect(true);
                        treeList[1].GetComponent<WaterTreeScript>().SetCorrect(false);
                        locx = treeList[0].transform.position.x;
                        hfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderHFreq();
                        vfreq = treeList[0].GetComponent<WaterTreeScript>().GetShaderVFreq();
                    }
                }
            }
			// NO bias correction with FOV location yet, but may need to add later
			if (Globals.perim && (((treeList.Count == 3 || treeList.Count == 2) && locx != Globals.worldXCenter) || treeList.Count == 4)) {  // perimetry is enabled, so pick from the set of random windows to use
				int rFOV = UnityEngine.Random.Range (0, Globals.fovsForPerimScaleInclusive [Globals.perimScale]);
				Debug.Log ("FOV: " + rFOV);
				Globals.SetOccluders (locx, rFOV);
			} else {
				Globals.SetOccluders(locx);
				Debug.Log ("no dynamic occlusion");
			}

            Globals.targetLoc.Add(locx);
            Globals.targetHFreq.Add(hfreq);
            Globals.targetVFreq.Add(vfreq);

            for (int i = start; i < end; i++)
            {
                treeList[i].SetActive(true);
            }
            System.GC.Collect();

            start += inc;
            end += inc;
        }
        else if (start > 0 && start >= treeList.Count)
        { 

			this.scenarioLoaded = true;
        }
        */
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

				if (xn ["presoRatio"] != null) {
					float.TryParse (xn ["presoRatio"].InnerText, out Globals.presoRatio);
					Debug.Log ("Found preso ratio " + Globals.presoRatio);
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

							float.TryParse(val.InnerText.Split(';')[0], out x);
							float.TryParse(val.InnerText.Split(';')[1], out y);
							float.TryParse(val.InnerText.Split(';')[2], out z);

							treeRotation = new Vector3(x, y, z);
						} else if (val.Name == "scale") {
							float x, y, z;

							float.TryParse(val.InnerText.Split(';')[0], out x);
							float.TryParse(val.InnerText.Split(';')[1], out y);
							float.TryParse(val.InnerText.Split(';')[2], out z);

							treeScale = new Vector3(x, y, z);
						}
					}

					Globals.AddTreeToWorld (worldNum, water, v, deg_LS, angle_LS, texture, restrictToCamera, vFreq, hFreq, rewardSize, rewardMulti, respawn, treeRotation, treeScale);
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