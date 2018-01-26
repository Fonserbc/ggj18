using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physics2DTest : MonoBehaviour {

    public Collider2D col1, col2;
    public ContactFilter2D contactFilter;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Collider2D[] res = new Collider2D[8];
        Debug.Log("Touching? "+col1.OverlapCollider(contactFilter, res));
	}
}
