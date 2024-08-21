using System;

namespace Engine.Representaion
{
    [Serializable]
    internal struct BoardState
    {
        public ZobristKey zobristKey;

        public ushort castlingRights;

        public ushort ply;

        public ushort moves;

        public short enpassant_flag;

        public bool WhiteToPlay;

        public int capturedPiece;

        public GameState gameState;

        public BoardState(ZobristKey zobristkey, ushort castling, ushort plyCount, ushort Moves, int CapturedPiece, short EnpassantFlag, GameState state, bool Turn)
        {
            zobristKey = zobristkey;
            castlingRights = castling;
            ply = plyCount;
            moves = Moves;
            enpassant_flag = EnpassantFlag;
            capturedPiece = CapturedPiece;
            gameState = state;
            WhiteToPlay = Turn;
        }

    }
}
