﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour {

    public GameObject BoltPrefab;
    public GameObject antenasParent;
    public GameObject MessagePrefab;
    public GameObject AudioPrefab;
    [System.NonSerialized]
    public AntennaScript[] antenas;
    public AntennaScript[] recieverAntenas;
    public AntennaScript[] baseAntenas;
    PlayerScript[] players;
    public Transform[] spawnPlaces;
    public PlayerScript playerPrefab;

    MessageScript[] messages;

    Animator[] playerAnimators;
	List<DigitalRuby.LightningBolt.LightningBoltScript> bolts = new List<DigitalRuby.LightningBolt.LightningBoltScript>();
    public Transform levelTransform;
    Logic myLogic;
    public Constants c;
    //Lista de pares de antenas

    public int ownPlayer = -1;

	public void Init (int numPlayers, Logic l)
    {
        myLogic = l;

        if (players == null || numPlayers != players.Length)
        {
            if (players != null) {
                for (int i = 0; i < numPlayers; ++i)
                {
                    Destroy(players[i].gameObject);
                }
            }

            players = new PlayerScript[numPlayers];
            for (int i = 0; i < numPlayers; ++i)
            {
                players[i] = Instantiate(playerPrefab).GetComponent<PlayerScript>();
            }
        }
        for (int i = 0; i < numPlayers; ++i)
        {
            players[i].transform.position = spawnPlaces[i].position;
            players[i].transform.rotation = spawnPlaces[i].rotation;
        }

        playerAnimators = new Animator[players.Length];
        for(int i = 0; i < playerAnimators.Length; ++i)
        {
            playerAnimators[i] = players[i].GetComponent<Animator>();
        }

        messages = new MessageScript[c.numMessages];
        for (int i = 0; i < messages.Length; ++i) messages[i] = null;
    }
	
    public void UpdateFrom (GameFrame frame)
    {
        GameState state = frame.state;
        //Player Visuals
        for(int i = 0; i < state.players.Length; ++i)
        {
            if(state.players[i].connected != players[i].gameObject.activeInHierarchy)
            {
                players[i].gameObject.SetActive(state.players[i].connected);
            }

            Vector3 newPos = new Vector3(state.players[i].position.x, players[i].transform.position.y, state.players[i].position.y);
            players[i].moving = players[i].transform.position != newPos;
            float smoothFactor = 0.2f;
            players[i].transform.position = Vector3.Lerp(players[i].transform.position, newPos, smoothFactor);
            players[i].transform.rotation = Quaternion.Lerp(players[i].transform.rotation, Quaternion.AngleAxis(state.players[i].rotation, Vector3.down), smoothFactor);

            SetPlayerWave(i, (i==0) ? frame.input_player0 : frame.input_player1);
            
            playerAnimators[i].SetBool("Moving", state.players[i].moving);
            bool stunned = playerAnimators[i].GetBool("Stunned");
            if (!stunned && state.players[i].stunnedTime > 0) players[i].playStunAudio();
            playerAnimators[i].SetBool("Stunned", state.players[i].stunnedTime > 0);
            playerAnimators[i].SetBool("Invincible", state.players[i].invincibleTime > 0);
            
        }
        //End Player Visuals

        //Antenna Visuals
        for (int i = 0; i < antenas.Length; ++i)
        {
            antenas[i].isConnected = myLogic.IsAntenaLinking(i);            
            antenas[i].SetColor(c.antennaColors[(int)state.antenas[i].lastState]);
            antenas[i].SetCoolDown(state.antenas[i].refreshTime > 0, c.antennaColors[(int)state.antenas[i].state], c.connectionColors[(int)state.antenas[i].state]);
            if (state.antenas[i].refreshTime == c.antenaRefreshTime) Instantiate(AudioPrefab, antenas[i].transform.position, antenas[i].transform.rotation);
        }
        //End Antenna Visuals

        //Message Visuals
        for(int i = 0; i < messages.Length; ++i)
        {
            int holderId = (int)state.messages[i].color;
            int currentAnt = state.messages[i].currentAntena;

            if (state.messages[i].state == GameState.MessageInfo.MessageState.Playing && messages[i] == null)
            {
                messages[i] = Instantiate(MessagePrefab, antenas[currentAnt].messageHolders[holderId-1].position, antenas[currentAnt].messageHolders[holderId - 1].rotation).GetComponent<MessageScript>();
                messages[i].currentAntena = currentAnt;
                messages[i].messageRend.material.color = c.connectionColors[holderId];
            } else if (state.messages[i].state == GameState.MessageInfo.MessageState.Out && messages[i] != null)
            {
                Destroy(messages[i].gameObject);
            }


            if (messages[i] != null)
            {
                if (messages[i].currentAntena != currentAnt) messages[i].playTravelAudio();
                messages[i].currentAntena = currentAnt;
                int nextAnt = state.messages[i].nextAntena;

                if(state.messages[i].state == GameState.MessageInfo.MessageState.End) messages[i].playSendAudio();

                Vector3 wantedPos = antenas[currentAnt].messageHolders[holderId - 1].position;
                if (nextAnt >= 0) {
                    wantedPos = Vector3.Lerp(wantedPos, antenas[nextAnt].messageHolders[holderId - 1].position, 1f - Mathf.Clamp01(state.messages[i].transmissionTime));
                }
                messages[i].transform.position = Vector3.Lerp(messages[i].transform.position, wantedPos, .1f);
                messages[i].transform.rotation = Quaternion.Lerp(messages[i].transform.rotation, antenas[currentAnt].messageHolders[holderId - 1].rotation, .1f);
            }


        }
        //End Message Visuals

        //Connections Visuals
        List<Vector2i> connections = myLogic.GetCurrentAntenasAristas();
        int indexBolt = 0;  
        while(indexBolt < bolts.Count)
        {
            if (indexBolt < connections.Count)
            {
                bolts[indexBolt].StartObject = antenas[connections[indexBolt].x].spawnBolt.gameObject;
                bolts[indexBolt].EndObject = antenas[connections[indexBolt].y].spawnBolt.gameObject;
                bolts[indexBolt].GetComponent<LineRenderer>().material.SetColor("_EmissionColor", c.connectionColors[((int)state.antenas[connections[indexBolt].x].lastState)]);
              
                ++indexBolt;
            }
            else
            {
                Destroy(bolts[indexBolt].gameObject);
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

    void SetPlayerWave(int id, PlayerInput input)
    {

            if (input.up)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.color = c.connectionColors[1];
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else if (input.down)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.color = c.connectionColors[2];
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else if (input.left)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.color = c.connectionColors[3];
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else if (input.right)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.color = c.connectionColors[4];
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else
            {
                players[id].waving = false;
                players[id].waveRenderer.gameObject.SetActive(false);
            }
    }

    public PlayerScript GetPlayer(int id) {
        return players[id];
    }
}
