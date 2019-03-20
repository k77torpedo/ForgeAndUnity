/// <summary>
/// An <see cref="InputFrame"/> with the absolute position when it was played with an <see cref="InputListener"/>.
/// </summary>
[System.Serializable]
public struct InputFrameHistoryItem {
    //Fields
    public float xPosition;
    public float yPosition;
    public float zPosition;
    public InputFrame inputFrame;
}

