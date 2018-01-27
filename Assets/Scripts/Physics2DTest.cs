using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physics2DTest : MonoBehaviour {

    public Collider2D col1, col2;
    public ContactFilter2D contactFilter;
    public Transform aux;

	// Use this for initialization
	void Start () {
        GameState state = new GameState();
        state.players = new GameState.PlayerInfo[2];
        state.players[0].stunnedTime = 1f;

        GameState copyState = new GameState(state);


        copyState.players[0].stunnedTime = 0;

        if (copyState.players[0].stunnedTime == state.players[0].stunnedTime) {
            Debug.Log("L'array no es copia");;
        }
	}
	
	// Update is called once per frame
	void Update () {
        aux.position = col1.bounds.ClosestPoint(col2.transform.position);

        //Debug.Log("Contains? "+col1.bounds.Contains(col2.transform.position));
	}
}
