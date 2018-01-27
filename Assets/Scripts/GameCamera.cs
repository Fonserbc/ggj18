using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour {

    public Visuals visuals;
	
	// Update is called once per frame
	void Update () {
        if (visuals.ownPlayer != -1) {
            Transform player = visuals.players[visuals.ownPlayer].transform;

            transform.position = player.position;
        }
	}
}
