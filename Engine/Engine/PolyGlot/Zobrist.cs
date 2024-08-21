using System;
using System.IO;

namespace Engine.Representaion
{

    /*
        Hashing Values and ployglot hashing algorithm rules from: http://hgm.nubati.net/book_format.html
     */

    [Serializable]
    public struct ZobristKey // a zobrsit key can be split up into 4 components
    {
        public ulong pieceKey;
        public ulong enPassantKey;
        public ulong castlingKey;
        public ulong moveKey;
        public ulong Key => (pieceKey ^ enPassantKey ^ castlingKey ^ moveKey); // a function as the key should be changed via its components

        public ZobristKey(bool a = false)  // bool a is a wasted value as parameterless constructors are not supported
        {
            pieceKey = 0;
            enPassantKey = 0;
            castlingKey = 0;
            moveKey = 0;
        }

    }

    internal static class Zobrist
    {
        /*
         zobristTables = 
        {
        0 - white pawn
        1 - black pawn

        2 - white knight
        3 - black knight

        4 - white bishop
        5 - black bishop

        6 - white rook
        7 - black rook

        8 - white queen
        9 - black queen

        10 - white king
        11 - black king

        12 - Castling

        13 - enPassant
        14 - SideToMove
        }
         */


        public static ulong[][] zobristTables = new ulong[15][];


        public static ulong[] zobristCastlingTable = new ulong[4];

        // each ulong represent the file that will contribute to the hash 0 (A file), 7 (H file)
        public static ulong[] zobristEnpassantFile = new ulong[8];


        // because of the way i have chosen to represent the indices of my bitboard they need to be swapped to be compatible with the polyglot values
        public static int[] indexTable = {
            7,   6,   5,   4,   3,  2,   1,   0,
            15,  14,  13,  12,  11,  10,  9,  8,
            23,  22,  21,  20,  19,  18,  17,  16,
            31,  30,  29,  28,  27,  26,  25,  24,
            39,  38,  37,  36,  35,  34,  33,  32,
            47,  46,  45,  44,  43,  42,  41,  40,
            55,  54,  53,  52,  51,  50,  49,  48,
            63,  62,  61,  60,  59,  58,  57,  56
        };

        public static ulong blackToMove;


        public static void changePieceSquare(ushort index, int pieceType, ref ulong key)
        {
            // convert the index to one that is used in the hashtable
            int zobristIndex = indexTable[index];
            // xor the key with the looked up value
            key ^= zobristTables[pieceType][zobristIndex];
        }


        public static ZobristKey CalculateZobrist(Bitboard board)
        {
            ZobristKey key = new ZobristKey(true);
            ulong PieceBB = 0;
            int turnAdder = board.WhiteToPlay ? 0 : 1;
            int counter = 0;
            for (int i = 2; i < board.Board.Length; i++) // for each piece bitboard i (2 is the index of the pawn bitboard)
            {
                for (int j = 0; j < 2; j++) // loop over each colour
                {
                    PieceBB = board.Board[i] & board.Board[j]; // gets the bitboard of the piece colours (white pawns, black pawns ect.)
                    while (PieceBB != 0) // loops over each piece
                    {
                        ulong piece = BitBoardTools.popLSB(ref PieceBB); // get the LSB
                        int index = BitBoardTools.BitboardToIndex(piece);

                        changePieceSquare((ushort)index, counter, ref key.pieceKey); // add the hash
                    }
                    counter++;
                }
            }

            if (board.enPassantFile != -1)
            {
                int index = board.enPassantFile - 1;
                // if the squares directly next to the pushed pawn are enemy pawns then apply the hash
                int file = 7 - index;
                int pieceindex;

                if (turnAdder == 1)
                {
                    pieceindex = 2 * 8 + file;
                }

                else
                {
                    pieceindex = 5 * 8 + file;
                }
                ulong pawnloc = BitBoardTools.IndexToBitboard((byte)pieceindex);
                ulong ajecentMask = BitBoardTools.getPawnAttacks(pawnloc, turnAdder ^ 1);
                ulong enemyPawns = Bitboard.getPawns(board.Board, turnAdder);
                if ((ajecentMask & enemyPawns) > 0)
                {
                    key.enPassantKey ^= zobristTables[13][index];
                }
            }

            // calculate Castling Hash
            ulong tempCastling = board.castlingRights;
            while (tempCastling != 0)
            {
                ulong piece = BitBoardTools.popLSB(ref tempCastling);
                int index = BitBoardTools.BitboardToIndex(piece);
                key.castlingKey ^= zobristTables[12][index];
            }

            if (board.WhiteToPlay)
            {
                key.moveKey ^= zobristTables[14][0];
            }

            return key;
        }


        static Zobrist()
        {
            // initilise piece tables
            string loc = Directory.GetParent(Directory.GetCurrentDirectory()).FullName.Split(new string[] { "UI", "Engine" }, StringSplitOptions.None)[0];
            loc = Path.Combine(loc, "Engine", "Engine", "PolyGlot", "PolyZobristKeys.txt");

            zobristTables[12] = new ulong[4];
            zobristTables[13] = new ulong[8];
            zobristTables[14] = new ulong[1];


            string fileText = File.ReadAllText(loc);
            string[] keys = fileText.Split(',');
            int piecetableCounter = -1;

            // loop over each key
            for (int i = 0; i < keys.Length; i++)
            {
                string temp = keys[i].Replace("\n", "").Trim();
                ulong key = (ulong)Convert.ToUInt64(temp, 16);
                if (i > 767) // in enpassant or castling
                {
                    if (i < 772)
                    { // in castling
                        zobristTables[12][i - 768] = key;
                    }
                    else if (i < 780)
                    {
                        // in enpassnt
                        zobristTables[13][i - 772] = key;
                    }
                    else
                    {
                        // black hash
                        zobristTables[14][0] = key;
                    }
                }

                else
                { // it is a piece hash

                    if (i % 64 == 0)
                    { // it is at the start of a new piece
                        // hash is in the format Pawn black, Pawn white, so convert that into white, black

                        if (piecetableCounter % 2 == 0)
                        {
                            piecetableCounter += 3;

                        }

                        else if (piecetableCounter == -1)
                        {
                            piecetableCounter += 2;
                        }
                        else
                        {
                            piecetableCounter--;
                        }
                        zobristTables[piecetableCounter] = new ulong[64];

                    }

                    zobristTables[piecetableCounter][i % 64] = key;
                }

            }
        }
    }
}
