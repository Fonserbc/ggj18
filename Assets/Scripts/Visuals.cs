﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour {

    public GameObject BoltPrefab;
    public AntennaScript[] antenas;
    public Transform[] players;
    Animator[] playerAnimators;
    List<DigitalRuby.LightningBolt.LightningBoltScript> bolts;
    public Transform levelTransform;
    Logic myLogic;
    //Lista de pares de antenas

	public void Init (Logic l)
    {
        myLogic = l;
        playerAnimators = new Animator[players.Length];
        for(int i = 0; i < playerAnimators.Length; ++i)
        {
            playerAnimators[i] = players[i].GetComponent<Animator>();
        }
	}
	
    void UpdateFrom (GameState state)
    {
        //Player Visuals
        for(int i = 0; i < state.players.Length; ++i)
        {
            if(state.players[i].connected != players[i].gameObject.activeInHierarchy)
            {
                players[i].gameObject.SetActive(state.players[i].connected);
            }

            players[i].position = new Vector3(state.players[i].position.x, players[i].position.y, state.players[i].position.y);
            players[i].rotation = Quaternion.Euler(0, state.players[i].rotation, 0);
            playerAnimators[i].SetBool("Stunned", state.players[i].stunned != 0);


        }
        //End Player Visuals

        //Connections Visuals
        List<Vector2i> connections = myLogic.GetCurrentAntenasAristas();
        int indexBolt = 0;        
        foreach (DigitalRuby.LightningBolt.LightningBoltScript bolt in bolts)
        {
            if(indexBolt < connections.Count)
            {
                bolt.StartObject = antenas[connections[indexBolt].x].spawnBolt.gameObject;
                bolt.EndObject = antenas[connections[indexBolt].y].spawnBolt.gameObject;
                ++indexBolt;
            } else
            {
                Destroy(bolt.gameObject);
                bolts.RemoveAt(indexBolt);
            }
        }
        
        for(int i = indexBolt; i < connections.Count; ++i)
        {
            DigitalRuby.LightningBolt.LightningBoltScript newBolt = (Instantiate(BoltPrefab, transform.position, transform.rotation, transform)).GetComponent<DigitalRuby.LightningBolt.LightningBoltScript>();
            newBolt.StartObject = antenas[connections[i].x].spawnBolt.gameObject;
            newBolt.EndObject = antenas[connections[i].y].spawnBolt.gameObject;
            bolts.Add(newBolt);
        }
        //End Connections

    }
}
