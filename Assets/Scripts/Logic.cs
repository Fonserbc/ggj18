using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logic : MonoBehaviour
{

    public Constants c;

    GameState newState;

    BoxCollider2D[] staticWorld;

    List<int>[] antenaConnections;

    void Init(Visuals v)
    {
        staticWorld = v.levelTransform.gameObject.GetComponentsInChildren<BoxCollider2D>();


    }

    public GameState UpdateState(GameState previousState, InputState previousInput, InputState newInput)
    {
        newState = previousState; // Copy (?)

        // PlayerInput
        for (int i = 0; i < c.numPlayers; ++i)
        {
            UpdatePlayerPos(i, newInput.playerInputs[i]);

            UpdatePlayerActions(i, previousInput.playerInputs[i], newInput.playerInputs[i]);
        }

        // Physics Collision
        for (int i = 0; i < c.numPlayers; ++i)
        {
            // Against static world
            for (int j = 0; j < staticWorld.Length; ++j)
            {
                if (CircleAABBCollides(newState.players[i].position, c.playerCollisionRadius, staticWorld[j].bounds))
                {
                    newState.players[i].position = CircleAABBCorrect(previousState.players[i].position, newState.players[i].position, c.playerCollisionRadius, staticWorld[j].bounds);
                }
            }

            // Against antenas
            for (int j = 0; j < newState.antenas.Length; ++j)
            {
                if (CircleCircleCollides(newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, c.antenaCollisionRadius))
                {
                    newState.players[i].position = CircleCircleCorrect(previousState.players[i].position, newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, c.antenaCollisionRadius);

                    if (IsAntenaLinking(j) && newState.players[i].stunned <= 0)
                    {
                        newState.players[i].stunned = c.stunnedFrames;
                    }
                }
            }

            // Against other players
            for (int j = 0; j < newState.players.Length; ++j)
            {
                if (i != j && CircleCircleCollides(newState.players[i].position, c.playerCollisionRadius, newState.players[j].position, c.playerCollisionRadius))
                {
                    newState.players[i].position = CircleCircleCorrect(previousState.players[i].position, newState.players[i].position, c.playerCollisionRadius, newState.players[j].position, c.playerCollisionRadius);
                }
            }
        }

        // World Logic
        // Electricity/antenagrabbing/whatever

        return newState;
    }

    void UpdatePlayerPos(int id, PlayerInput input)
    {
        if (newState.players[id].stunned > 0)
        {
            newState.players[id].stunned--;
            return;
        }

        if (!newState.players[id].connected)
        {
            return;
        }
        Vector2 axis = new Vector2(input.xAxis, input.yAxis);
        if (axis.x == 0.0f && axis.y == 0.0f)
        {
            return;
        }

        if (axis.magnitude > 1f)
        {
            axis.Normalize();
        }

        newState.players[id].position.x += axis.x * c.playerSpeed * c.fixedDeltaTime;
        newState.players[id].position.y += axis.y * c.playerSpeed * c.fixedDeltaTime;

        newState.players[id].rotation = Vector2.SignedAngle(Vector2.right, axis);
    }

    void UpdatePlayerActions(int id, PlayerInput lastInput, PlayerInput newInput)
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

    int FindAntenaNearPlayer(Vector2 playerPos)
    {
        for (int i = 0; i < newState.antenas.Length; ++i)
        {
            if (Vector2.Distance(playerPos, newState.antenas[i].position) < c.antenaActivationRadius) return i;
        }
        return -1;
    }

    bool CircleAABBCollides(Vector2 cPos, float r, Bounds aabb)
    {
        if (aabb.Contains(cPos)) return true;

        Vector2 closest = aabb.ClosestPoint(cPos);
        return Vector2.Distance(closest, cPos) < r;
    }

    Vector2 CircleAABBCorrect(Vector2 cBefore, Vector2 cAfter, float r, Bounds aabb)
    {
        // TODO check it works
        Vector2 closest = aabb.ClosestPoint(cAfter);
        Vector2 normal = cAfter - closest;
        normal.Normalize();

        if (aabb.Contains(cAfter)) {
            normal *= -1f;
        }

        return closest + normal * r;
    }

    bool CircleCircleCollides(Vector2 c1, float r1, Vector2 c2, float r2)
    {
        return Vector2.Distance(c1, c2) < r1 + r2;
    }

    Vector2 CircleCircleCorrect(Vector2 c1Before, Vector2 c1After, float r1, Vector2 c2, float r2)
    {
        // TODO check it works
        Vector2 normal = c1After - c2;
        normal.Normalize();

        return c2 + normal * (r1 + r2);
    }

    bool IsAntenaLinking(int antenaId)
    {
        return false;
    }

    public List<Vector2i> GetCurrentAntenasConnections() {
        return null;
    }
}
