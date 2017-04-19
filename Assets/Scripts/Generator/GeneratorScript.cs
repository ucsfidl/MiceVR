using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine.UI;

public class GeneratorScript : MonoBehaviour {

	public GameObject waterTreePrefab, dryTreePrefab, wallPrefab;
	public GameObject treeParent, wallParent;
    public Text errorText;
    public MovementRecorder movementRecorderScript;
    public float dragSpeed = 10f, scrollSpeed = 20f;

	private int waterTrees, dryTrees;
    private float waterMu, waterSigma, waterMinRotationGaussian, waterMaxRotationGaussian;
    private float dryMu, drySigma, dryMinRotationGaussian, dryMaxRotationGaussian;
    private float waterMinRotationUniform, waterMaxRotationUniform, dryMinRotationUniform, dryMaxRotationUniform;
    private int dryUniformSteps, waterUniformSteps;
    private bool waterUniform, waterGaussian, dryUniform, dryGaussian;
    private bool waterTraining;
    private GameObject spawnedWall, spawnedWaterTree, spawnedDryTree;
    private Vector3 acceleration;
    private bool placingWall, placingWaterTree, placingDryTree;
    private string loadScenarioFile, saveScenarioFile;
    private List<GameObject> waterTreesQueue, dryTreesQueue;

    private Texture2D waterTexture, dryTexture;
    private string waterTextureFile, dryTextureFile;
    private bool waterFixed, dryFixed;
    public Image waterImage, dryImage;

    private string waterTextureFile_LS, dryTextureFile_LS;
    private float deg_LS;

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

    private float angle_LS;
    private bool waterTop_LS, waterBot_LS, dryTop_LS, dryBot_LS, waterDouble_LS, waterSpherical_LS, dryDouble_LS, drySpherical_LS;

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
    public GameObject waterAngularTreePrefab, dryAngularTreePrefab;
    private Color ogButtonColor;

	// Use this for initialization
	void Start ()
    {
        this.placingWall = false;
        this.loadScenarioFile = this.saveScenarioFile = "";
        this.waterUniform = this.dryUniform = true;
        this.spawnWaterPattern = this.spawnDryPattern = true;
        this.scenarioLoaded = false;
        this.sceneEditing = false;

        start = 0;
        inc = 1000;
        end = inc;

        ended = false;
        printed = false;
        firstRun = true;

        treeList = new List<GameObject>();

        this.gridCenter = new Vector2(20000, 20000);
        this.gridWidth = 40000;
        this.gridHeight = 40000;

        buttonColor = wallButton.GetComponent<Image>().color;
        ogButtonColor = GameObject.FindGameObjectWithTag("editButton").GetComponent<Button>().colors.normalColor;
	}
	
	// Update is called once per frame
    void Update()
    {
        if (start < treeList.Count)
        {
            statusText.text = "Generating...";
            if (firstRun)
            {
                startTime = System.DateTime.Now;
                firstRun = false;
            }

            if (end > treeList.Count)
            {
                end = treeList.Count;
            }

            for (int i = start; i < end; i++)
            {
                treeList[i].SetActive(true);
            }

            start += inc;
            end += inc;
            System.GC.Collect();
        }
        else if (!ended && start > 0 && start >= treeList.Count)
        {
            for (int i = 0; i < this.waterTreesQueue.Count; i++)
            {
                Destroy((GameObject)this.waterTreesQueue[i]);
            }

            for (int i = 0; i < this.dryTreesQueue.Count; i++)
            {
                Destroy((GameObject)this.dryTreesQueue[i]);
            }

            this.waterTreesQueue = null;
            this.dryTreesQueue = null;

            ended = true;
            StartCoroutine(ShowStatus("Done!"));
        }

        EditorTools();
        CameraDrag();
    }

    public void Delete(ref bool b, GameObject go, GameObject button)
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            b = false;
            print("escaped");
            //delete current go
            GameObject.Destroy(go);
            //reset button color
            button.GetComponent<Image>().color = buttonColor;
        }
    }

    private void CameraDrag()
    {
        if (!this.sceneEditing)
        {
            if (this.acceleration != Vector3.zero)
            {
                Camera.main.transform.position -= acceleration;
                acceleration *= .95f;
            }

            if (!this.placingWall)
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0)
                {
                    Camera.main.transform.position += Vector3.down * this.scrollSpeed;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0)
                {
                    Camera.main.transform.position += Vector3.up * this.scrollSpeed;
                } 
            }

            if (Input.GetMouseButton(0))
            {
                acceleration = new Vector3(this.dragSpeed * Input.GetAxis("Mouse X"), 0f, this.dragSpeed * Input.GetAxis("Mouse Y"));
            }
        }
    }

    private void EditorTools()
    {
        if (this.placingWall)
        {
            wallButton.GetComponent<Image>().color = Color.green;
            waterTreeButton.GetComponent<Image>().color = this.buttonColor;
            dryTreeButton.GetComponent<Image>().color = this.buttonColor;
            if (Input.GetMouseButtonDown(1))
            {
                this.placingWall = false;
                this.spawnedWall.name = "PlacedWall";
                SpawnWall();
                RaycastHit hit1;
                Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray1, out hit1))
                {
                    Vector3 v = hit1.point;
                    v.y = 0f;
                    this.spawnedWall.transform.position = v;
                }
            }

            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                {
                    this.spawnedWall.transform.Rotate(Vector3.up, 5f);
                }
                else
                {
                    this.spawnedWall.transform.localScale += new Vector3(0, 0, 1.0f);
                }
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                {
                    this.spawnedWall.transform.Rotate(Vector3.up, -5f);
                }
                else
                {
                    this.spawnedWall.transform.localScale += new Vector3(0, 0, -1.0f);
					Debug.Log (this.spawnedWall.transform.localScale.z);
                }
            }

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.name == "Terrain")
                {
                    Vector3 v = hit.point;
                    v.y = this.spawnedWall.transform.localScale.y / 2f;
                    this.spawnedWall.transform.position = v;
                }
                /*else if( hit.collider.name.Contains("PlacedWall"))
                {
                    this.spawnedWall.transform.position = hit.collider.gameObject.transform.position + (Vector3.forward * this.spawnedWall.transform.localScale.z);
                }*/
            }

            Delete(ref placingWall, this.spawnedWall, wallButton);
        }
        else if (this.placingWaterTree)
        {
            waterTreeButton.GetComponent<Image>().color = Color.green;
            wallButton.GetComponent<Image>().color = this.buttonColor;
            dryTreeButton.GetComponent<Image>().color = this.buttonColor;
            if (Input.GetMouseButtonDown(1))
            {
                this.placingWaterTree = false;
                SpawnWaterTree();
                RaycastHit hit1;
                Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray1, out hit1))
                {
                    Vector3 v = hit1.point;
                    v.y = 0f;
                    this.spawnedWaterTree.transform.position = v;
                }
            }

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.name == "Terrain")
                {
                    Vector3 v = hit.point;
                    v.y = 0f;
                    this.spawnedWaterTree.transform.position = v;
                }
                /*else if( hit.collider.name.Contains("PlacedWall"))
                {
                    this.spawnedWall.transform.position = hit.collider.gameObject.transform.position + (Vector3.forward * this.spawnedWall.transform.localScale.z);
                }*/
            }

            Delete(ref placingWaterTree, this.spawnedWaterTree, waterTreeButton);
        }
        else if (this.placingDryTree)
        {
            dryTreeButton.GetComponent<Image>().color = Color.green;
            wallButton.GetComponent<Image>().color = this.buttonColor;
            waterTreeButton.GetComponent<Image>().color = this.buttonColor;
            if (Input.GetMouseButtonDown(1))
            {
                this.placingDryTree = false;
                SpawnDryTree();
                RaycastHit hit1;
                Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray1, out hit1))
                {
                    Vector3 v = hit1.point;
                    v.y = 0f;
                    this.spawnedDryTree.transform.position = v;
                }
            }

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.name == "Terrain")
                {
                    Vector3 v = hit.point;
                    v.y = 0f;
                    this.spawnedDryTree.transform.position = v;
                }
                /*else if( hit.collider.name.Contains("PlacedWall"))
                {
                    this.spawnedWall.transform.position = hit.collider.gameObject.transform.position + (Vector3.forward * this.spawnedWall.transform.localScale.z);
                }*/
            }
            Delete(ref placingDryTree, this.spawnedDryTree, dryTreeButton);
        }
    }

    public void Generate()
    {
        this.placed = false;
        generationTime = System.DateTime.Now;

        Vector3 tmpPos;
        GameObject tree;

        /*
        System.Random r = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            print((float)RandomExtensions.NextGaussian(r, this.waterMu, this.waterSigma));
        }
         * */

        System.DateTime d = System.DateTime.Now;

        if (this.waterTrees > 0 || this.dryTrees > 0)
        {
            this.waterTreesQueue = new List<GameObject>(this.waterTrees);
            this.dryTreesQueue = new List<GameObject>(this.dryTrees);

            SpawnWater();
            SpawnDry();
            print("creation: " + (System.DateTime.Now - d));
            int rows = Mathf.RoundToInt(Mathf.Sqrt(this.waterTrees + this.dryTrees));
            int columns = rows;

            int indexX = Mathf.RoundToInt((this.gridWidth / rows));
            int indexY = Mathf.RoundToInt((this.gridHeight / columns));
            int startX = Mathf.RoundToInt((indexX / 2) - (this.gridWidth / 2));
            int startY = Mathf.RoundToInt((indexY / 2) - (this.gridHeight / 2));

            int randomLimitsX = Mathf.RoundToInt(indexX / 3);
            int randomLimitsY = Mathf.RoundToInt(indexY / 3);

            System.DateTime gt = System.DateTime.Now;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    tmpPos = new Vector3(this.gridCenter.x + (Random.Range(-1 * randomLimitsX, randomLimitsX) + (startX + indexX * i)), 0f, this.gridCenter.y + Random.Range(-1 * randomLimitsY, randomLimitsY) + (startY + indexY * j));
                    float randVal = Random.value;

                    if (this.dryTreesQueue.Count == 0 || randVal < .5f)
                    {
                        if (this.waterTreesQueue.Count > 0)
                        {
                            tree = (GameObject)this.waterTreesQueue[0];
                            this.waterTreesQueue.RemoveAt(0);
                            tree.transform.position = tmpPos;
                        }
                    }
                    else if (this.waterTreesQueue.Count == 0 || randVal >= .5f)
                    {
                        if (this.dryTreesQueue.Count > 0)
                        {
                            tree = (GameObject)this.dryTreesQueue[0];
                            this.dryTreesQueue.RemoveAt(0);
                            tree.transform.position = tmpPos;
                        }
                    }
                }
            }

            print("reposition: " + (System.DateTime.Now - gt));

            TriggerBatchLoader();
        }
        else
        {
            StartCoroutine(ShowError("# of trees is 0"));
        }

        print("Elapsed: " + (System.DateTime.Now - d));
    }

    private void TriggerBatchLoader()
    {
        foreach (Transform tr in this.treeParent.transform)
        {
            treeList.Add(tr.gameObject);
        }

        this.start = 0;
        this.end = this.inc;
        this.ended = false;
    }


    public void SpawnWater()
    {
        GameObject go;
        System.DateTime g = System.DateTime.Now;

//new spawn water
        //GRADIENT
        if (this.waterGradient)
        {
            if (this.waterUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.waterTrees / (float)this.waterUniformSteps);
                float angleIncrement = (this.waterUniformSteps > 1) ? (float)(this.waterMaxRotationUniform - this.waterMinRotationUniform) / (float)(this.waterUniformSteps - 1) : 0f;
                for (int i = 0; i < this.waterUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<WaterTreeScript>().SetShaderRotation((float)this.waterMinRotationUniform + (i * angleIncrement));
                        go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                        go.GetComponent<WaterTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.waterTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.waterGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.waterTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.waterMu, this.waterSigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.waterSigma), this.waterMinRotationGaussian, this.waterMaxRotationGaussian));

                    go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<WaterTreeScript>().SetShaderRotation(ro);

                    go.GetComponent<WaterTreeScript>().Init();
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.waterFixed)
            {
                for (int j = 0; j < waterTrees; j++)
                {
                    go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<WaterTreeScript>().SetShaderRotation(waterFixedFloat);

                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.GetComponent<WaterTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }
        //TEXTURED
        else if (this.waterTextured)
        {
            for (int j = 0; j < waterTrees; j++)
            {
                go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                go.GetComponent<WaterTreeScript>().ChangeTexture(waterTexture);

                go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                go.GetComponent<WaterTreeScript>().Init();
                go.transform.parent = this.treeParent.transform;
                this.waterTreesQueue.Add(go);
                go.SetActive(false);
            }
        }
        //ANGULAR
        else if (this.waterAngularTop || this.waterAngularBot)
        {           
            if (this.waterUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.waterTrees / (float)this.waterUniformSteps);
                float angleIncrement = (this.waterUniformSteps > 1) ? (float)(this.waterMaxRotationUniform - this.waterMinRotationUniform) / (float)(this.waterUniformSteps - 1) : 0f;
                for (int i = 0; i < this.waterUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<AngularTreeScript>().ShapeShift("single");
                        
                        if (waterAngularBot)
                        {
                            go.GetComponent<AngularTreeScript>().ChangeBottomRing((float)this.waterMinRotationUniform + (i * angleIncrement));
                        }
                        else if (waterAngularTop)
                        {
                            go.GetComponent<AngularTreeScript>().ChangeTopRing((float)this.waterMinRotationUniform + (i * angleIncrement));
                        }

                        go.GetComponent<WaterTreeScript>().Init();
                        go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                        go.transform.parent = this.treeParent.transform;
                        this.waterTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.waterGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.waterTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.waterMu, this.waterSigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.waterSigma), this.waterMinRotationGaussian, this.waterMaxRotationGaussian));

                    go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("single");
                    if (waterAngularBot)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeBottomRing(ro);
                    }
                    else if (waterAngularTop)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeTopRing(ro);
                    }
                    go.GetComponent<WaterTreeScript>().Init();
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.waterFixed)
            {
                for (int j = 0; j < waterTrees; j++)
                {
                    go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("single");
                    if (waterAngularBot)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeBottomRing(waterFixedFloat);
                    }
                    else if (waterAngularTop)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeTopRing(waterFixedFloat);
                    }
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.GetComponent<WaterTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }
        //DOUBLE ANGULAR
        else if (this.waterDoubleAngular)
        {
            if (this.waterUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.waterTrees / (float)this.waterUniformSteps);
                float angleIncrement = (this.waterUniformSteps > 1) ? (float)(this.waterMaxRotationUniform - this.waterMinRotationUniform) / (float)(this.waterUniformSteps - 1) : 0f;
                for (int i = 0; i < this.waterUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<AngularTreeScript>().ShapeShift("double");

                        go.GetComponent<AngularTreeScript>().ChangeBottomRing((float)this.waterMinRotationUniform + (i * angleIncrement));
                        go.GetComponent<AngularTreeScript>().ChangeTopRing((float)this.waterMinRotationUniform + (i * angleIncrement));

                        go.GetComponent<WaterTreeScript>().Init();
                        go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                        go.transform.parent = this.treeParent.transform;
                        this.waterTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.waterGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.waterTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.waterMu, this.waterSigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.waterSigma), this.waterMinRotationGaussian, this.waterMaxRotationGaussian));

                    go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("double");

                    go.GetComponent<AngularTreeScript>().ChangeBottomRing(ro);
                    go.GetComponent<AngularTreeScript>().ChangeTopRing(ro);

                    go.GetComponent<WaterTreeScript>().Init();
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.waterFixed)
            {
                for (int j = 0; j < waterTrees; j++)
                {
                    go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("double");
                    
                    go.GetComponent<AngularTreeScript>().ChangeBottomRing(waterFixedFloat);
                    go.GetComponent<AngularTreeScript>().ChangeTopRing(waterFixedFloat);
                   
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.GetComponent<WaterTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }
        //SPHERICAL
        if (this.waterSpherical)
        {
            if (this.waterUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.waterTrees / (float)this.waterUniformSteps);
                float angleIncrement = (this.waterUniformSteps > 1) ? (float)(this.waterMaxRotationUniform - this.waterMinRotationUniform) / (float)(this.waterUniformSteps - 1) : 0f;
                for (int i = 0; i < this.waterUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<AngularTreeScript>().ShapeShift("spherical");

                        go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap((float)this.waterMinRotationUniform + (i * angleIncrement)));

                        go.GetComponent<WaterTreeScript>().Init();
                        go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                        go.transform.parent = this.treeParent.transform;
                        this.waterTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.waterGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.waterTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.waterMu, this.waterSigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.waterSigma), this.waterMinRotationGaussian, this.waterMaxRotationGaussian));

                    go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("spherical");

                    go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(ro));

                    go.GetComponent<WaterTreeScript>().Init();
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.waterFixed)
            {
                for (int j = 0; j < waterTrees; j++)
                {
                    go = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("spherical");

                    go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(waterFixedFloat));

                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.GetComponent<WaterTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }

            //old spawn water
        /*
            if (this.waterUniform && !this.waterGaussian && !this.waterFixed)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.waterTrees / (float)this.waterUniformSteps);
                //float angleIncrement = (float)(this.waterMaxRotationUniform - this.waterMinRotationUniform) / (float)(this.waterSteps-1);
                float angleIncrement = (this.waterUniformSteps > 1) ? (float)(this.waterMaxRotationUniform - this.waterMinRotationUniform) / (float)(this.waterUniformSteps - 1) : 0f;


                if (!this.waterAngular)
                {
                    for (int i = 0; i < this.waterUniformSteps; i++)
                    {
                        for (int j = 0; j < treesPerStep; j++)
                        {
                            go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                            //go.GetComponent<WaterTreeScript>().Init();
                            go.GetComponent<WaterTreeScript>().SetShaderRotation((float)this.waterMinRotationUniform + (i * angleIncrement));
                            go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                            go.GetComponent<WaterTreeScript>().Init();
                            go.transform.parent = this.treeParent.transform;
                            this.waterTreesQueue.Add(go);
                            go.SetActive(false);
                        }
                    }
                }
                else if (this.waterAngular)
                {
                    for (int i = 0; i < this.waterUniformSteps; i++)
                    {
                        for (int j = 0; j < treesPerStep; j++)
                        {
                            go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                            // go.GetComponent<WaterTreeScript>().Init();
                            if (waterAngularBot)
                            {
                                go.GetComponent<WaterTreeScript>().ChangeBottomRing((float)this.waterMinRotationUniform + (i * angleIncrement));
                            }
                            else if (waterAngularTop)
                            {
                                go.GetComponent<WaterTreeScript>().ChangeTopRing((float)this.waterMinRotationUniform + (i * angleIncrement));

                            }

                            go.GetComponent<WaterTreeScript>().Init();
                            go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                            go.transform.parent = this.treeParent.transform;
                            this.waterTreesQueue.Add(go);
                            go.SetActive(false);

                        }
                    }
                }
            }
            else if (this.waterGaussian && !this.waterUniform && !this.waterFixed)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.waterTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.waterMu, this.waterSigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.waterSigma), this.waterMinRotationGaussian, this.waterMaxRotationGaussian));
                    if (ro < 10f)
                        a1++;
                    else if (ro >= 10f && ro < 20f)
                        a2++;
                    else
                        a3++;
                    if (waterAngular)
                    {
                        go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                        // go.GetComponent<WaterTreeScript>().Init();
                        if (waterAngularBot)
                        {
                            go.GetComponent<WaterTreeScript>().ChangeBottomRing(ro);
                        }
                        else if (waterAngularTop)
                        {
                            go.GetComponent<WaterTreeScript>().ChangeTopRing(ro);
                        }
                    }
                    else
                    {
                        go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<WaterTreeScript>().SetShaderRotation(ro);
                    }
                    go.GetComponent<WaterTreeScript>().Init();
                    go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                    go.transform.parent = this.treeParent.transform;
                    this.waterTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            else if (this.waterFixed && !this.waterUniform && !this.waterGaussian)
            {
                if (waterAngular)
                {
                    for (int j = 0; j < waterTrees; j++)
                    {
                        go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                        //   go.GetComponent<WaterTreeScript>().Init();
                        if (waterAngularBot)
                        {
                            go.GetComponent<WaterTreeScript>().ChangeBottomRing(waterAngularAngle);
                        }
                        else if (waterAngularTop)
                        {
                            go.GetComponent<WaterTreeScript>().ChangeTopRing(waterAngularAngle);
                        }
                        go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                        go.GetComponent<WaterTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.waterTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
                else
                {
                    for (int j = 0; j < waterTrees; j++)
                    {
                        go = (GameObject)Instantiate(this.waterTreePrefab, Vector3.zero, Quaternion.identity);
                        //go.GetComponent<WaterTreeScript>().Init();
                        go.GetComponent<WaterTreeScript>().ChangeTexture(waterTexture);

                        go.GetComponent<WaterTreeScript>().SetForTraining(this.waterTraining);
                        go.GetComponent<WaterTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.waterTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
         * */

        Shuffle(ref this.waterTreesQueue);
        print("Spawn water in: " + (System.DateTime.Now-g));
    }

    public void SpawnDry()
    {
        GameObject go;
        System.DateTime g = System.DateTime.Now;

        if (this.dryGradient)
        {
            if (this.dryUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.dryTrees / (float)this.dryUniformSteps);
                float angleIncrement = (this.dryUniformSteps > 1) ? (float)(this.dryMaxRotationUniform - this.dryMinRotationUniform) / (float)(this.dryUniformSteps - 1) : 0f;
                for (int i = 0; i < this.dryUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<DryTreeScript>().SetShaderRotation((float)this.dryMinRotationUniform + (i * angleIncrement));
                        go.GetComponent<DryTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.dryTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.dryGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.dryTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.dryMu, this.drySigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.drySigma), this.dryMinRotationGaussian, this.dryMaxRotationGaussian));
                    go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);

                    go.GetComponent<DryTreeScript>().SetShaderRotation(ro);

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.dryFixed)
            {
                for (int j = 0; j < dryTrees; j++)
                {
                    go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<DryTreeScript>().SetShaderRotation(dryFixedFloat);

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }
        else if (this.dryTextured)
        {
            go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
            go.GetComponent<DryTreeScript>().ChangeTexture(dryTexture);

            go.GetComponent<DryTreeScript>().Init();
            go.transform.parent = this.treeParent.transform;
            this.dryTreesQueue.Add(go);
            go.SetActive(false);
        }
        else if (this.dryAngularTop || this.dryAngularBot)
        {
            if (this.dryUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.dryTrees / (float)this.dryUniformSteps);
                float angleIncrement = (this.dryUniformSteps > 1) ? (float)(this.dryMaxRotationUniform - this.dryMinRotationUniform) / (float)(this.dryUniformSteps - 1) : 0f;
                for (int i = 0; i < this.dryUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<AngularTreeScript>().ShapeShift("single");

                        if (dryAngularBot)
                        {
                            go.GetComponent<AngularTreeScript>().ChangeBottomRing((float)this.dryMinRotationUniform + (i * angleIncrement));
                        }
                        else if (dryAngularTop)
                        {
                            go.GetComponent<AngularTreeScript>().ChangeTopRing((float)this.dryMinRotationUniform + (i * angleIncrement));
                        }

                        go.GetComponent<DryTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.dryTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.dryGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.dryTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.dryMu, this.drySigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.drySigma), this.dryMinRotationGaussian, this.dryMaxRotationGaussian));
                    go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("single");
                    if (dryAngularBot)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeBottomRing(ro);
                    }
                    else if (dryAngularTop)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeTopRing(ro);
                    }
                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.dryFixed)
            {
                for (int j = 0; j < dryTrees; j++)
                {
                    go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("single");
                    if (dryAngularBot)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeBottomRing(dryFixedFloat);
                    }
                    else if (dryAngularTop)
                    {
                        go.GetComponent<AngularTreeScript>().ChangeTopRing(dryFixedFloat);
                    }

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }
        else if (this.dryDoubleAngular)
        {
            if (this.dryUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.dryTrees / (float)this.dryUniformSteps);
                float angleIncrement = (this.dryUniformSteps > 1) ? (float)(this.dryMaxRotationUniform - this.dryMinRotationUniform) / (float)(this.dryUniformSteps - 1) : 0f;
                for (int i = 0; i < this.dryUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<AngularTreeScript>().ShapeShift("double");

                        go.GetComponent<AngularTreeScript>().ChangeBottomRing((float)this.dryMinRotationUniform + (i * angleIncrement));
                        go.GetComponent<AngularTreeScript>().ChangeTopRing((float)this.dryMinRotationUniform + (i * angleIncrement));

                        go.GetComponent<DryTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.dryTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.dryGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.dryTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.dryMu, this.drySigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.drySigma), this.dryMinRotationGaussian, this.dryMaxRotationGaussian));

                    go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("double");

                    go.GetComponent<AngularTreeScript>().ChangeBottomRing(ro);
                    go.GetComponent<AngularTreeScript>().ChangeTopRing(ro);

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.dryFixed)
            {
                for (int j = 0; j < dryTrees; j++)
                {
                    go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("double");

                    go.GetComponent<AngularTreeScript>().ChangeBottomRing(dryFixedFloat);
                    go.GetComponent<AngularTreeScript>().ChangeTopRing(dryFixedFloat);

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }
        else if (this.drySpherical)
        {
            if (this.dryUniform)
            {
                int treesPerStep = Mathf.RoundToInt((float)this.dryTrees / (float)this.dryUniformSteps);
                float angleIncrement = (this.dryUniformSteps > 1) ? (float)(this.dryMaxRotationUniform - this.dryMinRotationUniform) / (float)(this.dryUniformSteps - 1) : 0f;
                for (int i = 0; i < this.dryUniformSteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                        go.GetComponent<AngularTreeScript>().ShapeShift("spherical");

                        go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap((float)this.dryMinRotationUniform + (i * angleIncrement)));

                        go.GetComponent<DryTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.dryTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            if (this.dryGaussian)
            {
                System.Random r = new System.Random();
                float d, ro;
                for (int i = 0; i < this.dryTrees; i++)
                {
                    d = (float)RandomExtensions.NextGaussian(r, this.dryMu, this.drySigma);
                    ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.drySigma), this.dryMinRotationGaussian, this.dryMaxRotationGaussian));

                    go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("spherical");

                    go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(ro));

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
            if (this.dryFixed)
            {
                for (int j = 0; j < dryTrees; j++)
                {
                    go = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<AngularTreeScript>().ShapeShift("spherical");

                    go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(dryFixedFloat));

                    go.GetComponent<DryTreeScript>().Init();
                    go.transform.parent = this.treeParent.transform;
                    this.dryTreesQueue.Add(go);
                    go.SetActive(false);
                }
            }
        }

        /*if (this.dryUniform && !this.dryGaussian && !this.dryFixed)
        {
            int treesPerStep = Mathf.RoundToInt((float)this.dryTrees / (float)this.drySteps);
            //float angleIncrement = (float)(this.dryMaxRotationUniform - this.dryMinRotationUniform) / (float)(this.drySteps - 1);
            float angleIncrement = (this.drySteps > 1) ? (float)(this.dryMaxRotationUniform - this.dryMinRotationUniform) / (float)(this.drySteps - 1) : 0f;


            if (!this.dryAngular)
            {
                for (int i = 0; i < this.drySteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                        //go.GetComponent<DryTreeScript>().Init();
                        go.GetComponent<DryTreeScript>().SetShaderRotation((float)this.dryMinRotationUniform + (i * angleIncrement));
                        go.GetComponent<DryTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.dryTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            else if (this.dryAngular)
            {
                for (int i = 0; i < this.drySteps; i++)
                {
                    for (int j = 0; j < treesPerStep; j++)
                    {
                        go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                        //go.GetComponent<DryTreeScript>().Init();
                        if (dryAngularBot)
                        {
                            go.GetComponent<DryTreeScript>().ChangeBottomRing((float)this.dryMinRotationUniform + (i * angleIncrement));
                        }
                        else if (dryAngularTop)
                        {
                            go.GetComponent<DryTreeScript>().ChangeTopRing((float)this.dryMinRotationUniform + (i * angleIncrement));
                        }
                        go.GetComponent<DryTreeScript>().Init();
                        go.transform.parent = this.treeParent.transform;
                        this.dryTreesQueue.Add(go);
                        go.SetActive(false);
                    }
                }
            }
        }
        else if (this.dryGaussian && !this.dryUniform && !this.dryFixed)
        {
            System.Random r = new System.Random();
            float d, ro;
            for (int i = 0; i < this.dryTrees; i++)
            {
                d = (float)RandomExtensions.NextGaussian(r, this.dryMu, this.drySigma);
                ro = (map(System.Convert.ToSingle(Mathf.Abs(d)), 0f, (2.14f * this.drySigma), this.dryMinRotationGaussian, this.dryMaxRotationGaussian));
                
                if (dryAngular)
                {
                    go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                    //go.GetComponent<DryTreeScript>().Init();
                    if (dryAngularBot)
                    {
                        go.GetComponent<DryTreeScript>().ChangeBottomRing(ro);
                    }
                    else if (dryAngularTop)
                    {
                        go.GetComponent<DryTreeScript>().ChangeTopRing(ro);
                    }
                }
                else
                {
                    go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                    go.GetComponent<DryTreeScript>().SetShaderRotation(ro);
                }
                go.GetComponent<DryTreeScript>().Init();
                go.transform.parent = this.treeParent.transform;
                this.dryTreesQueue.Add(go);
                go.SetActive(false);
            }
        }
        else if (this.dryFixed && !this.dryUniform && !this.dryGaussian)
        {
            for (int j = 0; j < this.dryTrees; j++)
            {

                if (dryAngular)
                {
                    go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);
                    //go.GetComponent<DryTreeScript>().Init();
                    if (dryAngularBot)
                    {
                        go.GetComponent<DryTreeScript>().ChangeBottomRing(dryAngularAngle);
                    }
                    else if (dryAngularTop)
                    {
                        go.GetComponent<DryTreeScript>().ChangeTopRing(dryAngularAngle);
                    }
                }
                else
                {
                    go = (GameObject)Instantiate(this.dryTreePrefab, Vector3.zero, Quaternion.identity);

                    go.GetComponent<DryTreeScript>().ChangeTexture(dryTexture);
                }
                go.GetComponent<DryTreeScript>().Init();
                go.transform.parent = this.treeParent.transform;
                this.dryTreesQueue.Add(go);
                go.SetActive(false);
            }
        }**/

        Shuffle(ref this.dryTreesQueue);
        print("Spawn dry in: " + (System.DateTime.Now-g));
    }

	public void SaveScenario()
	{
        Shader gradientShader = (Shader.Find("Custom/Gradient"));

        if (this.saveScenarioFile != "")
        {
            System.DateTime d = System.DateTime.Now;

            using (StreamWriter outfile = new StreamWriter(PlayerPrefs.GetString("scenarioFolder") + "/" + this.saveScenarioFile))
            {
                outfile.WriteLine("<document>");
                outfile.WriteLine("<config>");
                outfile.WriteLine("<waterConfig>");

                outfile.Write("<training>");
                outfile.Write(this.waterTraining);
                outfile.WriteLine("</training>");
                
                outfile.Write("<distType>");
                if (this.waterFixed)
                {
                    outfile.Write("f");
                }
                else if (this.waterUniform)
                {
                    outfile.Write("u");
                }
                else if (this.waterGaussian)
                {
                    outfile.Write("g");
                }
                outfile.WriteLine("</distType>");

                if (this.waterTextured)
                {
                    outfile.Write("<waterTex>");
                    outfile.Write(this.waterTextureFile);
                    outfile.WriteLine("</waterTex>");
                }

                if (waterAngularTop || waterAngularBot || waterDoubleAngular || waterSpherical)
                {
                    outfile.Write("<waterAngular>");

                        if (this.waterAngularBot)
                        {
                            outfile.Write("bot");
                        }
                        else if (this.waterAngularTop)
                        {
                            outfile.Write("top");
                        }
                        else if (this.waterDoubleAngular)
                        {
                            outfile.Write("double");
                        }
                        else if (this.waterSpherical)
                        {
                            outfile.Write("spherical");
                        }

                    outfile.WriteLine("</waterAngular>");
                }
  
                outfile.WriteLine("</waterConfig>");

                outfile.WriteLine("<dryConfig>");
                outfile.Write("<distType>");
                if (this.dryFixed)
                {
                    outfile.Write("f");
                }
                else if (this.dryUniform)
                {
                    outfile.Write("u");
                }
                else if (this.dryGaussian)
                {
                    outfile.Write("g");
                }
                outfile.WriteLine("</distType>");

                if (this.dryFixed)
                {
                    outfile.Write("<dryTex>");
                    outfile.Write(this.dryTextureFile);
                    outfile.WriteLine("</dryTex>");
                }

                if (dryAngularTop || dryAngularBot || dryDoubleAngular || drySpherical)
                {
                    outfile.Write("<dryAngular>");

                        if (this.dryAngularBot)
                        {
                            outfile.Write("bot");
                        }
                        else if (this.dryAngularTop)
                        {
                            outfile.Write("top");
                        }
                        else if (this.dryDoubleAngular)
                        {
                            outfile.Write("double");
                        }
                        else if (this.drySpherical)
                        {
                            outfile.Write("spherical");
                        }

                    outfile.WriteLine("</dryAngular>");
                }

                outfile.WriteLine("</dryConfig>");
                outfile.WriteLine("</config>");

                outfile.WriteLine("<trees>");

                foreach (Transform tr in this.treeParent.transform)
                {
                    GameObject go = tr.gameObject;

                    outfile.WriteLine("<t>");

                    outfile.Write("<w>");
                    if (go.tag == "water" || go.tag == "waterAngular")
                        outfile.Write("1");
                    else
                        outfile.Write("0");
                    outfile.WriteLine("</w>");

                    outfile.WriteLine("<pos>" + tr.position.x + ";" + tr.position.y + ";" + tr.position.z + "</pos>");

                    if (go.tag == "water" || go.tag == "waterAngular")
                    {
                        WaterTreeScript wts = go.GetComponent<WaterTreeScript>();

                        if (wts.angular)
                        {
                            outfile.Write("<a>");
                            outfile.Write(go.GetComponent<AngularTreeScript>().angle);
                            outfile.WriteLine("</a>");
                        }
                        else if (wts.gradient)
                        {
                            outfile.Write("<d>");
                            outfile.Write(wts.ReturnAngle());
                            outfile.WriteLine("</d>");
                        }
                        else if (wts.texture)
                        {
                            outfile.Write("<tex>");
                            outfile.Write(1);
                            outfile.WriteLine("</tex>");
                        }
                    }

                    else if (go.tag == "dry" || go.tag == "dryAngular")
                    {
                        DryTreeScript dts = go.GetComponent<DryTreeScript>();

                        if (dts.angular)
                        {
                            outfile.Write("<a>");
                            outfile.Write(go.GetComponent<AngularTreeScript>().angle);
                            outfile.WriteLine("</a>");
                        }
                        else if (dts.gradient)
                        {
                            outfile.Write("<d>");
                            outfile.Write(dts.ReturnAngle());
                            outfile.WriteLine("</d>");
                        }
                        else if (dts.texture)
                        {
                            outfile.Write("<tex>");
                            outfile.Write(1);
                            outfile.Write("</tex>");
                        }
                    }
                   
                    outfile.WriteLine("</t>");

                }

                outfile.WriteLine("</trees>");

                outfile.WriteLine("<walls>");

                foreach (Transform tr in this.wallParent.GetComponentsInChildren<Transform>())
                {
                    GameObject go = tr.gameObject;
                    if (go.tag != "initWall")
                    {

                        outfile.WriteLine("<wall>");
                        outfile.WriteLine("<pos>" + tr.position.x + ";" + tr.position.y + ";" + tr.position.z + "</pos>");
                        outfile.WriteLine("<rot>" + tr.eulerAngles.x + ";" + tr.eulerAngles.y + ";" + tr.eulerAngles.z + "</rot>");
                        outfile.WriteLine("<scale>" + tr.localScale.x + ";" + tr.localScale.y + ";" + tr.localScale.z + "</scale>");
                        outfile.WriteLine("</wall>");
                    }
                }

                outfile.WriteLine("</walls>");

                outfile.Write("</document>");


                print("time: " + (System.DateTime.Now - d));
            }
        }
        else
        {
            StartCoroutine(ShowError("ERROR: File name empty"));
        }
	}

	public void LoadScenario()
	{
        if (File.Exists(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile))// && this.movementRecorderScript.GetReplayFileName() != "")
        {
            this.scenarioLoaded = true;

            XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
            xmlDoc.LoadXml(File.ReadAllText(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile, ASCIIEncoding.ASCII)); // load the file.

            XmlNodeList waterConfigList = xmlDoc.SelectNodes("document/config/waterConfig");
            foreach (XmlNode xn in waterConfigList)
            {
                string waterTrainingXML = xn["training"].InnerText;

                if (waterTrainingXML == "True")
                {
                    this.waterTraining = true;
                }
                else
                {
                    this.waterTraining = false;
                }

                string waterDistType = xn["distType"].InnerText;
                if (waterDistType == "f" && xn["waterTex"] != null)
                {
                    this.waterTextureFile_LS = xn["waterTex"].InnerText;
                }

                if (xn["waterAngular"] != null)
                {
                    string angularPos = xn["waterAngular"].InnerText;

                    if (angularPos == "top")
                    {
                        this.waterTop_LS = true;
                        this.waterBot_LS = false;
                        this.waterDouble_LS = false;
                        this.waterSpherical_LS = false;
                    }
                    else if (angularPos == "bot")
                    {
                        this.waterBot_LS = true;
                        this.waterTop_LS = false;
                        this.waterDouble_LS = false;
                        this.waterSpherical_LS = false;
                    }
                    else if (angularPos == "double")
                    {
                        this.waterDouble_LS = true;
                        this.waterTop_LS = false;
                        this.waterBot_LS = false;
                        this.waterSpherical_LS = false;
                    }
                    else if (angularPos == "spherical")
                    {
                        this.waterSpherical_LS = true;
                        this.waterDouble_LS = false;
                        this.waterTop_LS = false;
                        this.waterBot_LS = false;
                        
                    }
                }
            }

            XmlNodeList dryConfigList = xmlDoc.SelectNodes("document/config/dryConfig");
            foreach (XmlNode xn in dryConfigList)
            {
                string dryDistType = xn["distType"].InnerText;
                if (dryDistType == "f" && xn["dryTex"] != null)
                {
                    this.dryTextureFile_LS = xn["dryTex"].InnerText;
                }

                if (xn["dryAngular"] != null)
                {
                    string angularPos = xn["dryAngular"].InnerText;
                    {
                        if (angularPos == "top")
                        {
                            this.dryTop_LS = true;
                            this.dryBot_LS = false;
                            this.dryDouble_LS = false;
                            this.drySpherical_LS = false;
                        }
                        else if (angularPos == "bot")
                        {
                            this.dryBot_LS = true;
                            this.dryTop_LS = false;
                            this.dryDouble_LS = false;
                            this.drySpherical_LS = false;
                        }
                        else if (angularPos == "double")
                        {
                            this.dryDouble_LS = true;
                            this.dryBot_LS = false;
                            this.dryTop_LS = false;
                            this.drySpherical_LS = false;
                        }
                        else if (angularPos == "spherical")
                        {
                            this.drySpherical_LS = true;
                            this.dryTop_LS = false;
                            this.dryBot_LS = false;
                            this.dryDouble_LS = false;
                        }
                    }
                }  
            }
            
            XmlNodeList levelsList = xmlDoc.GetElementsByTagName("t"); // array of the level nodes.

            foreach (XmlNode node in levelsList)
            {
                bool water = false;
                Vector3 v = Vector3.zero;
                XmlNodeList levelcontent = node.ChildNodes;

                GameObject go;

                bool angular, gradient, texture;
                gradient = false;
                angular = false;
                texture = false;

                foreach (XmlNode val in levelcontent)
                {
                    if (val.Name == "w")
                    {
                        water = (val.InnerText == "1") ? true : false;
                    }
                    if (val.Name == "pos")
                    {
                        float x, y, z;
                        float.TryParse(val.InnerText.Split(';')[0], out x);
                        float.TryParse(val.InnerText.Split(';')[1], out y);
                        float.TryParse(val.InnerText.Split(';')[2], out z);

                        v = new Vector3(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));
                    }
                    if (val.Name == "d")
                    {
                        float.TryParse(val.InnerText, out this.deg_LS);
                        gradient = true;
                    }
                    else if (val.Name == "a")
                    {
                        float.TryParse(val.InnerText, out this.angle_LS);
                        angular = true;
                    }
                    else if (val.Name == "tex")
                    {
                        texture = true;
                    }
                }
                if (water)
                {
                        if (gradient)
                        {
                            go = (GameObject)Instantiate(this.waterTreePrefab, v, Quaternion.identity);
                            go.GetComponent<WaterTreeScript>().SetShaderRotation(this.deg_LS);
                            go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
                            go.transform.parent = treeParent.transform;
                            go.SetActive(false);
                        }
                        else if (texture)
                        {
                            go = (GameObject)Instantiate(this.waterTreePrefab, v, Quaternion.identity);
                            go.GetComponent<WaterTreeScript>().ChangeTexture(LoadPNG(this.waterTextureFile_LS));
                            go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
                            go.transform.parent = treeParent.transform;
                            go.SetActive(false);
                        }
                        else if (angular)
                        {   
                            if (waterBot_LS)
                            {
                                go = (GameObject)Instantiate(this.waterAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("single");
                                go.GetComponent<AngularTreeScript>().ChangeBottomRing(angle_LS);
                                go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                            else if (waterTop_LS)
                            {
                                go = (GameObject)Instantiate(this.waterAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("single");
                                go.GetComponent<AngularTreeScript>().ChangeTopRing(angle_LS);
                                go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                            else if (waterDouble_LS)
                            {
                                go = (GameObject)Instantiate(this.waterAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("double");
                                go.GetComponent<AngularTreeScript>().ChangeBottomRing(angle_LS);
                                go.GetComponent<AngularTreeScript>().ChangeTopRing(angle_LS);
                                go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                            else if (waterSpherical_LS)
                            {
                                go = (GameObject)Instantiate(this.waterAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("spherical");
                                go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(angle_LS));
                                go.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                        }
                }

                else
                {
                    //((GameObject)Instantiate(this.dryTreePrefab, v, Quaternion.identity)).transform.parent = treeParent.transform;
                        if (gradient)
                        {
                            go = (GameObject)Instantiate(this.dryTreePrefab, v, Quaternion.identity);
                            go.GetComponent<DryTreeScript>().SetShaderRotation(this.deg_LS);
                            go.transform.parent = treeParent.transform;
                            go.SetActive(false);
                        }
                        else if (texture)
                        {
                            go = (GameObject)Instantiate(this.dryTreePrefab, v, Quaternion.identity);
                            go.GetComponent<DryTreeScript>().ChangeTexture(LoadPNG(this.dryTextureFile_LS));
                            go.transform.parent = treeParent.transform;
                            go.SetActive(false);
                        }
                        else if (angular)
                        {
                            if (dryBot_LS)
                            {
                                go = (GameObject)Instantiate(this.dryAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("single");
                                go.GetComponent<AngularTreeScript>().ChangeBottomRing(angle_LS);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                            else if (dryTop_LS)
                            {
                                go = (GameObject)Instantiate(this.dryAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("single");
                                go.GetComponent<AngularTreeScript>().ChangeTopRing(angle_LS);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                            else if (dryDouble_LS)
                            {
                                go = (GameObject)Instantiate(this.dryAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("double");
                                go.GetComponent<AngularTreeScript>().ChangeBottomRing(angle_LS);
                                go.GetComponent<AngularTreeScript>().ChangeTopRing(angle_LS);
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                            else if (drySpherical_LS)
                            {
                                go = (GameObject)Instantiate(this.dryAngularTreePrefab, v, Quaternion.identity);
                                go.GetComponent<AngularTreeScript>().ShapeShift("spherical");
                                go.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(angle_LS));
                                go.transform.parent = treeParent.transform;
                                go.SetActive(false);
                            }
                        }
                }
            }

            XmlNodeList wallConfigList = xmlDoc.GetElementsByTagName("wall");
            foreach (XmlNode xn in wallConfigList)
            {
                Vector3 v = Vector3.zero;
                Vector3 wallRotation = Vector3.zero;
                Vector3 wallScale = Vector3.zero;

                XmlNodeList wallConfigContent = xn.ChildNodes;

                GameObject go;

                foreach (XmlNode val in wallConfigContent)
                {
                    if (val.Name == "pos")
                    {
                        float x, y, z;
                        float.TryParse(val.InnerText.Split(';')[0], out x);
                        float.TryParse(val.InnerText.Split(';')[1], out y);
                        float.TryParse(val.InnerText.Split(';')[2], out z);

                        v = new Vector3(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));
                    }
                    if (val.Name == "rot")
                    {
                        float x, y, z;
                        
                        float.TryParse(val.InnerText.Split(';')[0], out x);
                        float.TryParse(val.InnerText.Split(';')[1], out y);
                        float.TryParse(val.InnerText.Split(';')[2], out z);

                        wallRotation = new Vector3(x, y, z);
                    }
                    if (val.Name == "scale")
                    {
                        float x, y, z;

                        float.TryParse(val.InnerText.Split(';')[0], out x);
                        float.TryParse(val.InnerText.Split(';')[1], out y);
                        float.TryParse(val.InnerText.Split(';')[2], out z);

                        wallScale = new Vector3(x, y, z);
                    }
                }

                go = (GameObject)Instantiate(this.wallPrefab, v, Quaternion.identity);
                go.transform.eulerAngles = wallRotation;
                go.transform.localScale += wallScale;

                go.transform.parent = wallParent.transform;

            }

            TriggerBatchLoader();

            this.errorText.text = "";
        }
        else if (!File.Exists(PlayerPrefs.GetString("scenarioFolder") + "/" + this.loadScenarioFile))
        {
            StartCoroutine(ShowError("ERROR: File does not exist"));
        }
        else if (this.movementRecorderScript.GetReplayFileName() == "")
        {
            StartCoroutine(ShowError("ERROR : Replay file does not exist"));
        }
    }

    public void SpawnWall()
    {
        this.placingWall = !this.placingWall;
        this.placingDryTree = false;
        this.placingWaterTree = false;
        this.sceneEditing = false;

        if (this.placingWall)
        {
            this.spawnedWall = (GameObject)Instantiate(wallPrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
            this.spawnedWall.transform.parent = this.wallParent.transform;    
        }
        else
        {
            Destroy(this.spawnedWall);
            wallButton.GetComponent<Image>().color = this.buttonColor;
        }
    }

    public void SpawnWaterTree()
    {
        this.placingWaterTree = !placingWaterTree;
        this.placingWall = false;
        this.placingDryTree = false;
        this.sceneEditing = false;

        if (this.placingWaterTree)
        {
            if (waterGradient)
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterTreePrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
                this.spawnedWaterTree.GetComponent<WaterTreeScript>().SetShaderRotation(waterFixedFloat);
            }
            else if (waterTextured)
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterTreePrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
                this.spawnedWaterTree.GetComponent<WaterTreeScript>().ChangeTexture(waterTexture);
            }
            else if (waterSpherical)
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ShapeShift("spherical");
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(waterFixedFloat));
            }
            else if (waterDoubleAngular)
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ShapeShift("double");
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ChangeBottomRing(waterFixedFloat);
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ChangeTopRing(waterFixedFloat);
            }
            else if (waterAngularBot)
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ShapeShift("single");
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ChangeBottomRing(waterFixedFloat);
            }
            else if (waterAngularTop)
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ShapeShift("single");
                this.spawnedWaterTree.GetComponent<AngularTreeScript>().ChangeTopRing(waterFixedFloat);
            }
            else
            {
                this.spawnedWaterTree = (GameObject)Instantiate(this.waterTreePrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
                this.spawnedWaterTree.GetComponent<WaterTreeScript>().SetShaderRotation(waterFixedFloat);
            }

            this.spawnedWaterTree.GetComponent<WaterTreeScript>().SetForTraining(waterTraining);
            this.spawnedWaterTree.GetComponent<WaterTreeScript>().Init();
            this.spawnedWaterTree.transform.parent = this.treeParent.transform; 
        }
        else
        {
            Destroy(this.spawnedWaterTree);
            waterTreeButton.GetComponent<Image>().color = this.buttonColor;
        }
    }

    public void SpawnDryTree()
    {
        this.placingDryTree = !this.placingDryTree;
        this.placingWall = false;
        this.placingWaterTree = false;
        this.sceneEditing = false;

        if (this.placingDryTree)
        {
            if (dryGradient)
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryTreePrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
                this.spawnedDryTree.GetComponent<DryTreeScript>().SetShaderRotation(dryFixedFloat);
            }
            else if (dryTextured)
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryTreePrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
                this.spawnedDryTree.GetComponent<DryTreeScript>().ChangeTexture(dryTexture);
            }
            else if (drySpherical)
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ShapeShift("spherical");
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ChangeSphereAngle(SphereAngleRemap(dryFixedFloat));
            }
            else if (dryDoubleAngular)
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ShapeShift("double");
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ChangeBottomRing(dryFixedFloat);
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ChangeTopRing(dryFixedFloat);
            }
            else if (dryAngularBot)
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ShapeShift("single");
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ChangeBottomRing(dryFixedFloat);
            }
            else if (dryAngularTop)
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryAngularTreePrefab, Vector3.zero, Quaternion.identity);
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ShapeShift("single");
                this.spawnedDryTree.GetComponent<AngularTreeScript>().ChangeTopRing(dryFixedFloat);
            }
            else
            {
                this.spawnedDryTree = (GameObject)Instantiate(this.dryTreePrefab, new Vector3(Camera.main.transform.position.x, 3, Camera.main.transform.position.z), Quaternion.identity);
                this.spawnedDryTree.GetComponent<DryTreeScript>().SetShaderRotation(dryFixedFloat);
            }

            this.spawnedDryTree.GetComponent<DryTreeScript>().Init();
            this.spawnedDryTree.transform.parent = this.treeParent.transform; 
        }
        else
        {
            Destroy(this.spawnedDryTree);
            dryTreeButton.GetComponent<Image>().color = this.buttonColor;
        }
    }

    public void ClearAll()
    {
        foreach (Transform tr in this.treeParent.transform)
        {
            GameObject.Destroy(tr.gameObject);
        }
        foreach (Transform tr in this.wallParent.transform)
        {
            GameObject.Destroy(tr.gameObject);
        }
    }
    
    //new set functions

    public void SetWaterTrees(string s)
    {
        int.TryParse(s, out this.waterTrees);
    }

    public void SetDryTrees(string s)
    {
        int.TryParse(s, out this.dryTrees);
    }

    public void SetWaterTraining(bool b)
    {
        this.waterTraining = b;
    }

    public void SetWaterAngularTop(bool b)
    {

        this.waterAngularTop = b;
    }

    public void SetWaterAngularBot(bool b)
    {
        this.waterAngularBot = b;
    }

    public void SetWaterDoubleAngular (bool b)
    {
        this.waterDoubleAngular = b;
    }

    public void SetDryDoubleAngular(bool b)
    {
        this.dryDoubleAngular = b;
    }

    public void SetDrySpherical(bool b)
    {
        this.drySpherical = b;
    }

    public void SetWaterSpherical (bool b)
    {
        this.waterSpherical = b;
    }

    public void SetWaterTextured (bool b)
    {
        this.waterTextured = b;
    }

    public void SetDryTextured(bool b)
    {
        this.dryTextured = b;
    }

    public void SetWaterGradient (bool b)
    {
        this.waterGradient = b;
    }

    public void SetDryGradient(bool b)
    {
        this.dryGradient = b;
    }

    public void SetWaterFixed(bool b)
    {
        this.waterFixed = b;
    }

    public void SetWaterFixedValue(string s)
    {
        if (this.waterTextured)
        {
            this.waterTextureFile = PlayerPrefs.GetString("textureFolder") + "/" + s + ".png";
            this.waterTexture = LoadPNG(waterTextureFile);
            if (waterTexture != null)
            {
                waterImage.sprite = Sprite.Create(waterTexture, new Rect(0, 0, 128, 128), new Vector2(0, 0), 1.0f);
            }
        }
        else
        {
            float.TryParse(s, out this.waterFixedFloat);
        }
    }

    public void SetDryFixedValue(string s)
    {
        if (this.dryTextured)
        {
            this.dryTextureFile = PlayerPrefs.GetString("textureFolder") + "/" + s + ".png";
            this.dryTexture = LoadPNG(dryTextureFile);
            if (dryTexture != null)
            {
                dryImage.sprite = Sprite.Create(dryTexture, new Rect(0, 0, 128, 128), new Vector2(0, 0), 1.0f);
            }
        }
        else
        {
            float.TryParse(s, out this.dryFixedFloat);
        }
    }

    public void SetWaterUniform(bool b)
    {
        this.waterUniform = b;
    }

        public void SetWaterUniformSteps(string i)
        {
            int.TryParse(i, out this.waterUniformSteps);
        }

        public void SetDryUniformSteps(string i)
        {
            int.TryParse(i, out this.dryUniformSteps);
        }

        public void SetWaterMinRotationUniform(string f)
        {
            float.TryParse(f, out this.waterMinRotationUniform);
        }

        public void SetWaterMaxRotationUniform(string f)
        {
            float.TryParse(f, out this.waterMaxRotationUniform);
        }

    public void SetWaterGaussian(bool b)
    {
        this.waterGaussian = b;
    }

        public void SetWaterMu(string f)
        {
            float.TryParse(f, out this.waterMu);
        }

        public void SetWaterSigma(string f)
        {
            float.TryParse(f, out this.waterSigma);
        }

        public void SetWaterMinRotationGaussian(string f)
        {
            float.TryParse(f, out this.waterMinRotationGaussian);
        }

        public void SetWaterMaxRotationGaussian(string f)
        {
            float.TryParse(f, out this.waterMaxRotationGaussian);
        }

    public float SphereAngleRemap (float f)
    {
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



    //end of new set functions
    

    public void SetDryMu(string f)
    {
        float.TryParse(f, out this.dryMu);
    }

    public void SetDrySigma(string f)
    {
        float.TryParse(f, out this.drySigma);
    }

    

    public void SetDryMinRotationUniform(string f)
    {
        float.TryParse(f, out this.dryMinRotationUniform);
    }

    public void SetDryMaxRotationUniform(string f)
    {
        float.TryParse(f, out this.dryMaxRotationUniform);
    }


    




    public void SetWaterAngular(bool b)
    {
        this.waterAngular = b;
    }

    

    public void SetDryAngularTop(bool b)
    {
        this.dryAngularTop = b;
    }

    public void SetDryAngularBot(bool b)
    {
        this.dryAngularBot = b;
    }

    public void SetDryUniform(bool b)
    {
        this.dryUniform = b;
    }

    public void SetDryGaussian(bool b)
    {
        this.dryGaussian = b;
    }

    public void SetDryFixed(bool b)
    {
        this.dryFixed = b;
    }

    public void SetDryAngular(bool b)
    {
        this.dryAngular = b;
    }

    public void SetWaterSpawnTexture(bool b)
    {
        this.spawnWaterTexture = b;
    }

    public void SetWaterSpawnPattern(bool b)
    {
        this.spawnWaterPattern = b;
    }

    public void SetDrySpawnTexture(bool b)
    {
        this.spawnDryTexture = b;
    }

    public void SetDrySpawnPattern(bool b)
    {
        this.spawnDryPattern = b;
    }




    public void SetDrySteps(string i)
    {
        int.TryParse(i, out this.dryUniformSteps);
    }

    public void SetDryMinRotationGaussian(string f)
    {
        float.TryParse(f, out this.dryMinRotationGaussian);
    }

    public void SetDryMaxRotationGaussian(string f)
    {
        float.TryParse(f, out this.dryMaxRotationGaussian);
    }

   
    public void SetSpawnWaterAngular(bool b)
    {
        this.spawnWaterAngular = b;
    }

    public void SetSpawnWaterAngularBot(bool b)
    {
        this.spawnWaterAngularBot = b;
    }

    public void SetSpawnWaterAngularTop(bool b)
    {
        this.spawnWaterAngularTop = b;
    }

    public void SetSpawnWaterAngle(string f)
    {
        float.TryParse(f, out this.spawnWaterAngularAngle);
    }

   
    public void SetSpawnDryAngular(bool b)
    {
        this.spawnDryAngular = b;
    }

    public void SetSpawnDryAngularBot(bool b)
    {
       this.spawnDryAngularBot = b;
    }

    public void SetSpawnDryAngularTop(bool b)
    {
        this.spawnDryAngularTop = b;
    }

    public void SetSpawnDryAngle(string f)
    {
        float.TryParse(f, out this.spawnDryAngularAngle);
    }

    public void SetWaterPattern(string f)
    {
        float.TryParse(f, out this.spawnWaterDegree);
    }

    public void SetDryPattern(string f)
    {
        float.TryParse(f, out this.spawnDryDegree);
    }



    public void SetGridWidth(string s)
    {
        int.TryParse(s, out this.gridWidth);
    }

    public void SetGridHeight(string s)
    {
        int.TryParse(s, out this.gridHeight);
    }

    public void SetGridCenterX(string s)
    {
        float.TryParse(s, out this.gridCenter.x);
    }

    public void SetGridCenterY(string s)
    {
        float.TryParse(s, out this.gridCenter.y);
    }

    public void SetLoadScenarioName(string s)
    {
        this.loadScenarioFile = s;
    }

    public void SetSaveScenarioName(string s)
    {
        this.saveScenarioFile = s;
    }

    public void ToggleSceneEditable()
    {
        if (!sceneEditing)
        {
            this.sceneEditing = true;
            this.placingWaterTree = false;
            this.placingWall = false;
            this.placingDryTree = false;
            ColorBlock c = GameObject.FindGameObjectWithTag("editButton").GetComponent<Button>().colors;
            c.normalColor = Color.green;
            GameObject.FindGameObjectWithTag("editButton").GetComponent<Button>().colors = c;
        }
        else
        {
            this.sceneEditing = false;
            this.placingWaterTree = false;
            this.placingWall = false;
            this.placingDryTree = false;
            ColorBlock c = GameObject.FindGameObjectWithTag("editButton").GetComponent<Button>().colors;
            c.normalColor = ogButtonColor;
            GameObject.FindGameObjectWithTag("editButton").GetComponent<Button>().colors = c;
        }
    }

    public void Play()
    {
        //Application.LoadLevel("miceVR");
        Application.LoadLevel("mouseVR");
    }

    private float map(float s, float a1, float a2, float b1, float b2)
    {
        return Mathf.Clamp(b1 + (s - a1) * (b2 - b1) / (a2 - a1), b1, b2);
    }

    private void Shuffle(ref List<GameObject> list)
    {
        System.Random rng = new System.Random();
        GameObject value = null;
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            value = (GameObject) list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(128, 128);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    private IEnumerator ShowError(string s)
    {
        this.errorText.text = s;
        yield return new WaitForSeconds(.1f);
        this.errorText.text = "";
        yield return new WaitForSeconds(.1f);
        this.errorText.text = s;
        yield return new WaitForSeconds(.1f);
        this.errorText.text = "";
        yield return new WaitForSeconds(.1f);
        this.errorText.text = s;
        yield return new WaitForSeconds(2f);
        this.errorText.text = "";

    }

    private IEnumerator ShowStatus(string s)
    {
        this.statusText.text = s;
        yield return new WaitForSeconds(.1f);
        this.statusText.text = "";
        yield return new WaitForSeconds(.1f);
        this.statusText.text = s;
        yield return new WaitForSeconds(.1f);
        this.statusText.text = "";
        yield return new WaitForSeconds(.1f);
        this.statusText.text = s;
        yield return new WaitForSeconds(2f);
        this.statusText.text = "";

    }
}
