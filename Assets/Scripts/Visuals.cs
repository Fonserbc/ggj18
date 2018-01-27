using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour {

    public GameObject BoltPrefab;
    public AntennaScript[] antenas;
    public Transform[] players;
    Animator[] playerAnimators;
	List<DigitalRuby.LightningBolt.LightningBoltScript> bolts = new List<DigitalRuby.LightningBolt.LightningBoltScript>();
    public Transform levelTransform;
    Logic myLogic;
    public Constants c;
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
	
    public void UpdateFrom (GameState state)
    {
        //Player Visuals
        for(int i = 0; i < state.players.Length; ++i)
        {
            if(state.players[i].connected != players[i].gameObject.activeInHierarchy)
            {
                players[i].gameObject.SetActive(state.players[i].connected);
            }

            players[i].position = new Vector3(state.players[i].position.x, players[i].position.y, state.players[i].position.y);
            players[i].rotation = Quaternion.AngleAxis(state.players[i].rotation, Vector3.down);

            
            playerAnimators[i].SetBool("Moving", state.players[i].moving);
            playerAnimators[i].SetBool("Stunned", state.players[i].stunnedTime > 0);
            playerAnimators[i].SetBool("Invincible", state.players[i].invincibleTime > 0);
            
        }
        //End Player Visuals

        //Antenna Visuals
        for (int i = 0; i < antenas.Length; ++i)
        {
            antenas[i].isConnected = myLogic.IsAntenaLinking(i);            
            antenas[i].SetColor(c.antennaColors[(int)state.antenas[i].state]);
        }
        //End Antenna Visuals

        //Connections Visuals
        List<Vector2i> connections = myLogic.GetCurrentAntenasAristas();
        int indexBolt = 0;  
        while(indexBolt < bolts.Count)
        {
            if (indexBolt < connections.Count)
            {
                bolts[indexBolt].StartObject = antenas[connections[indexBolt].x].spawnBolt.gameObject;
                bolts[indexBolt].EndObject = antenas[connections[indexBolt].y].spawnBolt.gameObject;
                bolts[indexBolt].GetComponent<LineRenderer>().material.SetColor("_EmissionColor", c.connectionColors[((int)state.antenas[connections[indexBolt].x].state)-1]);
              
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

    private void OnDrawGizmosSelected()
    {
        foreach (AntennaScript an in antenas) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(an.transform.position, c.antenaCollisionRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(an.transform.position, c.antenaActivationRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(an.transform.position, c.antenaLinkMaxRadius);
        }

        foreach(Transform pl in players)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pl.position, c.playerCollisionRadius);

        }
    }
}
