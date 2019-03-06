/// <summary>
/// A frame of input a player has done.
/// </summary>
[System.Serializable]
public struct InputFrame {
    //Fields
    public uint frame;
    public byte[] actions;
    public float horizontalMovement;
    public float verticalMovement;

    public bool HasMovement { get { return (horizontalMovement != 0f || verticalMovement != 0f); } }
    public bool HasActions { get { return (actions != null && actions.Length > 0); } }
}
