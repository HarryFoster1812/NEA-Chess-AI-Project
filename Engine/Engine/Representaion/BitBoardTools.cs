using System;
using System.Collections.Generic;

namespace Engine
{
    public static class BitBoardTools
    {
        public const ulong AFile = 0b10000000_10000000_10000000_10000000_10000000_10000000_10000000_10000000;
        public const ulong HFile = AFile >> 7;

        public const ulong Rank1 = 0b11111111;
        public const ulong Rank8 = Rank1 << (8 * 7);
        public const ulong Edges = AFile | HFile | Rank1 | Rank8;
        public const ulong notAFile = ~AFile; // 0b01111111_01111111_01111111_01111111_01111111_01111111_01111111_01111111
        public const ulong notHFile = ~(AFile >> 7);
        public const ulong notABFile = ~AFile & ~(AFile >> 1);
        public const ulong notGHFile = ~(AFile >> 6) & ~(AFile >> 7);

        public static readonly ulong[,] CastlingMasks = { { 6, 432345564227567616 }, { 112, 8070450532247928832 } };

        public static readonly byte[] DirectionMagnitudes = { 8, 1, 8, 1, 7, 9, 7, 9 };// {N,E,S,W,NE,SE,SW,NW}
        public static readonly ulong[] EdgeDirectionMask = { Rank8, HFile, Rank1, AFile, Edges, Edges, Edges, Edges };// {N,E,S,W,NE,SE,SW,NW}

        public static readonly ulong[] usefulPawnRanks = { Rank8, Rank1, Rank1 << 8, Rank8 >> 8, Rank8 >> 8 * 3, Rank1 << 8 * 3 }; // white promotion, black promotion, white start, black start, white EnPassant, BlackEnPassant
        public static readonly ulong[] wPawnAttacks;
        public static readonly ulong[] bPawnAttacks;
        public static readonly ulong[] KnightAttacks;
        public static readonly ulong[] KingAttacks;

        public static readonly Dictionary<ulong, byte> BitboardIndexDictionary = new Dictionary<ulong, byte>();


        public static byte CalculateFile(ulong position)
        {
            byte index = BitboardToIndex(position);

            return (byte)Math.Abs((index % 8) - 8);
        }

        public static byte CalculateRank(ulong position)
        {
            byte index = BitboardToIndex(position);
            return (byte)((index / 8) + 1);
        }

        public static byte CalculateFile(byte position)
        {
            return (byte)Math.Abs((position % 8) - 8);
        }

        public static byte CalculateRank(byte position)
        {
            return (byte)((position / 8) + 1);
        }

        internal static ulong wPawnWestAttacks(ulong whitePawns)
        {
            return (whitePawns << 9) & notHFile;
        }

        internal static ulong wPawnEastAttacks(ulong whitePawns)
        {
            return (whitePawns << 7) & notAFile;

        }

        internal static ulong bPawnWestAttacks(ulong blackPawns)
        {
            return (blackPawns >> 7) & notHFile;

        }

        internal static ulong bPawnEastAttacks(ulong blackPawns)
        {
            return (blackPawns >> 9) & notAFile;

        }

        public static ulong pawnAttacks(ulong pawns, int WhiteToPlay)
        {

            if (WhiteToPlay == 0) return (wPawnWestAttacks(pawns) | wPawnEastAttacks(pawns));

            else return (bPawnWestAttacks(pawns) | bPawnEastAttacks(pawns));
        }
        public static ulong pawnWestAttacks(ulong pawns, int WhiteToPlay)
        {

            if (WhiteToPlay == 0) return wPawnWestAttacks(pawns);

            else return bPawnWestAttacks(pawns);
        }

        public static ulong pawnEastAttacks(ulong pawns, int WhiteToPlay)
        {

            if (WhiteToPlay == 0) return wPawnEastAttacks(pawns);

            else return bPawnEastAttacks(pawns);
        }
        public static ulong getPawnAttacks(ulong pawns, int WhiteToPlay)
        {
            int index = BitboardToIndex(pawns);
            if (WhiteToPlay == 0) return wPawnAttacks[index];
            return bPawnAttacks[index];
        }

        public static ulong pushPawn(ulong start, int WhiteToPlay)
        {
            if (WhiteToPlay == 1)
            {
                return start >> 8;
            }
            return start << 8;
        }

        // returns the least significant bit
        public static ulong LSB(ulong x)
        {
            return x &= ~x + 1;
        }

        public static ulong popLSB(ref ulong x)
        {
            ulong lsb = x & ~x + 1;
            x ^= lsb;
            return lsb;
        }

        // converts the given index 0-63 into the relevent bitboard
        // eg index 2 would give
        // 00000000
        // 00000000
        // 00000000
        // 00000000
        // 00000000
        // 00000000
        // 00000000
        // 00000100 this would represent the square f1
        public static ulong IndexToBitboard(Byte index)
        {
            return 1UL << (index);
        }

        public static byte BitboardToIndex(ulong a)
        {
            return BitboardIndexDictionary[a];
        }

        public static byte CalculateBitboardToIndex(ulong a)
        {
            byte b = 0;
            while (a > 1)
            {
                a >>= 1;
                b++;
            }
            return b;
        }

        // Generate the knight attack mask for any given knight bitboard
        public static ulong knightAttacks(ulong knight)
        {
            ulong attacks = 0UL;
            attacks |= (knight << 17) & notHFile;  // 2 up 1 right
            attacks |= (knight >> 15) & notHFile;  // 2 up 1 left
            attacks |= (knight >> 17) & notAFile;  // 2 down 1 right
            attacks |= (knight << 15) & notAFile;  // 2 down 1 left


            attacks |= (knight >> 10) & notABFile; // 2 right 1  down
            attacks |= (knight << 6) & notABFile;  // 2 right 1 up
            attacks |= (knight >> 6) & notGHFile;  // 2 left 1 down
            attacks |= (knight << 10) & notGHFile; // 2 left 1 up

            return attacks;
        }

        public static ulong genkingAttacks(ulong king)
        {
            ulong attacks = 0UL;
            attacks |= (king >> 8);
            attacks |= (king << 8);

            attacks |= (king >> 9) & notAFile;
            attacks |= (king >> 7) & notHFile;
            attacks |= (king << 9) & notHFile;
            attacks |= (king << 7) & notAFile;


            attacks |= (king >> 1) & notAFile;
            attacks |= (king << 1) & notHFile;


            return attacks;
        }

        static BitBoardTools()
        {
            wPawnAttacks = new ulong[64];
            bPawnAttacks = new ulong[64];
            KnightAttacks = new ulong[64];
            KingAttacks = new ulong[64];

            for (byte i = 0; i < 64; i++)
            { // sets up the attack bitboards
                ulong b = IndexToBitboard(i);
                wPawnAttacks[i] = pawnAttacks(b, 0);
                bPawnAttacks[i] = pawnAttacks(b, 1);
                KnightAttacks[i] = knightAttacks(b);
                KingAttacks[i] = genkingAttacks(b);
                BitboardIndexDictionary.Add(b, i);
            }



        }

    }
}
