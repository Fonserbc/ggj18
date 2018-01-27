﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntennaRotation : MonoBehaviour {

    public Transform antenna;
    public float rotTime = .2f;
    public float minAngle = 0;
    public float maxAngle = 180;

    public Transform spawnBolt;
    public float minPosition = 0.75f;
    public float maxPosition = 4.25f;


    float counter = 0;

	// Update is called once per frame
	void Update () {
        counter += Time.deltaTime;
        if(counter >= rotTime)
        {
            counter -= rotTime;
            antenna.Rotate(Vector3.forward * Random.Range(minAngle, maxAngle));
            Vector3 pos = spawnBolt.localPosition;
            pos.y = Random.Range(minPosition, maxPosition);
            spawnBolt.localPosition = pos;
        }
	}
}
