using UnityEngine;
using System.Collections;

public class VisibilityScript : MonoBehaviour
{

    private bool water, dry, waterAngular, dryAngular, wall;

    private HoverScript hoverScript;
    private AngularTreeScript angularTreeScript;
    private WaterTreeScript waterTreeScript;
    private DryTreeScript dryTreeScript;
    private CapsuleCollider col;

	// Use this for initialization
	void Start ()
    {
	    if (this.transform.parent.tag == "water")
        {
            water = true;
            dry = false;
            wall = false;
            waterAngular = false;
            dryAngular = false;

            waterTreeScript = this.transform.parent.GetComponent<WaterTreeScript>();
            hoverScript = this.transform.parent.GetComponent<HoverScript>();
            this.col = waterTreeScript.gameObject.GetComponent<CapsuleCollider>();
        }
        else if (this.transform.parent.tag == "waterAngular")
        {
            waterAngular = true;
            dryAngular = false;
            water = false;
            dry = false;
            wall = false;

            waterTreeScript = this.transform.parent.GetComponent<WaterTreeScript>();
            hoverScript = this.transform.parent.GetComponent<HoverScript>();
            angularTreeScript = this.transform.parent.GetComponent<AngularTreeScript>();
            this.col = waterTreeScript.gameObject.GetComponent<CapsuleCollider>();   
        }
        else if (this.transform.parent.tag == "dry")
        {
            water = false;
            dry = true;
            wall = false;
            waterAngular = false;
            dryAngular = false;

            dryTreeScript = this.transform.parent.GetComponent<DryTreeScript>();
            hoverScript = this.transform.parent.GetComponent<HoverScript>();
            this.col = dryTreeScript.gameObject.GetComponent<CapsuleCollider>();
        }
        else if (this.transform.parent.tag == "dryAngular")
        {
            dryAngular = true;
            waterAngular = false;
            water = false;
            dry = false;
            wall = false;

            dryTreeScript = this.transform.parent.GetComponent<DryTreeScript>();
            hoverScript = this.transform.parent.GetComponent<HoverScript>();
            angularTreeScript = this.transform.parent.GetComponent<AngularTreeScript>();
            this.col = dryTreeScript.gameObject.GetComponent<CapsuleCollider>();
        }
        else if (this.gameObject.tag == "wall")
        {
            water = false;
            dry = false;
            wall = true;
            waterAngular = false;
            dryAngular = false;

            hoverScript = this.gameObject.GetComponent<HoverScript>();
        }

        hoverScript.enabled = false;

        if( !this.GetComponent<Renderer>().isVisible )
        {
            ToggleScript(false);
        }
	}

    void ToggleScript(bool b)
    {
        if ( water )
        {
            waterTreeScript.enabled = b;
            hoverScript.enabled = b;
            this.col.enabled = b;
        }
        else if ( waterAngular )
        {
            waterTreeScript.enabled = b;
            hoverScript.enabled = b;
            angularTreeScript.enabled = b;
            this.col.enabled = b;
        }
        else if( dry )
        {
            dryTreeScript.enabled = b;
            hoverScript.enabled = b;
            this.col.enabled = b;
        }
        else if ( dryAngular )
        {
            dryTreeScript.enabled = b;
            hoverScript.enabled = b;
            angularTreeScript.enabled = b;
            this.col.enabled = b;
        }
        else if (wall)
        {
            hoverScript.enabled = b;
        }
    }

    void OnBecameInvisible()
    {
        ToggleScript(false);
    }
    void OnBecameVisible()
    {
        ToggleScript(true);
    }
}
