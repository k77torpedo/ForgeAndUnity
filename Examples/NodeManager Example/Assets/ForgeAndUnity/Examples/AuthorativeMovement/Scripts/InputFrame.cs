/// <summary>
/// A frame of input a player has done.
/// </summary>
[System.Serializable]
public struct InputFrame {
    //Fields
    public uint frame;
    public byte[] actions;
    public float horizontalInput;
    public float verticalInput;
}
