using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour {


    public Transform[] antenas;
    public Transform[] players;
    Animator[] playerAnimators;
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

        //Connections
        List<Vector2i> connections = myLogic.GetCurrentAntenasAristas();
        for (int i = 0; i < connections.Count; ++i)
        {

        }

        //mover todo al siti
        //desactivar y activar cosas
        //animaciones

        //Electricity
        //ANimations

    }
}
