namespace Engine.PolyGlot
{
    internal struct PolyEntry
    {
        #region values
        internal ulong key;
        internal ushort move;
        internal ushort weight;
        internal uint learn;
        #endregion

        #region masks
        private const ushort destFileMask = 0b111;
        private const ushort destRowMask = 0b111_000;
        private const ushort startFileMask = 0b111_000_000;
        private const ushort startRowMask = 0b111_000_000_000;
        private const ushort promotionMask = 0b111_000_000_000_000;
        #endregion

        internal PolyEntry(ulong Key, ushort Move, ushort Weight, uint Learn)
        {
            key = Key;
            move = Move;
            weight = Weight;
            learn = Learn;
        }

        #region Functions
        public byte destinationFile => (byte)(move & destFileMask);

        public byte destinationRow => (byte)((move & destRowMask) >> 3);

        public byte startFile => (byte)((move & startFileMask) >> 6);

        public byte startRow => (byte)((move & startRowMask) >> 9);

        public int pieceIndex => (byte)((move & startRowMask) >> 12) + 2;

        public bool isPromotion => ((byte)((move & promotionMask) >> 12) > 0);
        #endregion
    }
}
