using System;
using System.IO;

namespace Engine.PolyGlot
{

    /// <summary>
    /// A static class which (should) handle any polyglot chess book
    /// </summary>
    /// Note: This code reads the entire book into memory so do not use very large books
    /// The polyglot format is open source and under the GPL License.
    internal static class Polyglot
    {
        private static string bookPath; // the current path of the book which is being read
        private static PolyEntry[] entries; //the array of all of the opening book entries
        private static Random randomGen = new Random();

        public static string BookPath
        {
            get { return bookPath; }
            set
            {
                // gets the dynamic path of the openings folder
                string root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName.Split(new string[] { "UI", "Engine" }, StringSplitOptions.None)[0];
                // gets the user input name
                root = Path.Combine(root, "Openings", $"{value}.bin");

                if (File.Exists(root))
                {
                    bookPath = root;
                    readBook();
                }
                else
                {
                    throw new Exception("Book name was not found");
                }
            }
        }
        #region Endian Swaps


        //Endian Swap algorithms found on:https://stackoverflow.com/questions/19560436/bitwise-endian-swap-for-various-types


        static ulong EndianSwapU64(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);
            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);

        }

        static uint EndianSwapU32(uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        static ushort EndianSwapU16(ushort x)
        {
            x = (ushort)(
                (x >> 8) |
                (x << 8));
            return x;
        }
        #endregion

        /// <summary>
        /// Opens the current value of bookPath and tries to create the opening book
        /// </summary>
        static void readBook()
        {
            byte[] bytes = File.ReadAllBytes(bookPath);

            // each entry is 16 bytes long
            int numEntries = bytes.Length / 16;

            // create a new opening book array
            entries = new PolyEntry[numEntries];

            int i = 0;

            for (int j = 0; j < numEntries; j++)
            {
                // first 8 bytes are the key
                // 2 bytes for the move
                // 2 bytes for the weight 
                // 4 bytes for the learn
                // the infomation is in the big endian format so it needs to be converted.

                ulong key = BitConverter.ToUInt64(bytes, i);
                key = EndianSwapU64(key);

                ushort move = BitConverter.ToUInt16(bytes, i + 8);
                move = EndianSwapU16(move);

                ushort weight = BitConverter.ToUInt16(bytes, i + 10);
                weight = EndianSwapU16(weight);

                uint learn = BitConverter.ToUInt32(bytes, i + 12);

                // store the information
                entries[i / 16] = new PolyEntry(key, move, weight, learn);

                // increment the index by 16 bytes
                i += 16;
            }
        }

        /// <summary>
        /// returns the index of the first element in the opening book which is associated with the given key
        /// </summary>
        /// <param name="startindex">The index of the first found entry</param>
        /// <param name="key">The key of the position to lookup</param>
        /// <returns></returns>
        static int findStartIndex(int startindex, ulong key)
        {
            bool stop = false;

            do
            {
                // check if the index is outside the bounds of the array
                if (startindex < 0)
                {
                    stop = true;
                    startindex = 0;
                }
                // check if the entry has the desired key
                else if (entries[startindex].key == key)
                {
                    startindex--;
                }
                // the element does not so the element after this must be the start of the instances
                else
                {
                    stop = true;
                    startindex++;
                }
            } while (!stop);
            return startindex;
        }

        /// <summary>
        /// Finds the range of the entries with the desired key
        /// </summary>
        /// <param name="startIndex">The index which is where the entries start</param>
        /// <param name="key">The desired key of the position</param>
        /// <returns>The number of entries with the desired key</returns>
        static int findRange(int startIndex, ulong key)
        {
            int counter = 0;
            // loop through until the key does not match
            for (int i = startIndex; i < entries.Length; i++)
            {
                // check if the key matches
                if (entries[i].key == key)
                {
                    counter++;
                }

                else
                {
                    // it does not so we can return
                    return counter;
                }
            }
            return counter;
        }

        /// <summary>
        /// Loops through at the start index and outputs the entry information
        /// </summary>
        /// <param name="Startindex">The index which is where the entries start</param>
        /// <param name="key">The desired key of the position</param>
        static void listBook(int Startindex, ulong key)
        {
            // find true startpoint

            Startindex = findStartIndex(Startindex, key);

            for (int i = Startindex; i < entries.Length; i++)
            {
                if (entries[i].key == key)
                {
                    // get the move information
                    PolyEntry move = entries[i];

                    // convert it to the move struct
                    int fromLoc = 8 * move.startRow + (7 - move.startFile);
                    int toLoc = 8 * move.destinationRow + (7 - move.destinationFile);
                    ushort promotion = (ushort)move.pieceIndex;

                    Move temp = new Move((byte)fromLoc, (byte)toLoc, promotion);

                    // output the information
                    Console.WriteLine($"bookinfo weight {move.weight} move {Tools.MoveToString(temp)}");

                    if (CommandHandler.Debug)
                    {
                        Console.Write($"Key: {move.key.ToString("X")}   ");
                        Console.Write($"Move: {Tools.MoveToString(temp)}   ");
                        Console.Write($"Index: {i}");
                        Console.Write($"weight {move.weight}\n");
                    }
                }
                // if the element if not the desired key we can just return
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Performs a recursive binary search to find an element with the desired key in the entry array
        /// </summary>
        /// <param name="left">The leftmost index of the current iteration (when calling it is 0)</param>
        /// <param name="right">The rightmost index of the current iteration (when calling it is the last index of the array)</param>
        /// <param name="desiredKey"></param>
        /// <returns>The index of the element or -1 if one is not found</returns>
        static int binarySearch(int left, int right, ulong desiredKey)
        {
            //
            if (right >= left)
            {
                // find the middle of the array
                int mid = left + (right - left) / 2;

                // check if the middle is the desired element
                if (entries[mid].key == desiredKey)
                    return mid;
                // if the probed key is higher then we know that all the elements are irrelavent
                if (entries[mid].key > desiredKey)
                    // make the rightmost index the mid -1 as we know the desired element is not at the mid indexv
                    return binarySearch(left, mid - 1, desiredKey);

                // we know the opposite so we look at everything after mid
                return binarySearch(mid + 1, right, desiredKey);
            }

            // a value was not found so return -1
            return -1;
        }

        /// <summary>
        /// Lists all of the book moves for a position, if one was not found then a error message is shown
        /// </summary>
        /// <param name="desiredKey">The key of the position that is to be found in the book</param>
        public static void showBookMoves(ulong desiredKey)
        {
            int index = binarySearch(0, entries.Length, desiredKey);
            if (index == -1)
            {
                Console.WriteLine("Could not find");
                return;
            }
            listBook(index, desiredKey);

            return;
        }

        /// <summary>
        /// Probes the book and finds the best entry 
        /// </summary>
        /// <param name="kingloc"></param>
        /// <param name="desiredKey"></param>
        /// <returns>The best opening book move</returns>
        public static Move queryBook(int kingloc, ulong desiredKey)
        {
            // perform a binary search to find an index
            int index = binarySearch(0, entries.Length, desiredKey);

            if (index == -1) // a entry was not found so a empty move is returned
            {
                return new Move(true);
            }

            int startIndex = findStartIndex(index, desiredKey);

            int bestWeight = -10000;
            Move bestMove = new Move(true);
            int counter = 0;
            try
            {
                // check if the book uses the same weight for all of the moves
                if (entries[startIndex].weight == entries[startIndex + 1].weight)
                {
                    // pick a random move which is in the range
                    int length = findRange(startIndex, desiredKey);
                    index = randomGen.Next(startIndex, startIndex + length);
                }

                else
                {
                    // pick the first move as it is stored in decending weight order
                    index = startIndex;
                }
            }
            catch
            {
                // if something goes wrong return the first index
                index = startIndex;
            }

            // get the move information (the start and destination index)
            PolyEntry entry = entries[index];
            int fromLoc = 8 * entry.startRow + (7 - entry.startFile);
            int toLoc = 8 * entry.destinationRow + (7 - entry.destinationFile);

            // castling moves are in a untradiditonal format so we need to correct this (e1h1 replace with e1g1)
            if (fromLoc == kingloc)
            {
                // check if it is catling king side
                if (entry.destinationFile == 7)
                {
                    toLoc = 8 * entry.destinationRow + (8 - entry.destinationFile);
                }

                else if (entry.destinationFile == 0)
                {
                    toLoc = 8 * entry.destinationRow + (5 - entry.destinationFile);
                }
            }

            // check if it is a promotion
            ushort promotion = (ushort)entry.pieceIndex;

            return new Move((byte)fromLoc, (byte)toLoc, promotion);


        }


        static Polyglot()
        {
            bookPath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName.Split(new string[] { "UI", "Engine" }, StringSplitOptions.None)[0];
            bookPath = Path.Combine(bookPath, "Openings", "codekiddy.bin");
            readBook();
        }
    }
}

/*
Books with Weight:
codekiddy.bin
DCbook_large.bin
Elo2400.bin
final-book.bin
gm2600.bin
SmallBook.bin
Titans.bin

castling moves are encoded as e1h1 not e1g1 which is annoying.
 */
