using UnityEngine;

public struct GameState
{
    public enum ColorState {
        Off = 0,
        ColorUp = 1,
        ColorDown = 2,
        ColorLeft = 3,
        ColorRight = 4
    }
    public struct PlayerInfo
    {
        public bool connected;
        public Vector2 position;
        public float rotation;
        public float stunnedTime;
        public float invincibleTime;
        public bool moving;
        public int points;
    }
    public struct AntenaInfo
    {
        public ColorState state;
        public ColorState lastState;
        public Vector2 position;
        public float rotation;
        public float refreshTime;
    }
    public struct MessageInfo
    {
        public enum MessageState {
            Out,
            Playing,
            End
        }
        public ColorState color;
        public int currentAntena;
        public int nextAntena;
        public int lastAntena;
        public float transmissionTime;
        public MessageState state;
    }

    //
    public PlayerInfo[] players;
    public AntenaInfo[] antenas;
    public MessageInfo[] messages;
    public int winnerPlayer;
    public int seed;

    public GameState(GameState from) {
        players = null;
        if (from.players != null)
        {
            players = new PlayerInfo[from.players.Length];
            from.players.CopyTo(players, 0);
        }

        antenas = null;
        if (from.antenas != null)
        {
            antenas = new AntenaInfo[from.antenas.Length];
            from.antenas.CopyTo(antenas, 0);
        }

        messages = null;
        if (from.messages != null)
        {
            messages = new MessageInfo[from.messages.Length];
            from.messages.CopyTo(messages, 0);
        }

        winnerPlayer = from.winnerPlayer;
        seed = from.seed;
    }

    public void CopyFrom(GameState from) {
        players = null;
        if (from.players != null)
        {
            players = new PlayerInfo[from.players.Length];
            from.players.CopyTo(players, 0);
        }

        antenas = null;
        if (from.antenas != null)
        {
            antenas = new AntenaInfo[from.antenas.Length];
            from.antenas.CopyTo(antenas, 0);
        }

        messages = null;
        if (from.messages != null)
        {
            messages = new MessageInfo[from.messages.Length];
            from.messages.CopyTo(messages, 0);
        }

        winnerPlayer = from.winnerPlayer;
        seed = from.seed;
    }
}