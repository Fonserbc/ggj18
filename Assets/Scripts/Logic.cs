using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logic : MonoBehaviour {

    public Constants c;

    public GameState currentState;
    public InputState currentInput;

    GameState newState;

    public GameObject level;

    public BoxCollider2D[] staticWorld;
	
	void Start () {
        staticWorld = level.GetComponentsInChildren<BoxCollider2D>();
	}
	
    public void UpdateState (InputState newInput)
    {
        newState = currentState;

        // PlayerInput
        for (int i = 0; i < c.numPlayers; ++i)
        {
            UpdatePlayerPos(i, newInput.playerInputs[i]);

            UpdatePlayerActions(i, currentInput.playerInputs[i], newInput.playerInputs[i]);
        }

        // Physics Collision
        for (int i = 0; i < c.numPlayers; ++i)
        {
            // Against static world
            for (int j = 0; j < staticWorld.Length; ++j) {
                if (CircleAABBCollides(newState.players[i].position, c.playerCollisionRadius, staticWorld[j].bounds))
                {
                    newState.players[i].position = CircleAABBCorrect(newState.players[i].position, c.playerCollisionRadius, staticWorld[j].bounds);
                }
            }

            // Against antenas

        }

        // World Logic
	}

    void UpdatePlayerPos(int id, InputState.PlayerInput input) {
        if (newState.players[id].stunned > 0) {
            newState.players[id].stunned--;
            return;
        }

        if (!newState.players[id].connected) {
            return;
        }
        Vector2 axis = new Vector2(input.xAxis, input.yAxis);
        if (axis.x == 0.0f && axis.y == 0.0f) {
            return;
        }

        if (axis.magnitude > 1f) {
            axis.Normalize();
        }

        newState.players[id].position.x += axis.x * c.playerSpeed * c.fixedDeltaTime;
        newState.players[id].position.y += axis.y * c.playerSpeed * c.fixedDeltaTime;
    }

    void UpdatePlayerActions(int id, InputState.PlayerInput lastInput, InputState.PlayerInput newInput)
    {
        int nearestAntenna = FindAntenaNearPlayer(newState.players[id].position);

        if (nearestAntenna < 0) return;

        if (!lastInput.up && newInput.up)
        {
            newState.antenas[nearestAntenna].state = GameState.AntenaInfo.AntenaState.ColorUp;
        }
        else if (!lastInput.down && newInput.down)
        {
            newState.antenas[nearestAntenna].state = GameState.AntenaInfo.AntenaState.ColorDown;
        }
        else if (!lastInput.left && newInput.left)
        {
            newState.antenas[nearestAntenna].state = GameState.AntenaInfo.AntenaState.ColorLeft;
        }
        else if (!lastInput.right && newInput.right)
        {
            newState.antenas[nearestAntenna].state = GameState.AntenaInfo.AntenaState.ColorRight;
        }
    }

    int FindAntenaNearPlayer (Vector2 playerPos) {
        for (int i = 0; i < newState.antenas.Length; ++i) {
            if (Vector2.Distance(playerPos, newState.antenas[i].position) < c.antenaActivationRadius) return i;
        }
        return -1;
    }

    bool CircleAABBCollides (Vector2 cPos, float r, Bounds aabb) {

        return false;
    }

    Vector2 CircleAABBCorrect(Vector2 cPos, float r, Bounds aabb)
    {

        return cPos;
    }
}
