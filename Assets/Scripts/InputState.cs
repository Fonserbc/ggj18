public struct InputState {
    public struct PlayerInput {
        public float xAxis, yAxis;
        public bool up, down, left, right;
    }

    public PlayerInput[] playerInputs;
}
