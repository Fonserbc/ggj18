using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntennaRotation : MonoBehaviour {

    public Transform antenna;
    public float rotTime = .2f;
    public float minAngle = 0;
    public float maxAngle = 180;

    float counter = 0;

	// Update is called once per frame
	void Update () {
        counter += Time.deltaTime;
        if(counter >= rotTime)
        {
            counter -= rotTime;
            antenna.Rotate(Vector3.forward * Random.Range(minAngle, maxAngle));
        }
	}
}
