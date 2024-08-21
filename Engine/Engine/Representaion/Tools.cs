using Engine.Representaion;
using System;
using System.Collections.Generic;

namespace Engine
{
    public static class Tools
    {
        public enum BB : int
        {
            WhitePieces,
            BlackPieces,
            Pawns,
            Knights,
            Bishops,
            Rooks,
            Queen,
            King,
        };

        public enum Castling
        {
            KC = 1,
            QC = 2,
            kc = 4,
            qc = 8,
        };

        /// <summary>
        /// Outputs a visual representation of the bitboard. 0's are outputted as '.'
        /// </summary>
        /// <param name="b">The bitboard</param>
        public static void printBitboard(ulong b)
        {
            // converts the bitboard into binary
            string binary = Convert.ToString((long)b, 2);
            // pads zeros so that it is formatted correctly
            binary = binary.PadLeft(64, '0');
            // replaces 0 to . for comfort 
            binary = binary.Replace('0', '.');

            Console.WriteLine(
                "8 " + binary.Substring(0, 8) + "\n" +
                "7 " + binary.Substring(8, 8) + "\n" +
                "6 " + binary.Substring(16, 8) + "\n" +
                "5 " + binary.Substring(24, 8) + "\n" +
                "4 " + binary.Substring(32, 8) + "\n" +
                "3 " + binary.Substring(40, 8) + "\n" +
                "2 " + binary.Substring(48, 8) + "\n" +
                "1 " + binary.Substring(56, 8) + "\n" +
                "  abcdefgh\n");

        }

        /// <summary>
        /// Outputs the ascii representation of the given board
        /// </summary>
        /// <param name="b">The board to be displayed</param>
        public static void printPieceBoard(Bitboard b)
        {
            Console.WriteLine(PieceBoardToString(b));
        }

        /// <summary>
        /// Creates string which is the ascii representation of the given board
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string PieceBoardToString(Bitboard b)
        {
            string output = "";
            string[] board = new string[64];
            for (byte i = 0; i < 64; i++) // loop over each square
            {
                int pieceType = Bitboard.getPieceType(b.Board, BitBoardTools.IndexToBitboard(i));
                string ascii = string.Empty;
                // get the type of piece
                switch (pieceType)
                {
                    case 2: ascii = "p"; break;

                    case 3: ascii = "n"; break;

                    case 4: ascii = "b"; break;

                    case 5: ascii = "r"; break;

                    case 6: ascii = "q"; break;

                    case 7: ascii = "k"; break;

                    default: ascii = " "; break;
                }
                // if it is black then make it upper case
                if (b.IsColour(BitBoardTools.IndexToBitboard(i), 0))
                {
                    ascii = ascii.ToUpper();
                }
                // store it in the ascii representation of the board
                board[63 - i] = ascii;
            }


            int counter = 7; // start at 7 since we count down

            for (int i = 0; i < 17; i++) // 17 is the total number of rows used to create the table
            {
                if (i % 2 == 0)
                {
                    output += "\n   +---+---+---+---+---+---+---+---+";
                }
                else
                {
                    int rowNumber = 8 - ((i - 1) / 2);
                    output += ($" \n {rowNumber} | {board[counter - 7]} | {board[counter - 6]} | {board[counter - 5]} | {board[counter - 4]} | {board[counter - 3]} | {board[counter - 2]} | {board[counter - 1]} | {board[counter]} |");
                    // go to the next row
                    counter += 8;
                }
            }
            output += ("\n     a   b   c   d   e   f   g   h\n");

            output += ($"\nTurn: {(b.WhiteToPlay ? "White" : "Black")}");
            output += ($"\nCastling Rights: {castlingToString(b.castlingRights)}");
            output += ($"\nEn Passant: {(b.enPassantFile == -1 ? -1 : IndexToFile((byte)b.enPassantFile))}");
            output += ($"\nZobrist Key: {b.zobristKey.Key.ToString("X")}");
            return output;
        }

        /// <summary>
        /// Counts the amount of 1's in a binary 64-bit int
        /// </summary>
        /// <param name="u">A 64-bit intger</param>
        /// <returns>The number of 1's</returns>
        public static byte countOnes(ulong u)
        {
            byte a = 0;

            while (u != 0)
            {
                a++;
                u = u & (u - 1);
            }

            return a;
        }

        /// <summary>
        /// Converts the bit representation of castling rights to a string
        /// </summary>
        /// <example>1111 would return "KQkq"</example>
        /// <param name="castling"></param>
        /// <returns>The string of the catling rights</returns>
        public static string castlingToString(ushort castling)
        {
            string result = "";
            ulong ulongcastling = (ulong)castling;
            while (ulongcastling != 0)
            {
                ulong castlingright = BitBoardTools.popLSB(ref ulongcastling);
                switch (castlingright)
                {
                    case 1:
                        result += "K";
                        break;
                    case 2:
                        result += "Q";
                        break;
                    case 4:
                        result += "k";
                        break;
                    case 8:
                        result += "q";
                        break;
                }
            }
            if (result == "")
            {
                return "-";
            }
            return result;
        }

        public static Bitboard FENtoBitboard(string FEN)
        {

            FEN = FEN.Trim();
            if (FEN.Length == 0)
            {
                throw new Exception("Invalid FEN String");
            }
            string[] FENSplit;
            string[] FENBoard;

            FENSplit = FEN.Split(' ');

            if (FENSplit.Length < 6)
            {
                throw new Exception("Invalid FEN String");
            }

            FENBoard = FENSplit[0].Split('/');
            Bitboard b = new Bitboard();

            b.Board[(int)BB.WhitePieces] = FENToBinary(FENBoard, new List<char>() { 'P', 'R', 'B', 'N', 'Q', 'K' });
            b.Board[(int)BB.BlackPieces] = FENToBinary(FENBoard, new List<char>() { 'r', 'n', 'b', 'q', 'k', 'p' });
            b.Board[(int)BB.Pawns] = FENToBinary(FENBoard, new List<char>() { 'p', 'P' });
            b.Board[(int)BB.Knights] = FENToBinary(FENBoard, new List<char>() { 'n', 'N' });
            b.Board[(int)BB.Bishops] = FENToBinary(FENBoard, new List<char>() { 'b', 'B' });
            b.Board[(int)BB.Rooks] = FENToBinary(FENBoard, new List<char>() { 'r', 'R' });
            b.Board[(int)BB.Queen] = FENToBinary(FENBoard, new List<char>() { 'q', 'Q' });
            b.Board[(int)BB.King] = FENToBinary(FENBoard, new List<char>() { 'k', 'K' });

            if (FENSplit[1] == "w") b.WhiteToPlay = true;
            else b.WhiteToPlay = false;

            b.castlingRights = FENToCastling(FENSplit[2]);

            if (FENSplit[3] != "-")
            {
                b.enPassantFile = SquareToFile(FENSplit[3]);
            }

            if (FENSplit[4] != "-")
            {
                b.plyCount = ushort.Parse(FENSplit[4]);
            }

            if (FENSplit[5] != "-")
            {
                b.moveCount = ushort.Parse(FENSplit[5]);
            }

            b.zobristKey = Zobrist.CalculateZobrist(b);

            return b;
        }

        /// <summary>
        /// Converts the given index of the square to the chess notation format (e1, g5, b3)
        /// </summary>
        /// <param name="index">the index of the square</param>
        /// <returns>The string format of a square</returns>
        public static string IndexToSquare(byte index)
        {
            byte file = BitBoardTools.CalculateFile(index);
            byte rank = BitBoardTools.CalculateRank(index);
            file += 96;

            return Convert.ToChar(file) + rank.ToString();

        }

        public static char IndexToFile(byte file)
        {
            file += 96;
            return Convert.ToChar(file);

        }

        public static ulong clearBit(byte index, ulong b)
        {
            return b & ~(1UL << index);
        }

        public static ushort clearCastleBit(ushort castling, ushort right)
        {
            return (ushort)(castling & ~right);
        }

        public static byte SquareToIndex(string square)
        {
            square = square.ToLower();

            char file = square[0];
            int intFile = file;
            intFile -= 96;

            char rank = square[1];
            int intRank = rank - 48;

            return (byte)((intRank - 1) * 8 + Math.Abs(intFile - 8));
        }

        public static byte SquareToFile(string square)
        {
            square = square.ToLower();
            char file = square[0];
            int intFile = file;
            return (byte)(intFile - 96);
        }

        internal static ushort FENToCastling(string rights)
        {
            ushort result = 0;
            if (rights == "-") return result;

            foreach (char i in rights)
            {
                switch (i)
                {

                    case 'K':
                        result += (ushort)Castling.KC;
                        break;

                    case 'Q':
                        result += (ushort)Castling.QC;
                        break;

                    case 'k':
                        result += (ushort)Castling.kc;
                        break;

                    case 'q':
                        result += (ushort)Castling.qc;
                        break;

                }

            }
            return result;
        }

        internal static ulong FENToBinary(string[] FEN, List<char> target)
        {
            string result = "";
            foreach (string row in FEN)
            {

                foreach (char c in row)
                {
                    if (target.Contains(c))
                    {
                        result += "1";
                    }
                    else if (int.TryParse(c.ToString(), out int a))
                    {
                        for (int i = 0; i < a; i++)
                        {
                            result += "0";
                        }
                    }
                    else result += "0";
                }
            }
            return (ulong)Convert.ToInt64(result, 2);
        }

        internal static string MoveToString(Move move)
        {
            string result = string.Empty;
            byte destination = move.destinationIndex;
            byte start = move.startIndex;

            result = $"{IndexToSquare(start) + IndexToSquare(destination)}";

            if (move.isPromotion)
            {
                switch (move.pieceIndex)
                {
                    case 0:
                        result += "n";
                        break;

                    case 1:
                        result += "b";
                        break;

                    case 2:
                        result += "r";
                        break;

                    case 3:
                        result += "q";
                        break;

                }
            }

            return result;
        }

        internal static string BoardToFEN(Bitboard b)
        {
            throw new NotImplementedException();
        }

    }
}
