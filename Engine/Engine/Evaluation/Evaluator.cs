namespace Engine.Evaluation
{
    internal struct BoardInfo
    {

        internal int wPawnCount;
        internal int bPawnCount;
        internal int PawnCount => wPawnCount + bPawnCount;

        internal int wKnightCount;
        internal int bKnightCount;
        internal int KnightCount => wKnightCount + bKnightCount;

        internal int wBishopCount;
        internal int bBishopCount;
        internal int BishopCount => wBishopCount + bBishopCount;

        internal int wRookCount;
        internal int bRookCount;
        internal int RookCount => wRookCount + bRookCount;


        internal int wQueenCount;
        internal int bQueenCount;
        internal int QueenCount => wQueenCount + bQueenCount;


        internal BoardInfo(Bitboard bitboard)
        {
            ulong wPieces = bitboard.Board[0];
            ulong bPieces = bitboard.Board[1];

            wPawnCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Pawns] & wPieces);
            bPawnCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Pawns] & bPieces);

            wKnightCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Knights] & wPieces);
            bKnightCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Knights] & bPieces);

            bBishopCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Bishops] & bPieces);
            wBishopCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Bishops] & wPieces);

            wRookCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Rooks] & wPieces);
            bRookCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Rooks] & bPieces);

            wQueenCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Queen] & wPieces);
            bQueenCount = Tools.countOnes(bitboard.Board[(int)Tools.BB.Queen] & bPieces);
        }

    }

    internal class Evaluator
    {
        Bitboard board;

        #region Bonuses
        const double BishopPairBonus = 50;
        const double PassedPawnBonus = 50;
        const double RookOnOpenFileBonus = 10;
        #endregion

        #region Penalties
        const double DoublePawnsPenalty = -10;
        const double IsolatedPawnPenalty = -25;
        const double BackwardsPawnPenalty = -10;
        #endregion

        static readonly double[][] materialScores = {
            // opening scores
            new double[] { 82, 337, 365, 477, 1025, 12000 },
            //endgame scores
            new double[] { 94, 291, 297, 512, 936, 12000 }

        };

        enum gamephase { opening, endgame, middlegame }
        const double openingPhaseValue = 6192;
        const double endgamePhaseValue = 518;

        private double calculateGamePhase(BoardInfo info)
        {
            double pawnPhase = 0;
            double knightPhase = 1;
            double bishopPhase = 1;
            double rookPhase = 1;
            double queenPhase = 1;

            double phase = 0;

            phase += info.PawnCount * materialScores[0][0] * pawnPhase;
            phase += info.KnightCount * materialScores[0][1] * knightPhase;
            phase += info.BishopCount * materialScores[0][2] * bishopPhase;
            phase += info.RookCount * materialScores[0][3] * rookPhase;
            phase += info.QueenCount * materialScores[0][4] * queenPhase;

            return phase;
        }

        public double Evaluate(Bitboard board)
        {
            this.board = board;
            gamephase phase = gamephase.middlegame;
            BoardInfo info = new BoardInfo(board);

            double value = 0;

            double gamePhaseValue = calculateGamePhase(info);

            if (gamePhaseValue > openingPhaseValue) phase = gamephase.opening;
            else if (gamePhaseValue < endgamePhaseValue) phase = gamephase.endgame;

            // loop over each piece and give it a material score (exact or interpolated) and a positional score 

            for (int i = 2; i < board.Board.Length; i++)
            {

                // loop over each piece
                int pieceType = i - 2; // the piece bitboards start at index 2, the indexes in material values start at 0
                ulong pieceBoard = board.Board[i];

                while (pieceBoard != 0)
                {
                    // get the location of the piece
                    ulong loc = BitBoardTools.popLSB(ref pieceBoard);
                    byte index = BitBoardTools.BitboardToIndex(loc);
                    bool isWhite = board.IsColour(loc, 0);
                    double ColourSign = isWhite ? 1 : -1; // if it is white then add, if it is black subtract

                    // check the phase

                    // use exact values
                    if (phase != gamephase.middlegame)
                    {
                        // add the piece values
                        value += materialScores[(int)phase][pieceType] * ColourSign;

                        // add positional values
                        value += PieceTables.LookUp(pieceType, index, (int)phase, isWhite) * ColourSign;

                    }

                    // interpolate values
                    else
                    {
                        double opEval = materialScores[0][pieceType];
                        double egEval = materialScores[1][pieceType];
                        value += ((opEval * (gamePhaseValue) + (egEval * (openingPhaseValue - gamePhaseValue))) / openingPhaseValue) * ColourSign;


                        double opScore = 0;
                        double egScore = 0;
                        value += ((opScore * (gamePhaseValue) + (egScore * (openingPhaseValue - gamePhaseValue))) / openingPhaseValue) * ColourSign;
                    }

                    // add piece specific bonuses/penalties
                    if (pieceType == 0)
                    { // pawns 
                        // passed pawn
                        // double pawn
                        // backwards pawn
                        // isolated pawn
                    }

                    else if (pieceType == 1)
                    { // knight 
                        // add your own bounuses / penalties
                    }

                    else if (pieceType == 2)
                    { // bishop
                        // add your own bounuses / penalties
                    }

                    else if (pieceType == 3)
                    { // rook
                        // open file / semi open file
                    }

                    else if (pieceType == 4)
                    { // queen

                    }

                    else if (pieceType == 5)
                    { // king
                        // pawn shield
                    }
                }
            }
            return value;

        }

        public double EvaluateFromPov(Bitboard board)
        {
            double value = Evaluate(board);

            return board.WhiteToPlay ? value : -value;

        }

    }
}
