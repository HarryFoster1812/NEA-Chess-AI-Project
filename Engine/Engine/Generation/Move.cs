namespace Engine
{

    // Each move is represented as a 16bit ushort  
    // Bits 1-6 will represent the starting square
    // Bits 6-12 will represent the destination square
    // Bits 12-16 will represent the flag

    // For example to move a pawn from e2 to e4 the move would be represented as the following:

    // 0010 11011 01011 
    // enpassant set flag -- 27 is destination -- 11 is the starting square
    public struct Move
    {
        readonly ushort move;


        /*
         4 bits to play around with max value is 1111 = 15 
         
         0000 - 0 - no flag
		 0001 - 1 - Enpassant capture
		 0010 - 2 - Castling
		 0011 - 3 - EnPassant Set
		 
		 Promotion
		 0100 - 4 - Knight 
		 0101 - 5 - Bishop
		 0110 - 6 - Rook
		 0111 - 7 - Queen
		 
         
         */

        const ushort startIndexMask = 0b111111;
        const ushort destinationIndexMask = 0b111111_000000;
        const ushort flagMask = 0b1111_000000_000000;
        public const ushort enPassantCapture = 1;
        public const ushort castling = 2;
        public const ushort enPassantSet = 3;

        public Move(bool empty) // empty move the parameter is because c# does not support parameterless structs
        {
            this.move = 0;
        }
        public Move(ushort moveValue)
        {
            this.move = moveValue;
        }

        public Move(byte start, byte destination) // we know it is a quiet move so we know the flag will be 0
        {
            move = (ushort)(start | (destination << 6));
        }

        public Move(ulong start, ulong destination) // if we need to pass through the bitboards instead of the index values
        {
            start = BitBoardTools.BitboardToIndex(start);
            destination = BitBoardTools.BitboardToIndex(destination);
            move = (ushort)(start | (destination << 6));
        }

        public Move(byte start, byte destination, ushort flag) // overloading for flags
        {
            move = (ushort)(start | (destination << 6) | (flag << 12));
        }

        public Move(ulong start, ulong destination, ushort flag)
        {
            start = BitBoardTools.BitboardToIndex(start);
            destination = BitBoardTools.BitboardToIndex(destination);

            move = (ushort)(start | (destination << 6) | ((ulong)flag << 12));
        }

        public byte startIndex => (byte)(move & startIndexMask);

        public byte destinationIndex => (byte)((move & destinationIndexMask) >> 6);

        public byte flag => (byte)((move & flagMask) >> 12);

        public bool isPromotion => (flag > 3 && flag < 8);

        public int pieceIndex => (flag & 3);

        public bool isEmpty => (move == 0);

    }
}
