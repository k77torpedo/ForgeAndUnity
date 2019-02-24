namespace ForgeAndUnity.Unity {

    /// <summary>
    /// Class for generating identifiers for various needs like sessionIDs, EntityIDs, HandlerIDs etc.
    /// </summary>
    public class HandlerPoolULong : HandlerPoolBaseOfT<ulong> {
        //Functions
        public HandlerPoolULong () : this(ulong.MinValue, ulong.MaxValue) { }

        public HandlerPoolULong (ulong pLowerBounds, ulong pUpperBounds, ulong pStartIdentifier = default(ulong), bool pUseFreeIds = true) : base(pLowerBounds, pUpperBounds, pStartIdentifier, pUseFreeIds) { }

        protected override ulong NextIdentifierIncrement (ulong pNextIdentifier) {
            return pNextIdentifier + 1UL;
        }
    }
}