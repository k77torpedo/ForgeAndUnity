/// <summary>
/// Class for generating identifiers for various needs like sessionIDs, EntityIDs, HandlerIDs etc.
/// </summary>
public class HandlerPoolUShort : HandlerPoolBaseOfT<ushort> {
    //Functions
    public HandlerPoolUShort () : this (ushort.MinValue, ushort.MaxValue) { }

    public HandlerPoolUShort (ushort pLowerBounds, ushort pUpperBounds, ushort pStartIdentifier = default(ushort), bool pUseFreeIds = true) : base(pLowerBounds, pUpperBounds, pStartIdentifier, pUseFreeIds) { }

    protected override ushort NextIdentifierIncrement (ushort pNextIdentifier) {
        return (ushort)(pNextIdentifier + 1);
    }
}
