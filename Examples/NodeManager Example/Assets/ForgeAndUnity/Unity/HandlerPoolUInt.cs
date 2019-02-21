/// <summary>
/// Class for generating identifiers for various needs like sessionIDs, EntityIDs, HandlerIDs etc.
/// </summary>
public class HandlerPoolUInt : HandlerPoolBaseOfT<uint> {
    //Functions
    public HandlerPoolUInt () : this(uint.MinValue, uint.MaxValue) { }

    public HandlerPoolUInt (uint pLowerBounds, uint pUpperBounds, uint pStartIdentifier = default(uint), bool pUseFreeIds = true) : base(pLowerBounds, pUpperBounds, pStartIdentifier, pUseFreeIds) { }

    protected override uint NextIdentifierIncrement (uint pNextIdentifier) {
        return pNextIdentifier + 1;
    }
}
