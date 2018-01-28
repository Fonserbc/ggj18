﻿using System.Collections.Generic;
using UnityEngine;

public class Logic
{
    public Constants c;

    GameState newState;

    Bounds[] staticWorld;

    List<int>[] antenaConnections;
    List<Vector2i> antenaLinks;
    int[] receiverAntenaId;
    int[] baseAntenaId;
    RaycastHit[] raycastsHits;

    Visuals v;

    System.Random rand;

    Vector2[] startPos;

    public GameState InitFirstState (Visuals visuals, uint seed = 37)
    {
        raycastsHits = new RaycastHit[1];
        v = visuals;

        v.antenas = v.antenasParent.GetComponentsInChildren<AntennaScript>();

        int antenaCount = v.antenas.Length;
        Collider[] staticColliders = v.levelTransform.gameObject.GetComponentsInChildren<Collider>();
        staticWorld = new Bounds[staticColliders.Length];

        for (int i = 0; i < staticColliders.Length; ++i) {
            Bounds b = staticColliders[i].bounds;

            staticWorld[i] = new Bounds(new Vector2(b.center.x, b.center.z), new Vector2(b.size.x, b.size.z));
        }

        antenaConnections = new List<int>[antenaCount];
        antenaLinks = new List<Vector2i>(antenaCount * (antenaCount - 1));
        receiverAntenaId = new int[v.recieverAntenas.Length];
        Utilities.InitializeArray(ref receiverAntenaId, -1);
        baseAntenaId = new int[c.numPlayers];
        Utilities.InitializeArray(ref baseAntenaId, -1);

        for (int i = 0; i < antenaConnections.Length; ++i)
        {
            antenaConnections[i] = new List<int>(antenaCount - 1);
            for (int j = 0; j < receiverAntenaId.Length; ++j) {
                if (receiverAntenaId[j] < 0 && v.antenas[i] == v.recieverAntenas[j]) {
                    receiverAntenaId[j] = i;
                    break;
                }
            }
            for (int j = 0; j < baseAntenaId.Length; ++j)
            {
                if (baseAntenaId[j] < 0 && v.antenas[i] == v.baseAntenas[j])
                {
                    baseAntenaId[j] = i;
                    break;
                }
            }
        }


        newState = new GameState();
        newState.seed = (int)seed;
        rand = new System.Random(newState.seed);
        newState.players = new GameState.PlayerInfo[c.numPlayers];
        bool firstInit = false;
        if (startPos == null) {
            startPos = new Vector2[c.numPlayers];
            firstInit = true;
        }
        for (int i = 0; i < newState.players.Length; ++i) {
            newState.players[i] = new GameState.PlayerInfo();
            if (firstInit) {
                startPos[i] = new Vector2(v.players[i].transform.position.x, v.players[i].transform.position.z);
            }
            newState.players[i].position = startPos[i];
            newState.players[i].rotation = v.players[i].transform.eulerAngles.y;
            newState.players[i].connected = true;
            newState.players[i].moving = false;
            newState.players[i].points = 0;
        }
        newState.antenas = new GameState.AntenaInfo[antenaCount];
        for (int i = 0; i < antenaCount; ++i)
        {
            newState.antenas[i] = new GameState.AntenaInfo();
            newState.antenas[i].state = GameState.ColorState.Off;
            newState.antenas[i].lastState = GameState.ColorState.Off;
            newState.antenas[i].position = new Vector2(v.antenas[i].transform.position.x, v.antenas[i].transform.position.z);
            newState.antenas[i].rotation = v.antenas[i].transform.rotation.eulerAngles.y;
            newState.antenas[i].refreshTime = 0;
        }
        newState.messages = new GameState.MessageInfo[c.numMessages];
        for (int i = 0; i < newState.messages.Length; ++i)
        {
            newState.messages[i] = new GameState.MessageInfo();
            newState.messages[i].state = GameState.MessageInfo.MessageState.Out;
            newState.messages[i].transmissionTime = (i + 1) * c.timeBetweenMessages;
            newState.messages[i].color = (GameState.ColorState)rand.Next(1, 5);
            newState.messages[i].currentAntena = -1;
            newState.messages[i].lastAntena = -1;
            newState.messages[i].nextAntena = -1;
        }

        newState.winnerPlayer = -1;
        newState.winTime = 0;

        v.Init(this);

        return newState;
    }

    public void UpdateState(ref GameFrame frame)
    {
        newState = frame.state;
        newState.seed = (int) (1103515245 * (uint)newState.seed + 12345);
        rand = new System.Random(newState.seed);

        UpdateAntennaTimings();

        if (newState.winnerPlayer != -1)
        { // Winner
            newState.winTime -= c.fixedDeltaTime;

            if (newState.winTime <= 0)
            {
                newState = InitFirstState(v, (uint)newState.seed);
                frame.state.CopyFrom(newState);
                return;
            }
        }
        else
        {
            // PlayerInput
            newState.players[0].moving = UpdatePlayerPos(0, frame.input_player1);
            UpdatePlayerActions(0, frame.input_player1);
            newState.players[1].moving = UpdatePlayerPos(1, frame.input_player2);
            UpdatePlayerActions(1, frame.input_player2);

            //for (int i = 0; i < c.numPlayers; ++i)
            //{
            //    UpdatePlayerPos(i, newInput.playerInputs[i]);

            //    UpdatePlayerActions(i, previousInput.playerInputs[i], newInput.playerInputs[i]);
            //}
        }

        UpdateAntennaConnections();

        UpdateMessageStates();

        // Physics Collisions
        bool[] playerWasCorrected = new bool[c.numPlayers];
        Utilities.InitializeArray(ref playerWasCorrected, false);

        for (int i = 0; i < c.numPlayers; ++i)
        {
            // Against static world
            for (int j = 0; j < staticWorld.Length; ++j)
            {
                if (CircleAABBCollides(newState.players[i].position, c.playerCollisionRadius, staticWorld[j]))
                {
                    newState.players[i].position = CircleAABBCorrect(newState.players[i].position, c.playerCollisionRadius, staticWorld[j]);
                    playerWasCorrected[i] = true;
                }
            }

            // Against antenas
            for (int j = 0; j < newState.antenas.Length; ++j)
            {
                if (CircleCircleCollides(newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, v.antenas[j].collisionRadius))
                {
                    newState.players[i].position = CircleCircleCorrect(newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, v.antenas[j].collisionRadius);

                    /*
                    if (IsAntenaLinking(j) && IsPlayerVulnerable(i))
                    {
                        StunPlayer(i);
                    }
                    */

                    playerWasCorrected[i] = true;
                }
            }

            // Against other players
            for (int j = 0; j < newState.players.Length; ++j)
            {
                if (i != j && CircleCircleCollides(newState.players[i].position, c.playerCollisionRadius, newState.players[j].position, c.playerCollisionRadius))
                {
                    if (newState.players[i].moving && !playerWasCorrected[i]) {
                        newState.players[i].position = CircleCircleCorrect(newState.players[i].position, c.playerCollisionRadius, newState.players[j].position, c.playerCollisionRadius);
                        playerWasCorrected[i] = true;
                    }
                    if (newState.players[j].moving && !playerWasCorrected[i]) {
                        newState.players[j].position = CircleCircleCorrect(newState.players[j].position, c.playerCollisionRadius, newState.players[i].position, c.playerCollisionRadius);
                        playerWasCorrected[j] = true;
                    }
                }
            }
        }

        // World Logic
        // Message Updates
        for (int i = 0; i < newState.messages.Length; ++i)
        {
            GameState.MessageInfo currentMessage = newState.messages[i];
            if (currentMessage.state != GameState.MessageInfo.MessageState.Playing)
                continue;

            // Remove last antena if broken
            if (currentMessage.lastAntena != -1 && newState.antenas[currentMessage.lastAntena].lastState != currentMessage.color)
            {
                currentMessage.lastAntena = -1;
            }

            // Reset timer if own antena changed color
            if (newState.antenas[currentMessage.currentAntena].lastState != currentMessage.color)
            {
                currentMessage.transmissionTime = c.messageTransmissionTime;
                currentMessage.nextAntena = -1;
                currentMessage.lastAntena = -1;
            }
            else if (currentMessage.nextAntena == -1 || newState.antenas[currentMessage.nextAntena].lastState != currentMessage.color)
            {
                currentMessage.nextAntena = -1;
                currentMessage.transmissionTime = c.messageTransmissionTime;

                // Find possible next antena
                List<int> possibleNextAntenas = new List<int>(antenaConnections[currentMessage.currentAntena].Count);
                for (int j = 0; j < antenaConnections[currentMessage.currentAntena].Count; ++j) {
                    if (currentMessage.lastAntena != antenaConnections[currentMessage.currentAntena][j]
                        && currentMessage.color == newState.antenas[antenaConnections[currentMessage.currentAntena][j]].state) {
                        possibleNextAntenas.Add(antenaConnections[currentMessage.currentAntena][j]);
                    }
                }
                if (possibleNextAntenas.Count > 0)
                {
                    currentMessage.nextAntena = possibleNextAntenas[rand.Next(0, possibleNextAntenas.Count)];
                }
            }

            if (currentMessage.nextAntena != -1 && newState.antenas[currentMessage.currentAntena].lastState == currentMessage.color && newState.antenas[currentMessage.nextAntena].lastState == currentMessage.color)
            {
                currentMessage.transmissionTime -= c.fixedDeltaTime;

                if (currentMessage.transmissionTime <= 0)
                {
                    currentMessage.lastAntena = currentMessage.currentAntena;
                    currentMessage.currentAntena = currentMessage.nextAntena;
                    currentMessage.nextAntena = -1;
                    currentMessage.transmissionTime = c.messageTransmissionTime;

                    for (int j = 0; j < baseAntenaId.Length; ++j)
                    {
                        if (currentMessage.currentAntena == baseAntenaId[j])
                        {
                            newState.players[j].points++;
                            currentMessage.state = GameState.MessageInfo.MessageState.End;
                            currentMessage.transmissionTime = 1f;
                            break;
                        }
                    }
                }
            }

            newState.messages[i] = currentMessage;
        }

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

        frame.state.winnerPlayer = newState.winnerPlayer;
        frame.state.seed = newState.seed;
        frame.state.winTime = newState.winTime;
    }

    bool UpdatePlayerPos(int id, PlayerInput input)
    {
        if (newState.players[id].stunnedTime > 0)
        {
            newState.players[id].stunnedTime -= c.fixedDeltaTime;
            return false;
        }
        else if (newState.players[id].invincibleTime > 0)
        {
            newState.players[id].invincibleTime -= c.fixedDeltaTime;
        }

        if (!newState.players[id].connected)
        {
            return false;
        }
        Vector2 axis = new Vector2(input.xAxis, input.yAxis);
        if (Mathf.Abs(axis.x) < Mathf.Epsilon && Mathf.Abs(axis.y) < Mathf.Epsilon)
        {
            return false;
        }

        if (axis.magnitude > 1f)
        {
            axis.Normalize();
        }

        newState.players[id].position.x += axis.x * c.playerSpeed * c.fixedDeltaTime;
        newState.players[id].position.y += axis.y * c.playerSpeed * c.fixedDeltaTime;

        newState.players[id].rotation = Vector2.SignedAngle(Vector2.right, axis);

        return true;
    }

    void UpdatePlayerActions(int id, PlayerInput newInput)
    {
        int nearestAntenna = FindAntenaNearPlayer(newState.players[id].position);

        if (nearestAntenna < 0)
        {
            return;
        }

        if (newState.antenas[nearestAntenna].refreshTime > 0)
            return;

        if (newInput.up)
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorUp) ? GameState.ColorState.Off : GameState.ColorState.ColorUp;
            newState.antenas[nearestAntenna].refreshTime = c.antenaRefreshTime;
        }
        else if (newInput.down)
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorDown) ? GameState.ColorState.Off : GameState.ColorState.ColorDown;
            newState.antenas[nearestAntenna].refreshTime = c.antenaRefreshTime;
        }
        else if (newInput.left)
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorLeft) ? GameState.ColorState.Off : GameState.ColorState.ColorLeft;
            newState.antenas[nearestAntenna].refreshTime = c.antenaRefreshTime;
        }
        else if (newInput.right)
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorRight) ? GameState.ColorState.Off : GameState.ColorState.ColorRight;
            newState.antenas[nearestAntenna].refreshTime = c.antenaRefreshTime;
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

        for (int i = 0; i < newState.antenas.Length; ++i)
        {
            for (int j = i + 1; j < newState.antenas.Length; ++j)
            {
                float d = Vector2.Distance(newState.antenas[i].position, newState.antenas[j].position);
                if (d <= v.antenas[j].linkMaxRadius && d <= v.antenas[i].linkMaxRadius
                    && newState.antenas[i].lastState == newState.antenas[j].lastState && newState.antenas[i].lastState != GameState.ColorState.Off) {

                    Vector3 from = new Vector3(newState.antenas[i].position.x, 0.5f, newState.antenas[i].position.y);
                    Vector3 dir = new Vector3(newState.antenas[j].position.x, 0.5f, newState.antenas[j].position.y) - from;
                    if (Physics.RaycastNonAlloc(from, dir, raycastsHits, d) == 0)
                    {
                        antenaConnections[i].Add(j);
                        antenaConnections[j].Add(i);
                        antenaLinks.Add(new Vector2i(i, j));
                    }
                }
            }
        }
    }

    void UpdateAntennaTimings () {
        for (int i = 0; i < newState.antenas.Length; ++i)
        {
            if (newState.antenas[i].refreshTime > 0)
            {
                newState.antenas[i].refreshTime -= c.fixedDeltaTime;

                if (newState.antenas[i].refreshTime <= 0) {
                    newState.antenas[i].lastState = newState.antenas[i].state;
                }
            }
        }
    }

    int FindAntenaNearPlayer(Vector2 playerPos)
    {
        for (int i = 0; i < newState.antenas.Length; ++i)
        {
            if (Vector2.Distance(playerPos, newState.antenas[i].position) < v.antenas[i].activationRadius) return i;
        }
        return -1;
    }

    void UpdateMessageStates() {
        for (int i = 0; i < newState.messages.Length; ++i)
        {
            if (newState.messages[i].state == GameState.MessageInfo.MessageState.Out && newState.messages[i].transmissionTime > 0)
            {
                newState.messages[i].transmissionTime -= c.fixedDeltaTime;

                if (newState.messages[i].transmissionTime <= 0)
                {
                    InstantiateMessage(i);
                }
            }
            else if (newState.messages[i].state == GameState.MessageInfo.MessageState.End && newState.messages[i].transmissionTime > 0)
            {
                newState.messages[i].transmissionTime -= c.fixedDeltaTime;

                if (newState.messages[i].transmissionTime <= 0)
                {
                    newState.messages[i].state = GameState.MessageInfo.MessageState.Out;

                    CheckWon();
                }
            }
        }
    }

    void CheckWon() {
        bool won = true;

        for (int i = 0; won && i < newState.messages.Length; ++i) {
            won &= newState.messages[i].state == GameState.MessageInfo.MessageState.Out && newState.messages[i].transmissionTime <= 0;
        }

        if (won) {
            int winner = 0;

            for (int i = 1; i < newState.players.Length; ++i) {
                if (newState.players[i].points > newState.players[winner].points) {
                    winner = i;
                }
            }

            newState.winnerPlayer = winner;
            newState.winTime = c.winShowTime;
        }
    }

    void InstantiateMessage(int id) {
        int receiver = receiverAntenaId[rand.Next(0, receiverAntenaId.Length)]; //FindFreeReceiver();
        if (receiver >= 0) {
            newState.messages[id].currentAntena = receiver;
            newState.messages[id].state = GameState.MessageInfo.MessageState.Playing;
            newState.messages[id].transmissionTime = c.messageTransmissionTime;
        }
    }

    int FindFreeReceiver() {
        List<int> freeReceivers = new List<int>(receiverAntenaId.Length);

        for (int i = 0; i < receiverAntenaId.Length; ++i) {
            bool free = true;
            for (int j = 0; free && j < newState.messages.Length; ++j) {
                if (newState.messages[j].color != GameState.ColorState.Off &&
                    newState.messages[j].currentAntena == receiverAntenaId[i]) {
                    free = false;
                    break;
                }
            }

            if (free) freeReceivers.Add(receiverAntenaId[i]);
        }

        if (freeReceivers.Count == 0)
            return -1;
        else {
            return freeReceivers[rand.Next(0, freeReceivers.Count)];
        }
    }

    bool CircleLineCollides (Vector2 lPos1, Vector2 lPos2, Vector2 p, float r)
    {
        Vector2 lineDir = lPos2 - lPos1;
        float lineMagnitude = lineDir.magnitude;
        lineDir.Normalize();

        Vector2 delta1 = p - lPos1;
        float d = Vector2.Dot(delta1, lineDir);

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

    Vector2 CircleAABBCorrect(Vector2 cAfter, float r, Bounds aabb)
    {
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

    Vector2 CircleCircleCorrect(Vector2 c1, float r1, Vector2 c2, float r2)
    {
        Vector2 normal = c1 - c2;
        if (Mathf.Abs(normal.sqrMagnitude) <= 0.01f) { // Inside, whoops
            float angle = Random.Range(0, 2f*Mathf.PI);
            normal.x = Mathf.Cos(angle);
            normal.y = Mathf.Sin(angle);
        }
        else normal.Normalize();

        return c2 + normal * (r1 + r2);
    }

    bool SegmentAABBIntersect (Vector2 p1, Vector2 p2, Bounds aabb)
    {
        return SegmentSegmentCollides(p1, p2, aabb.min, aabb.max) || SegmentSegmentCollides(p1, p2, new Vector2(aabb.min.x, aabb.max.y), new Vector2(aabb.max.x, aabb.min.y));
    }

    bool SegmentSegmentCollides(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2) {

        Vector2 a = p2 - p1;
        Vector2 b = q1 - q2;
        Vector2 c = p1 - q1;

        float alphaNumerator = b.y * c.x - b.x * c.y;
        float alphaDenominator = a.y * b.x - a.x * b.y;
        float betaNumerator = a.x * c.y - a.y * c.x;
        float betaDenominator = a.y * b.x - a.x * b.y;

        bool doIntersect = true;

        if (alphaDenominator == 0 || betaDenominator == 0)
        {
            doIntersect = false;
        }
        else
        {

            if (alphaDenominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
                {
                    doIntersect = false;

                }
            }
            else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
            {
                doIntersect = false;
            }

            if (doIntersect && betaDenominator > 0) {
                if (betaNumerator < 0 || betaNumerator > betaDenominator)
                {
                    doIntersect = false;
                }
            } else if (betaNumerator > 0 || betaNumerator < betaDenominator)
            {
                doIntersect = false;
            }
        }

        return doIntersect;
    }

    public bool IsAntenaLinking(int antenaId)
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
