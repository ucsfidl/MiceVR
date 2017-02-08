using UnityEngine;
using System.Collections;

public class DebugControl : MonoBehaviour {

    private CharacterController control;

	// Use this for initialization
	void Start () {
        this.control = GameObject.Find("FPSController").GetComponent<CharacterController>();
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.A))
            //this.transform.Translate(Vector3.left * 4f);
            this.control.Move(Vector3.left*2f);

        if (Input.GetKey(KeyCode.W))
            //this.transform.Translate(Vector3.forward * 4f);
            this.control.Move(Vector3.forward * 2f);

        if (Input.GetKey(KeyCode.S))
            //this.transform.Translate(Vector3.back * 4f);
            this.control.Move(Vector3.back * 2f);

        if (Input.GetKey(KeyCode.D))
            //this.transform.Translate(Vector3.right * 4f);
            this.control.Move(Vector3.right * 2f);

        if (Input.GetKey(KeyCode.Q))
            this.transform.Rotate(Vector3.up, -2f);

        if (Input.GetKey(KeyCode.E))
            this.transform.Rotate(Vector3.up, 2f);
	}
}
