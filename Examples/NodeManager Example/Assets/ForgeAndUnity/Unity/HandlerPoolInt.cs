/// <summary>
/// Class for generating identifiers for various needs like sessionIDs, EntityIDs, HandlerIDs etc.
/// </summary>
public class HandlerPoolInt : HandlerPoolBaseOfT<int> {
    //Functions
    public HandlerPoolInt () : this(int.MinValue, int.MaxValue) { }

    public HandlerPoolInt (int pLowerBounds, int pUpperBounds, int pStartIdentifier = default(int), bool pUseFreeIds = true) : base(pLowerBounds, pUpperBounds, pStartIdentifier, pUseFreeIds) { }

    protected override int NextIdentifierIncrement (int pNextIdentifier) {
        return pNextIdentifier + 1;
    }
}
