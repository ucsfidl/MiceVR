using UnityEngine;
using System.Collections;

public class PhysicsDisabler : MonoBehaviour {

    private CapsuleCollider col;

	// Use this for initialization
	void Start () {
        this.col = GetComponent<CapsuleCollider>();

        if( !this.GetComponent<Renderer>().isVisible )
            this.col.enabled = false;
	}

    void OnBecameInvisible()
    {
        this.col.enabled = false;
    }

    void OnBecameVisible()
    {
        this.col.enabled = true;
    }
}
