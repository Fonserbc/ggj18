using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour {

    public GameObject BoltPrefab;
    public GameObject antenasParent;
    public GameObject MessagePrefab;
    [System.NonSerialized]
    public AntennaScript[] antenas;
    public AntennaScript[] recieverAntenas;
    public AntennaScript[] baseAntenas;
    public PlayerScript[] players;
    MessageScript[] messages;
    Animator[] playerAnimators;
	List<DigitalRuby.LightningBolt.LightningBoltScript> bolts = new List<DigitalRuby.LightningBolt.LightningBoltScript>();
    public Transform levelTransform;
    Logic myLogic;
    public Constants c;
    //Lista de pares de antenas

    public int ownPlayer = -1;

	public void Init (Logic l)
    {
        myLogic = l;
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
            players[i].transform.position = newPos;
            players[i].transform.rotation = Quaternion.AngleAxis(state.players[i].rotation, Vector3.down);

            SetPlayerWave(i, (i==0) ? frame.input_player1 : frame.input_player2);
            
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

        //Message Visuals
        for(int i = 0; i < messages.Length; ++i)
        {
            int holderId = (int)state.messages[i].color;
            int currentAnt = state.messages[i].currentAntena;

            if (state.messages[i].onScene && messages[i] == null)
            {
                messages[i] = Instantiate(MessagePrefab, antenas[currentAnt].messageHolders[holderId-1].position, antenas[currentAnt].messageHolders[holderId - 1].rotation).GetComponent<MessageScript>();
                messages[i].messageRend.material.color = c.connectionColors[holderId - 1];
            } else if (!state.messages[i].onScene && messages[i] != null)
            {
                Destroy(messages[i].gameObject);
            }

            if(messages[i] != null)
            {
                messages[i].transform.position = Vector3.Lerp(messages[i].transform.position, antenas[currentAnt].messageHolders[holderId - 1].position, .1f);
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

    void SetPlayerWave(int id, PlayerInput input)
    {

            if (input.up)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.SetColor("_EmissionColor", c.connectionColors[0]);
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else if (input.down)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.SetColor("_EmissionColor", c.connectionColors[1]);
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else if (input.left)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.SetColor("_EmissionColor", c.connectionColors[2]);
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else if (input.right)
            {
                players[id].waving = true;
                players[id].waveRenderer.material.SetColor("_EmissionColor", c.connectionColors[3]);
                players[id].waveRenderer.gameObject.SetActive(true);
            }
            else
            {
                players[id].waving = false;
                players[id].waveRenderer.gameObject.SetActive(false);
            }
    }

    private void OnDrawGizmosSelected()
    {
        foreach(PlayerScript pl in players)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pl.transform.position, c.playerCollisionRadius);

        }
    }
}
