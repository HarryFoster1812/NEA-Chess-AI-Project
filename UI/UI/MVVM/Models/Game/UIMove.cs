using Engine;
using System.Collections.Generic;

namespace UI.MVVM.Models
{
    internal class UIMove
    {
        public Move Move { get; set; }
        public string MoveStringAN = ""; //stores the algebraic notation of the move eg e4, e5, Nf3 
        public string MoveStringUCI = "";//stores the UCI notation for the move eg e2e4, e7e5, g1f3
        public List<string> Comments = new List<string>(); // comments stored in the format
        public readonly string uciCommand = "";


        public UIMove(Move move, Bitboard positionBeforeMove, string uciCommand)
        {
            Move = move;
            MoveStringAN = MoveToAN(move, positionBeforeMove);
            MoveStringUCI = MoveToUCI(move, positionBeforeMove);
            this.uciCommand = uciCommand + MoveStringUCI + " ";
        }

        public UIMove(Move move, Bitboard positionBeforeMove)
        {
            Move = move;
            MoveStringAN = MoveToAN(move, positionBeforeMove);
            MoveStringUCI = MoveToUCI(move, positionBeforeMove);
            this.uciCommand = "";
        }
        public static string MoveToAN(Move move, Bitboard positionBeforeMove)
        {
            string ANMove = "";
            int enemyIndex = positionBeforeMove.WhiteToPlay ? 1 : 0;

            if (move.flag == Move.castling)
            {
                switch (move.destinationIndex)
                {
                    case 1:
                    case 57:
                        return "O-O";
                    case 5:
                    case 61:
                        return "O-O-O";
                }
            }

            // find which piece is moving
            switch (Bitboard.getPieceType(positionBeforeMove.Board, BitBoardTools.IndexToBitboard(move.startIndex)))
            {
                case 3: //knight
                    ANMove += "n";
                    break;
                case 4: //bishop
                    ANMove += "b";
                    break;

                case 5: //rook
                    ANMove += "r";
                    break;

                case 6: //queen
                    ANMove += "q";
                    break;

                case 7: //king
                    ANMove += "k";
                    break;

            }

            if ((positionBeforeMove.Board[enemyIndex] & BitBoardTools.IndexToBitboard(move.destinationIndex)) != 0)
            { // it is a capture
                if (ANMove == "")
                { // pawn is capturing so the start file needs to be appended

                    ANMove += (char)(BitBoardTools.CalculateFile(move.startIndex) + 96); // 96 is the char code before a, Calculate file returns 1-8. This will reult in 1=a 2=b ect..
                }

                return ANMove + "x" + Tools.IndexToSquare(move.destinationIndex);
            }

            // check if piece of the same type can move to the same square

            ANMove += Tools.IndexToSquare(move.destinationIndex);

            if (move.isPromotion)
            {
                ANMove += "=";
                switch (move.pieceIndex)
                {
                    case 0: //knight
                        ANMove += "n";
                        break;
                    case 1: //bishop
                        ANMove += "b";
                        break;

                    case 2: //rook
                        ANMove += "r";
                        break;

                    case 3: //queen
                        ANMove += "q";
                        break;

                }
            }

            return ANMove;
        }

        public static string MoveToUCI(Move move, Bitboard positionBeforeMove)
        {
            string result = string.Empty;
            result += Tools.IndexToSquare(move.startIndex);
            result += Tools.IndexToSquare(move.destinationIndex);

            if (move.isPromotion)
            {
                switch (move.pieceIndex)
                {
                    case 0: //knight
                        result += "n";
                        break;
                    case 1: //bishop
                        result += "b";
                        break;

                    case 2: //rook
                        result += "r";
                        break;

                    case 3: //queen
                        result += "q";
                        break;

                }
            }

            return result;
        }
    }
}
