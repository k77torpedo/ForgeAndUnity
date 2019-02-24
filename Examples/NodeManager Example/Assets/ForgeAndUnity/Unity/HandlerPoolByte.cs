namespace ForgeAndUnity.Unity {

    /// <summary>
    /// Class for generating identifiers for various needs like sessionIDs, EntityIDs, HandlerIDs etc.
    /// </summary>
    public class HandlerPoolByte : HandlerPoolBaseOfT<byte> {
        //Functions
        public HandlerPoolByte () : this(byte.MinValue, byte.MaxValue) { }

        public HandlerPoolByte (byte pLowerBounds, byte pUpperBounds, byte pStartIdentifier = default(byte), bool pUseFreeIds = true) : base(pLowerBounds, pUpperBounds, pStartIdentifier, pUseFreeIds) { }

        protected override byte NextIdentifierIncrement (byte pNextIdentifier) {
            return (byte)(pNextIdentifier + 1);
        }
    }
}
