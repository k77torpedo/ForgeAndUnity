namespace ForgeAndUnity.Forge {

    /// <summary>
    /// Enum for returning simple callback-results on a <see cref="ServiceCallback"/>.
    /// </summary>
    [System.Serializable]
    public enum ServiceCallbackStateEnum : byte {
        NONE,
        AWAITING_RESPONSE,
        RESPONSE_SUCCESS,
        RESPONSE_FAILED,
        ERROR_TIMEOUT,
        ERROR_SERVICE_NOT_INITIALIZED,
        ERROR_NO_CONNECTION,
        ERROR_NO_DATA
    }
}
