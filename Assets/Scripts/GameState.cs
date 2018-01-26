using UnityEngine;

public struct GameState
{
    public struct PlayerInfo
    {
        public bool connected;
        public Vector3 position;
        public Quaternion rotation;
        public int stunned;
    }
    public struct AntenaInfo
    {
        public enum AntenaState {
            Off,
            Color1,
            Color2,
            Color3,
            Color4
        }
        public AntenaState state;
        public Vector3 position;
        public Quaternion rotation;
    }

    //
    public PlayerInfo[] players;
    public AntenaInfo[] antenas;
}