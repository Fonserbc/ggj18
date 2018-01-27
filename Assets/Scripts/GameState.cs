using UnityEngine;

public struct GameState
{
    public struct PlayerInfo
    {
        public bool connected;
        public Vector2 position;
        public float rotation;
        public int stunned;
        public int invincible;
    }
    public struct AntenaInfo
    {
        public enum AntenaState {
            Off,
            ColorUp,
            ColorDown,
            ColorLeft,
            ColorRight
        }
        public AntenaState state;
        public Vector2 position;
        public float rotation;
    }

    //
    public PlayerInfo[] players;
    public AntenaInfo[] antenas;
}