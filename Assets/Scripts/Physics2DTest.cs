using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physics2DTest : MonoBehaviour {

    public Collider2D col1, col2;
    public ContactFilter2D contactFilter;
    public Transform aux;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        aux.position = col1.bounds.ClosestPoint(col2.transform.position);

        //Debug.Log("Contains? "+col1.bounds.Contains(col2.transform.position));
	}
}
