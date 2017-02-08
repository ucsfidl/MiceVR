using UnityEngine;
using System.Collections;

public class HoverScript : MonoBehaviour
{
    private Renderer rend;
    private Color ogColor;
    private Transform tr;
    
    private bool selected;
    private bool hovered;

    private GameObject generator;
    private bool HoverStatus;

    private bool wall;
    private bool wallSelected;

    private WaterTreeScript wts;
    private DryTreeScript dts;
    private AngularTreeScript ats;

    static bool itemSelected;

    private GeneratorScript generatorScript;

    // Use this for initialization
    void Start()
    {
        if (this.gameObject.tag == "wall")
        {
            ogColor = GetComponent<Renderer>().material.color;
            rend = GetComponent<Renderer>();
            tr = GetComponent<Transform>();

            wall = true;
        }
        else
        {
            wall = false;

            if (this.gameObject.tag == "water")
            {
                wts = GetComponent<WaterTreeScript>();
            }
            else if (this.gameObject.tag == "dry")
            {
                dts = GetComponent<DryTreeScript>();
            }
            else if (this.gameObject.tag == "waterAngular" || this.gameObject.tag == "dryAngular")
            {
                ats = GetComponent<AngularTreeScript>();
            }
        }

        generator = GameObject.FindGameObjectWithTag("generator");
        generatorScript = generator.GetComponent<GeneratorScript>();

        selected = false;
        itemSelected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (generatorScript != null)
        {
            HoverStatus = generatorScript.sceneEditing;

            if (HoverStatus)
            {
                WallOrTree();
                ColorChanger();

                if (selected)
                {
                    tr = this.gameObject.GetComponent<Transform>();

                    //move object with mouse position using raycast to terrain to convert mouse position on screen to world position coordinates
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.collider.name == "Terrain")
                        {
                            Vector3 v = hit.point;
                            v.y = 0f;
                            tr.position = v;
                        }
                    }

                    //object deletion

                    if (Input.GetKey(KeyCode.Delete))
                    {
                        Object.Destroy(this.gameObject);
                    }
                }

                if (HoverStatus && wallSelected && selected)
                {
                    if (Input.GetAxis("Mouse ScrollWheel") > 0)
                    {
                        if (!Input.GetKey(KeyCode.RightShift) && !Input.GetKey(KeyCode.LeftShift))
                        {
                            tr.Rotate(Vector3.up, 5f);
                        }
                        else
                        {
                            tr.localScale += new Vector3(0, 0, 1.0f);
                        }
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") < 0)
                    {
                        if (!Input.GetKey(KeyCode.RightShift) && !Input.GetKey(KeyCode.LeftShift))
                        {
                            tr.Rotate(Vector3.up, -5f);
                        }
                        else
                        {
                            tr.localScale += new Vector3(0, 0, -1.0f);
                        }
                    }
                }
            }
        }
    }

    void ColorChanger()
    {
        if (HoverStatus)
        {
            if (wall)
            {
                if (selected)
                {
                    rend.material.color = Color.green;
                }
                else if (hovered && !itemSelected)
                {
                    rend.material.color = Color.red;
                }
                else
                {
                    Reset();
                }
            }
            else
            {
                if (selected)
                {
                    if (wts != null)
                    {
                        wts.ChangeColor(Color.green);
                    }
                    else if (dts != null)
                    {
                        dts.ChangeColor(Color.green);
                    }
                    else if (ats != null)
                    {
                        ats.ChangeColor(Color.green);
                    }
                }
                else if (hovered && !itemSelected)
                {
                    if (wts != null)
                    {
                        wts.ChangeColor(Color.red);
                    }
                    else if (dts != null)
                    {
                        dts.ChangeColor(Color.red);
                    }
                    else if (ats != null)
                    {
                        ats.ChangeColor(Color.red);
                    }
                }
                else
                {
                    if (wts != null)
                    {
                        wts.ResetColor();
                    }
                    else if (dts != null)
                    {
                        dts.ResetColor();
                    }
                    else if (ats != null)
                    {
                        ats.ResetColor();
                    }
                }
            }
        }
    }

    void WallOrTree()
    {
        if (this.gameObject != null)
        {
            if (this.gameObject.tag == "wall")
            {
                wallSelected = true;
            }
            else if (this.gameObject.tag == "dry" || this.gameObject.tag == "water")
            {
                wallSelected = false;
            }
        }
        else
        {
            wallSelected = false;
        }
    }

    void Reset()
    {
        if (rend != null && rend.material.color != ogColor)
        {
            rend.material.color = ogColor;
        }
    }

    void OnMouseEnter()
    {
        if (HoverStatus && !selected && !itemSelected)
        {
            hovered = true;
        }
    }
    
    void OnMouseExit()
    {
        if (HoverStatus)
        {
            if (!selected && wall)
            {
                Reset();
            }
            else if (!selected && !wall)
            {
                if (wts != null)
                {
                    wts.ResetColor();
                }
                else if (dts != null)
                {
                    dts.ResetColor();
                }
                else if (ats != null)
                {
                    ats.ResetColor();
                }
            }
            hovered = false;
        }
    }

    void OnMouseDown()
    {
        if (HoverStatus)
        {
            if (!selected && !itemSelected)
            {
                selected = true;
                itemSelected = true;
            }
            else if (selected)
            {
                selected = false;
                itemSelected = false;
            }
        }
    }
}