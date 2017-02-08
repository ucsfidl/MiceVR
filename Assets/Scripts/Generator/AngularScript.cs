using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AngularScript : MonoBehaviour {

    public Text waterFixedText, waterUniformMinText, waterUniformMaxText, waterGaussianMinText, waterGaussianMaxText;
    public Text dryFixedText, dryUniformMinText, dryUniformMaxText, dryGaussianMinText, dryGaussianMaxText;

    private string ogFixedText, ogUniformMinText, ogUniformMaxText, ogGaussianMinText, ogGaussianMaxText;
    private GeneratorScript generatorScript;

	// Use this for initialization
	void Start ()
    {
        generatorScript = GameObject.FindGameObjectWithTag("generator").GetComponent<GeneratorScript>();
        
        ogFixedText = waterFixedText.text;
        ogUniformMaxText = waterUniformMaxText.text;
        ogUniformMinText = waterUniformMinText.text;
        ogGaussianMaxText = waterGaussianMaxText.text;
        ogGaussianMinText = waterGaussianMinText.text;
	}
	
	// Update is called once per frame
	void Update ()
    {
        UIToggler();
	}

    void UIToggler ()
    {
        if (generatorScript.waterAngular)
        {
            waterFixedText.text = "1";
            waterUniformMinText.text = waterGaussianMinText.text = "Min. Angle";
            waterUniformMaxText.text = waterGaussianMaxText.text = "Max. Angle";
        }
        else if (waterFixedText.text != ogFixedText)
        {
            waterFixedText.text = ogFixedText;
            waterUniformMaxText.text = ogUniformMaxText;
            waterUniformMinText.text = ogUniformMinText;
            waterGaussianMaxText.text = ogGaussianMaxText;
            waterGaussianMinText.text = ogGaussianMinText;
        }

        if(generatorScript.dryAngular)
        {
            dryFixedText.text = "0";
            dryUniformMinText.text = dryGaussianMinText.text = "Min. Angle";
            dryUniformMaxText.text = dryGaussianMaxText.text = "Max. Angle";
        }
        else if (dryFixedText.text != ogFixedText)
        {
            dryFixedText.text = ogFixedText;
            dryUniformMaxText.text = ogUniformMaxText;
            dryUniformMinText.text = ogUniformMinText;
            dryGaussianMaxText.text = ogGaussianMaxText;
            dryGaussianMinText.text = ogGaussianMinText;
        }
    }
}
