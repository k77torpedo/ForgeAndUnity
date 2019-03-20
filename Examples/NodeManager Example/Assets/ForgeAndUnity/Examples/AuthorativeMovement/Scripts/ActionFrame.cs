/// <summary>
/// A frame of action a player has done with a <see cref="InputListener"/>.
/// </summary>
[System.Serializable]
public struct ActionFrame {
    //Fields
    public byte actionId;
    public byte[] data;
}