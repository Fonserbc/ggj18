using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour {

    public Renderer waveRenderer;
    public bool waving;
	// Update is called once per frame
	void Update () {
        if (!waving) return;
        waveRenderer.transform.Rotate(Vector3.up*5);
	}
}
