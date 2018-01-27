using System.Collections.Generic;
using UnityEngine;

public class Logic
{
    public Constants c;

    GameState newState;

    Bounds[] staticWorld;

    List<int>[] antenaConnections;
    List<Vector2i> antenaLinks;

    public GameState InitFirstState (Visuals v)
    {
        int antenaCount = v.antenas.Length;
        Collider[] staticColliders = v.levelTransform.gameObject.GetComponentsInChildren<Collider>();
        staticWorld = new Bounds[staticColliders.Length];

        for (int i = 0; i < staticColliders.Length; ++i) {
            Bounds b = staticColliders[i].bounds;

            staticWorld[i] = new Bounds(new Vector2(b.center.x, b.center.z), new Vector2(b.size.x, b.size.z));
        }

        antenaConnections = new List<int>[antenaCount];
        antenaLinks = new List<Vector2i>(antenaCount * (antenaCount - 1));
        for (int i = 0; i < antenaConnections.Length; ++i)
        {
            antenaConnections[i] = new List<int>(antenaCount - 1);
        }


        newState = new GameState();
        newState.players = new GameState.PlayerInfo[c.numPlayers];
        for (int i = 0; i < newState.players.Length; ++i) {
            newState.players[i] = new GameState.PlayerInfo();
            newState.players[i].position = new Vector2(v.players[i].transform.position.x, v.players[i].transform.position.z);
            newState.players[i].rotation = v.players[i].transform.eulerAngles.y;
            newState.players[i].connected = true;
        }
        newState.antenas = new GameState.AntenaInfo[antenaCount];
        for (int i = 0; i < antenaCount; ++i)
        {
            newState.antenas[i] = new GameState.AntenaInfo();
            newState.antenas[i].position = new Vector2(v.antenas[i].transform.position.x, v.antenas[i].transform.position.z);
            newState.antenas[i].rotation = v.antenas[i].transform.rotation.eulerAngles.y;
        }

        v.Init(this);

        return newState;
    }

    public void UpdateState(GameFrame previousFrame, ref GameFrame frame)
    {
        newState = frame.state;
        GameState previousState = previousFrame.state;

        // PlayerInput
        UpdatePlayerPos(0, frame.input_player1);
        UpdatePlayerActions(0, previousFrame.input_player1, frame.input_player1);
        UpdatePlayerPos(1, frame.input_player2);
        UpdatePlayerActions(1, previousFrame.input_player1, frame.input_player2);

        //for (int i = 0; i < c.numPlayers; ++i)
        //{
        //    UpdatePlayerPos(i, newInput.playerInputs[i]);

        //    UpdatePlayerActions(i, previousInput.playerInputs[i], newInput.playerInputs[i]);
        //}

        UpdateAntennaConnections();

        // Physics Collisions
        for (int i = 0; i < c.numPlayers; ++i)
        {
            // Against static world
            for (int j = 0; j < staticWorld.Length; ++j)
            {
                if (CircleAABBCollides(newState.players[i].position, c.playerCollisionRadius, staticWorld[j]))
                {
                    newState.players[i].position = CircleAABBCorrect(previousState.players[i].position, newState.players[i].position, c.playerCollisionRadius, staticWorld[j]);
                }
            }

            // Against antenas
            for (int j = 0; j < newState.antenas.Length; ++j)
            {
                if (CircleCircleCollides(newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, c.antenaCollisionRadius))
                {
                    newState.players[i].position = CircleCircleCorrect(previousState.players[i].position, newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, c.antenaCollisionRadius);

                    if (IsAntenaLinking(j) && IsPlayerVulnerable(i))
                    {
                        StunPlayer(i);
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

        // Physics Triggers
        for (int i = 0; i < c.numPlayers; ++i)
        {
            if (!IsPlayerVulnerable(i)) continue;
            for (int j = 0; j < antenaLinks.Count; ++j) {
                Vector2i link = antenaLinks[j];

                if (CircleLineCollides(newState.antenas[link.x].position, newState.antenas[link.y].position,
                                       newState.players[i].position, c.playerCollisionRadius))
                {
                    StunPlayer(i);
                    break;
                }
            }
        }
    }

    void UpdatePlayerPos(int id, PlayerInput input)
    {
        if (newState.players[id].stunnedTime > 0)
        {
            newState.players[id].stunnedTime -= c.fixedDeltaTime;
            return;
        }
        else if (newState.players[id].invincibleTime > 0)
        {
            newState.players[id].invincibleTime -= c.fixedDeltaTime;
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

    bool IsPlayerVulnerable(int p) {
        return newState.players[p].stunnedTime <= 0 && newState.players[p].invincibleTime <= 0;
    }

    void StunPlayer(int p) {
        newState.players[p].stunnedTime = c.stunnedtime;
        newState.players[p].invincibleTime = c.invincibilityTime;
    }

    void UpdateAntennaConnections()
    {
        for (int i = 0; i < antenaConnections.Length; ++i) {
            antenaConnections[i].Clear();
        }
        antenaLinks.Clear();

        for (int i = 0; i < newState.antenas.Length; ++i) {
            for (int j = i + 1; j < newState.antenas.Length; ++j)
            {
                if (Vector2.Distance(newState.antenas[i].position, newState.antenas[j].position) <= c.antenaLinkMaxRadius
                    && newState.antenas[i].state == newState.antenas[j].state && newState.antenas[i].state != GameState.AntenaInfo.AntenaState.Off) {
                    antenaConnections[i].Add(j);
                    antenaConnections[j].Add(i);
                    antenaLinks.Add(new Vector2i(i, j));
                }
            }
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

    bool CircleLineCollides (Vector2 lPos1, Vector2 lPos2, Vector2 p, float r)
    {
        Vector2 lineDir = lPos2 - lPos1;
        float lineMagnitude = lineDir.magnitude;
        lineDir.Normalize();

        Vector2 v = p - lPos1;
        float d = Vector2.Dot(v, lineDir);

        Vector2 projected = lPos1 + lineDir * d;

        if (Vector2.Distance(projected, p) > r)
            return false;

        float d1 = Vector2.Distance(lPos1, projected);
        float d2 = Vector2.Distance(lPos2, projected);

        return d1 + d2 <= lineMagnitude+2*r;
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
        return antenaConnections[antenaId].Count > 0;
    }

    public List<Vector2i> GetCurrentAntenasAristas() {
        return antenaLinks;
    }

    public List<int>[] GetCurrentAntenaConnections() {
        return antenaConnections;
    }
}
