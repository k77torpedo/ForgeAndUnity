/// <summary>
/// Interface for serializing an object to a struct for transmition over the network via a Remote Procedure Call (RPC).
/// </summary>
/// <typeparam name="T">The type of the RPC-Object</typeparam>
public interface IRPCSerializable<T> : IRPCSerializable where T : struct {
    T ToRPC ();
    void FromRPC (T pRPC);
}
