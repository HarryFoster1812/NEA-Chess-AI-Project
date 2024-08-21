using System;

namespace Engine
{
    public static class Magic  // its magic, don't question it.
    {

        static ulong[][] RookMagic = new ulong[64][]; // the list of rook magic numbers for each square and blocker combinations
        static ulong[][] BishopMagic = new ulong[64][]; // the list of bishop magic numbers for each square and blocker combinations

        /*
         Piece maks are the psuedo legal moves that they can move to in an empty board without the edges

         e.g
         The pseudo legal moves that a rook on e4 are:
        ....1...
		....1...
		....1...
		....1...
		1111.111
		....1...
		....1...
		....1...

        minus the edges:
        ........
        ....1...
        ....1...
        ....1...
        .111.11.
        ....1...
        ....1...
        ........

        This however is not always the case as the mask for a rook on e1 is:
        ........
        ....1...
        ....1...
        ....1...
        ....1...
        ....1...
        ....1...
        .111.11.
        
         */

        static ulong[] rookMasks = new ulong[64]; // the mask for each rooks square
        static ulong[] bishopMasks = new ulong[64]; // the mask for each bishop square

        internal static Random r = new Random(); // used for the production of magic numbers

        public static ulong[,] directions = new ulong[64, 8];

        #region RelevencyBits

        /*
         Relevency bits are the number of bits that can be changed when looping through each of the blockers

         for a rook on e2 the rook mask would be:
        ........
		.....1..
		.....1..
		.....1..
		.....1..
		.....1..
		.1111.1.
        ........

        if you count the number of 1's it is 10, thus there are only 10 relevent bits
        The 1's reprensent a location where a blocker could possibly be

         */

        static byte[] rookRelevencyBits = {
            12, 11, 11, 11, 11, 11, 11, 12,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            12, 11, 11, 11, 11, 11, 11, 12
        };

        static byte[] bishopRelevencyBits = {
          6, 5, 5, 5, 5, 5, 5, 6,
          5, 5, 5, 5, 5, 5, 5, 5,
          5, 5, 7, 7, 7, 7, 5, 5,
          5, 5, 7, 9, 9, 7, 5, 5,
          5, 5, 7, 9, 9, 7, 5, 5,
          5, 5, 7, 7, 7, 7, 5, 5,
          5, 5, 5, 5, 5, 5, 5, 5,
          6, 5, 5, 5, 5, 5, 5, 6
        };

        #endregion

        #region Pre-Generated Magic numbers
        static readonly ulong[] RookMagicNums = {
            36029072039489824,
            5782624189288546370,
            144150443348476040 ,
            4683778865573660676,
            144119895662026896 ,
            720580411708801032 ,
            216211265088365056 ,
            4755801546350985732,
            9799973544901223808,
            2324068515031552000,
            140806277038208    ,
            162411130282115104 ,
            3754594787358408710,
            140823387832448    ,
            1189513286240960772,
            281477196611602    ,
            44530225123969     ,
            1157427578673315840,
            621497849179283536 ,
            11601292431886172168,
            37155246891337728   ,
            397583954963595776  ,
            576465150637377552  ,
            4613940017272865028 ,
            2900318510066468896 ,
            2305878194659577856 ,
            18150536118304      ,
            1153361313555028225 ,
            19703274141651112   ,
            653094515883901056  ,
            6751691217168       ,
            369311670709030980  ,
            1154117780077806240 ,
            1373741647501729792 ,
            900794829720523008  ,
            1225559651530057728 ,
            9224075758664680448 ,
            99219972248568832   ,
            18316253173125128   ,
            4908924366961508417 ,
            1873567815880966145 ,
            616219780227153920  ,
            2331102106812432    ,
            2533343512035336    ,
            2823546145406981    ,
            1750211442500567044 ,
            5637231283011718    ,
            4906127683962929156 ,
            5116165043808895232 ,
            9153435374985472    ,
            1450194285995507968 ,
            288802132936622976  ,
            9574547388924032    ,
            1243556516444242432 ,
            4612248973347127808 ,
            9232471631597732352 ,
            10204586778300706   ,
            9809403019975098658 ,
            1198133973571207186 ,
            2359921408581042433 ,
            9548194504113408002 ,
            3891391638924298257 ,
            9144254366212       ,
            288321645490470946

        };

        public static readonly ulong[] BishopMagicNums = {
            2670656032459719233,
            2891315681645586954,
            4505188834541572,
            582094694981304320,
            5188446662528073728,
            5476659758951833600,
            6918093096971688194,
            563096120984576,
            18119990850917380,
            18023229100673088,
            576759823794708736,
            36068396625829888,
            9965397868544,
            9223374240514839812,
            36734143124473090,
            23645564508325392,
            291610412402607616,
            4505835431592136,
            2328501779207328257,
            4902590578690113540,
            598460753510536,
            9301129542138634248,
            9224499128112910336,
            281486805173504,
            2332972359193216000,
            658125913573296136,
            114349477740616,
            1134704656978960,
            180707005948776448,
            4508684885460995,
            9223533118596712448,
            865818729185837440,
            1157471421976316928,
            1155340469960443400,
            106824429732480,
            10160125161842671872,
            18015502316376128,
            3467780511315837128,
            76562456385749632,
            9514421561491260048,
            2559697529946114,
            81636612491969792,
            9278260208161542144,
            9296566535915045376,
            15904994243379712,
            8215137535106482336,
            73342115827941440,
            1732199222897410080,
            5633108415096832,
            4621001639552225296,
            45054277870157904,
            2260905690139656,
            72198957014586500,
            2882339633118220288,
            604643580409151488,
            9016549402771488,
            563505213735936,
            9304439037794321152,
            9223460000478007296,
            10746070016,
            2453617385946534144,
            4954050875412185600,
            40547859336069376,
            74311661598670976,

        };
        #endregion

        /*
         Rook mask

		........
		.....1..
		.....1..
		.....1..
		.....1..
		.....1..
		.....1..
		.1111.1.
         */

        static ulong genrookMask(ulong rook)
        {  // creates the rook attack mask
            byte file = BitBoardTools.CalculateFile(rook);

            byte rank = BitBoardTools.CalculateRank(rook);

            ulong attack = 0;

            attack = ((BitBoardTools.AFile >> file - 1) & ((~BitBoardTools.Rank1) & ~(BitBoardTools.Rank8))) | ((BitBoardTools.Rank1 << (8 * (rank - 1))) & (~BitBoardTools.AFile & ~(BitBoardTools.HFile)));
            attack &= ~rook;
            return attack;

        }

        /*
		........
		........
		........
		.1......
		..1.....
		...1....
		....1.1.
		........
		*/

        static ulong bishopNW(ulong bishop)
        {
            // Calculate Bishop north west

            ulong attack = bishop;

            const ulong pr1 = BitBoardTools.notHFile & (BitBoardTools.notHFile << 9);
            const ulong pr2 = pr1 & (pr1 << 18);

            attack |= BitBoardTools.notHFile & (bishop << 9);

            attack |= pr1 & (attack << 18);

            attack |= pr2 & (attack << 36);

            return attack ^ bishop;
        }


        static ulong bishopNE(ulong bishop)
        {
            // Calculate Bishop north west

            ulong attack = bishop;

            const ulong pr1 = BitBoardTools.notAFile & (BitBoardTools.notAFile << 7);
            const ulong pr2 = pr1 & (pr1 << 14);

            attack |= BitBoardTools.notAFile & (bishop << 7);

            attack |= pr1 & (attack << 14);

            attack |= pr2 & (attack << 21);

            return attack ^ bishop;
        }

        static ulong bishopSW(ulong bishop)
        {
            // Calculate Bishop north west

            ulong attack = bishop;

            const ulong pr1 = BitBoardTools.notHFile & (BitBoardTools.notHFile >> 7);
            const ulong pr2 = pr1 & (pr1 >> 14);

            attack |= BitBoardTools.notHFile & (bishop >> 7);

            attack |= pr1 & (attack >> 14);

            attack |= pr2 & (attack >> 21);

            return attack ^ bishop;
        }

        static ulong bishopSE(ulong bishop)
        {
            // Calculate Bishop north west

            ulong attack = bishop;

            const ulong pr1 = BitBoardTools.notAFile & (BitBoardTools.notAFile >> 9);
            const ulong pr2 = pr1 & (pr1 >> 18);

            attack |= BitBoardTools.notAFile & (bishop >> 9);

            attack |= pr1 & (attack >> 18);

            attack |= pr2 & (attack >> 36);

            return attack ^ bishop;
        }

        static ulong genbishopMask(ulong bishop)
        {
            return (bishopNW(bishop) | bishopNE(bishop) | bishopSW(bishop) | bishopSE(bishop)) & (~BitBoardTools.Edges);
        } // creates the bishop attack mask

        // Given the mask and the occupancy we need to calculate the attacks

        /*
                   ........                 ........            ........
	        	   .....1..                 .....1..            ........
	        	   .....1..                 .....1..            .....1..
	        mask = .....1..    occupancy =  ........   attack = .....1..
	        	   .....1..                 ........            .....1..
	        	   .....1..                 ........            .....1..
	        	   .....1..                 ........            .....1..
	        	   .1111.1.                 ..1.1...            ....1.1.
         */

        /// <summary>
        /// Perform a forward fill on the given start postion and stops when the edge or a blocker is hit
        /// </summary>
        /// <param name="Pos">The start bitboard</param>
        /// <param name="blocker">The bitboard of the blocker which might obstruct the fill</param>
        /// <param name="edgemask">The edge of the board</param>
        /// <param name="magnitude">The direction (1 west, 8 north, 7NE, 9NW)</param>
        /// <returns>The resultant fill bitboard</returns>
        public static ulong ForwardFill(ulong Pos, ulong blocker, ulong edgemask, byte magnitude)
        {
            ulong attack = 0;
            byte inc = 1;
            bool flag = false;
            do
            {
                ulong temp = Pos << (magnitude * inc);
                if ((temp & edgemask) > 0 || (temp & blocker) > 0) flag = true;
                attack |= temp;
                inc++;

            } while (!flag);

            return attack;
        }

        /// <summary>
        /// Perform a backwards fill on the given start postion and stops when the edge or a blocker is hit
        /// </summary>
        /// <param name="Pos">The start bitboard</param>
        /// <param name="blocker">The bitboard of the blocker which might obstruct the fill</param>
        /// <param name="edgemask">The edge of the board</param>
        /// <param name="magnitude">The direction (1 east, 8 south, 7 SW, 9 SE)</param>
        /// <returns>The resultant fill bitboard</returns>
        public static ulong BackwardFill(ulong Pos, ulong blocker, ulong edgemask, byte magnitude)
        {
            ulong attack = 0; // the bitboard generated from the fill
            byte inc = 1;
            bool flag = false;
            do
            {

                ulong temp = Pos >> (magnitude * inc);

                // check is we have hit the edge or a blocker
                if ((temp & edgemask) > 0 || (temp & blocker) > 0 || temp <= 0) flag = true; // stop the fill

                attack |= temp;
                inc++;

            } while (!flag);

            return attack;
        }

        static ulong genRookAttack(ulong rookPos, ulong blockers)
        {
            // bitscan in a certain direction until we hit a blocker
            ulong attack = 0;
            byte file = BitBoardTools.CalculateFile(rookPos);
            byte rank = BitBoardTools.CalculateRank(rookPos);

            // NorthFill +8 (8th rank)
            if (rank != 8)
            {
                attack |= ForwardFill(rookPos, blockers, BitBoardTools.Rank8, 8);
            }

            if (rank != 1)
            {
                // SouthFill -8 (1st rank) 
                attack |= BackwardFill(rookPos, blockers, BitBoardTools.Rank1, 8);
            }

            if (file != 1)
            {
                // WestFill forward +1 (A file)
                attack |= ForwardFill(rookPos, blockers, BitBoardTools.AFile, 1);
            }

            if (file != 8)
            {
                // EastFill -1 H File
                attack |= BackwardFill(rookPos, blockers, BitBoardTools.HFile, 1);
            }

            return attack;
        }

        static ulong genBishopAttack(ulong bishopPos, ulong blockers)
        {
            // bitscan in a certain direction until we hit a blocker
            ulong attack = 0;
            byte file = BitBoardTools.CalculateFile(bishopPos);
            byte rank = BitBoardTools.CalculateRank(bishopPos);

            if (rank != 8 && file != 8)
            {
                // NorthEastFill forward 7 
                attack |= ForwardFill(bishopPos, blockers, BitBoardTools.Edges, 7);
            }

            if (file != 8 && rank != 1) // if the bishop is on the HFile there is no need to generate the East fills
            {
                // SouthEastFill backwards 9
                attack |= BackwardFill(bishopPos, blockers, BitBoardTools.Edges, 9);
            }

            if (file != 1 && rank != 1)
            {
                // SouthWestFill backwards 7 
                attack |= BackwardFill(bishopPos, blockers, BitBoardTools.Edges, 7);
            }

            if (file != 1 && rank != 8)
            { // if the bishop is on the AFile there is no need to generate the West fills
                // NorthWestFill forward 9
                attack |= ForwardFill(bishopPos, blockers, BitBoardTools.Edges, 9);
            }


            return attack;
        }

        // each magic corresponds to one square and acts as a hashing function to create a unique index for each blocking pattern
        // Generate a magic number, hash every blocker posibility and then assign a index to each
        // if there is a collision then the magic number is not magic and we need to regenerate a new one and clear the table
        // To reduce the size of the magic table we only use 2^(relevent bits)
        // for the size of the look up array; that is why jagged arrays are used
        // This will take more computation but once we have all the magic numbers we dont need to regenerate them just use them

        public static bool tryGenTable(ulong magic, byte relevancy, ulong mask, byte index, ref ulong[][] table, bool isRook)
        {
            // try each variation for a given mask

            ulong occ = 0;
            ulong Pos = BitBoardTools.IndexToBitboard(index);

            do
            {
                // calulate magic index
                int magicIndex = (int)((occ * magic) >> (64 - relevancy));

                // calculate attacks
                ulong attacks;

                if (isRook)
                {
                    attacks = genRookAttack(Pos, occ);
                }

                else
                {
                    attacks = genBishopAttack(Pos, occ);
                }

                // check if the index is negative
                if (magicIndex < 0)
                {
                    Array.Clear(table[index], 0, table[index].Length);
                    return false;
                }

                else if (table[index][magicIndex] == 0)
                {
                    table[index][magicIndex] = attacks;
                }

                // Check for positve collision (different magic numbers points to the same attacking bitboard)
                else if (table[index][magicIndex] != attacks)
                {
                    Array.Clear(table[index], 0, table[index].Length);
                    return false;
                }

                occ = (occ - mask) & mask;

            } while (occ > 0);

            return true;
        }

        public static ulong getRookBlockerAttacks(ulong pos, ulong blockers)
        {
            int index = BitBoardTools.BitboardToIndex(pos);
            ulong magic = RookMagicNums[index];
            byte relevancy = rookRelevencyBits[index];
            blockers = blockers & rookMasks[index];
            int magicIndex = (int)((blockers * magic) >> (64 - relevancy));

            return RookMagic[index][magicIndex];

        }

        public static ulong getBishopBlockerAttacks(ulong pos, ulong blockers)
        {
            int index = BitBoardTools.BitboardToIndex(pos);
            ulong magic = BishopMagicNums[index];
            byte relevancy = bishopRelevencyBits[index];
            blockers = blockers & bishopMasks[index];
            int magicIndex = (int)((blockers * magic) >> (64 - relevancy));
            return BishopMagic[index][magicIndex];
        }

        public static ulong getRookBlockerAttacks(byte index, ulong blockers) //overload for index
        {
            ulong magic = RookMagicNums[index];
            byte relevancy = rookRelevencyBits[index];
            blockers = blockers & rookMasks[index];
            int magicIndex = (int)((blockers * magic) >> (64 - relevancy));
            return RookMagic[index][magicIndex];
        }

        public static ulong getBishopBlockerAttacks(byte index, ulong blockers) //overload for index
        {
            ulong magic = BishopMagicNums[index];
            byte relevancy = bishopRelevencyBits[index];
            blockers = blockers & bishopMasks[index];
            int magicIndex = (int)((blockers * magic) >> (64 - relevancy));
            return BishopMagic[index][magicIndex];
        }

        public static ulong findPossibleMagic()
        {
            return (genRandom64() & genRandom64() & genRandom64());
        }

        public static ulong genRandom64()
        {
            ulong u1, u2, u3, u4;
            u1 = (ulong)(r.Next()) & 0xFFFF;
            u2 = (ulong)(r.Next()) & 0xFFFF;
            u3 = (ulong)(r.Next()) & 0xFFFF;
            u4 = (ulong)(r.Next()) & 0xFFFF;

            return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
        }

        static Magic()
        {

            bool flag = false;

            // set up the direction maks
            for (byte i = 0; i < 64; i++) // loop over each square
            { // {N,E,S,W,NE,SE,SW,NW}

                ulong loc = BitBoardTools.IndexToBitboard(i);
                byte file = BitBoardTools.CalculateFile(loc);
                byte rank = BitBoardTools.CalculateRank(loc);

                if (rank != 8)
                {
                    directions[i, 0] = ForwardFill(loc, 0, BitBoardTools.Rank8, 8);
                }
                if (rank != 1)
                {
                    // SouthFill -8 (1st rank) 
                    directions[i, 2] = BackwardFill(loc, 0, BitBoardTools.Rank1, 8);
                }
                if (file != 1)
                {
                    // WestFill forward +1 (A file)
                    directions[i, 3] = ForwardFill(loc, 0, BitBoardTools.AFile, 1);
                }
                if (file != 8)
                {
                    // EastFill -1 H File
                    directions[i, 1] = BackwardFill(loc, 0, BitBoardTools.HFile, 1);
                }


                // NorthEastFill forwards 7 HFile || Rank8 

                if (rank != 8 && file != 8)
                {
                    // NorthEastFill forward 7 
                    directions[i, 4] = ForwardFill(loc, 0, BitBoardTools.Edges, 7);
                }

                if (file != 8 && rank != 1) // if the bishop is on the HFile there is no need to generate the East fills
                {
                    // SouthEastFill backwards 9
                    directions[i, 5] = BackwardFill(loc, 0, BitBoardTools.Edges, 9);
                }

                if (file != 1 && rank != 1)
                {
                    // SouthWestFill backwards 7 
                    directions[i, 6] = BackwardFill(loc, 0, BitBoardTools.Edges, 7);
                }

                if (file != 1 && rank != 8)
                { // if the bishop is on the AFile there is no need to generate the West fills
                  // NorthWestFill forward 9
                    directions[i, 7] = ForwardFill(loc, 0, BitBoardTools.Edges, 9);
                }
            }

            // ################ GEN MAGIC TABLES ##########################

            // Bishops
            for (int i = 0; i < 64; i++) // repeat for each square
            {
                // ulong magic; // if you want to generate new magic numbers
                ulong magic = BishopMagicNums[i];
                ulong mask = genbishopMask(BitBoardTools.IndexToBitboard((byte)i));
                bishopMasks[i] = mask;
                byte relevancy = bishopRelevencyBits[i];
                BishopMagic[i] = new ulong[BitBoardTools.IndexToBitboard(relevancy)];

                do
                {
                    // magic = findPossibleMagic(); // if you want to generate new magic numbers
                    flag = tryGenTable(magic, relevancy, mask, (byte)i, ref BishopMagic, false);
                } while (!flag);

                BishopMagicNums[i] = magic;
            }

            // Rooks
            for (int i = 0; i < 64; i++) // repeat for each square for rooks
            {
                // ulong magic; // if you want to generate new magic numbers
                ulong magic = RookMagicNums[i];
                ulong mask = genrookMask(BitBoardTools.IndexToBitboard((byte)i));
                rookMasks[i] = mask;
                byte relevancy = rookRelevencyBits[i];
                RookMagic[i] = new ulong[BitBoardTools.IndexToBitboard(relevancy)];

                do
                {
                    // magic = findPossibleMagic(); // if you want to generate new magic numbers
                    flag = tryGenTable(magic, relevancy, mask, (byte)i, ref RookMagic, true);
                } while (!flag);

                RookMagicNums[i] = magic;
            }
        }
    }
}
