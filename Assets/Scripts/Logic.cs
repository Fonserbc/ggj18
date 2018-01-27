using System.Collections.Generic;
using UnityEngine;

public class Logic
{
    public Constants c;

    GameState newState;
    int currentFrame;

    Bounds[] staticWorld;

    List<int>[] antenaConnections;
    List<Vector2i> antenaLinks;
    int[] receiverAntenaId;
    int[] baseAntenaId;

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
        }


        newState = new GameState();
        newState.players = new GameState.PlayerInfo[c.numPlayers];
        for (int i = 0; i < newState.players.Length; ++i) {
            newState.players[i] = new GameState.PlayerInfo();
            newState.players[i].position = new Vector2(v.players[i].transform.position.x, v.players[i].transform.position.z);
            newState.players[i].rotation = v.players[i].transform.eulerAngles.y;
            newState.players[i].connected = true;
            newState.players[i].moving = false;
            newState.players[i].antenaInRadius = -1;
            newState.players[i].points = 0;
        }
        newState.antenas = new GameState.AntenaInfo[antenaCount];
        for (int i = 0; i < antenaCount; ++i)
        {
            newState.antenas[i] = new GameState.AntenaInfo();
            newState.antenas[i].position = new Vector2(v.antenas[i].transform.position.x, v.antenas[i].transform.position.z);
            newState.antenas[i].rotation = v.antenas[i].transform.rotation.eulerAngles.y;
        }
        newState.messages = new GameState.MessageInfo[c.numMessages];
        Random.InitState(0);
        for (int i = 0; i < newState.messages.Length; ++i)
        {
            newState.messages[i] = new GameState.MessageInfo();
            newState.messages[i].onScene = false;
            newState.messages[i].transmissionTime = i * c.timeBetweenMessages;
            newState.messages[i].color = (GameState.ColorState)Random.Range(1, 5);
            newState.messages[i].currentAntena = -1;
            newState.messages[i].lastAntena = -1;
            newState.messages[i].nextAntena = -1;
        }

        v.Init(this);

        return newState;
    }

    public void UpdateState(ref GameFrame frame)
    {
        newState = frame.state;
        currentFrame = (int)frame.frame_id;

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

        UpdateAntennaConnections();

        UpdateNewMessages();

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
                if (CircleCircleCollides(newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, c.antenaCollisionRadius))
                {
                    newState.players[i].position = CircleCircleCorrect(newState.players[i].position, c.playerCollisionRadius, newState.antenas[j].position, c.antenaCollisionRadius);

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
            if (currentMessage.color == GameState.ColorState.Off)
                continue;

            // Remove last antena if broken
            if (currentMessage.lastAntena != -1 && newState.antenas[currentMessage.lastAntena].state != currentMessage.color)
            {
                currentMessage.lastAntena = -1;
            }

            // Reset timer if own antena changed color
            if (newState.antenas[currentMessage.currentAntena].state != currentMessage.color)
            {
                currentMessage.transmissionTime = c.messageTransmissionTime;
                currentMessage.nextAntena = -1;
                currentMessage.lastAntena = -1;
            }
            else if (currentMessage.nextAntena == -1)
            {   // Find possible next antena
                List<int> possibleNextAntenas = new List<int>(antenaConnections[currentMessage.currentAntena].Count);
                for (int j = 0; j < antenaConnections[currentMessage.currentAntena].Count; ++j) {
                    if (currentMessage.color == newState.antenas[antenaConnections[currentMessage.currentAntena][j]].state) {
                        possibleNextAntenas.Add(antenaConnections[currentMessage.currentAntena][j]);
                    }
                }
                if (possibleNextAntenas.Count > 0)
                {
                    currentMessage.nextAntena = possibleNextAntenas[currentFrame % possibleNextAntenas.Count];
                }
            }

            if (newState.antenas[currentMessage.currentAntena].state != currentMessage.color && newState.antenas[currentMessage.nextAntena].state != currentMessage.color)
            {
                currentMessage.transmissionTime -= c.fixedDeltaTime;

                if (currentMessage.transmissionTime <= 0) {
                    currentMessage.lastAntena = currentMessage.currentAntena;
                    currentMessage.currentAntena = currentMessage.nextAntena;
                    currentMessage.transmissionTime = c.messageTransmissionTime;

                    for (int j = 0; j < baseAntenaId.Length; ++j) {
                        if (currentMessage.currentAntena == baseAntenaId[j]) {
                            newState.players[j].points++;
                            currentMessage.onScene = false;
                            currentMessage.transmissionTime = 0f;
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
            newState.players[id].antenaInRadius = -1;
            return;
        }

        if (newInput.justUp || (newInput.up && newState.players[id].antenaInRadius != nearestAntenna))
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorUp) ? GameState.ColorState.Off : GameState.ColorState.ColorUp;
        }
        else if (newInput.justDown || (newInput.down && newState.players[id].antenaInRadius != nearestAntenna))
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorDown) ? GameState.ColorState.Off : GameState.ColorState.ColorDown;
        }
        else if (newInput.justLeft || (newInput.left && newState.players[id].antenaInRadius != nearestAntenna))
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorLeft) ? GameState.ColorState.Off : GameState.ColorState.ColorLeft;
        }
        else if (newInput.justRight || (newInput.right && newState.players[id].antenaInRadius != nearestAntenna))
        {
            newState.antenas[nearestAntenna].state = (newState.antenas[nearestAntenna].state == GameState.ColorState.ColorRight) ? GameState.ColorState.Off : GameState.ColorState.ColorRight;
        }

        newState.players[id].antenaInRadius = nearestAntenna;
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
                    && newState.antenas[i].state == newState.antenas[j].state && newState.antenas[i].state != GameState.ColorState.Off) {
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

    void UpdateNewMessages() {
        for (int i = 0; i < newState.messages.Length; ++i)
        {
            if (!newState.messages[i].onScene && newState.messages[i].transmissionTime > 0)
            {
                newState.messages[i].transmissionTime -= c.fixedDeltaTime;

                if (newState.messages[i].transmissionTime <= 0)
                {
                    InstantiateMessage(i);
                }
            }
        }
    }

    void InstantiateMessage(int id) {
        int receiver = FindFreeReceiver();

        if (receiver >= 0) {
            newState.messages[id].currentAntena = receiver;
            newState.messages[id].onScene = true;
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
            return freeReceivers[currentFrame % freeReceivers.Count];
        }
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
