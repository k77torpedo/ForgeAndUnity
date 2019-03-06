/// <summary>
/// An <see cref="InputFrame"/> with the absolute position when it was played.
/// </summary>
[System.Serializable]
public struct InputFrameHistoryItem {
    //Fields
    public uint frame;
    public float xPosition;
    public float yPosition;
    public float zPosition;
    public InputFrame inputFrame;
}

