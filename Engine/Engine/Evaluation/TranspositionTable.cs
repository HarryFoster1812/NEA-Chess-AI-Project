using System;
using System.Linq;

namespace Engine.Evaluation
{

    public enum TTFlag
    {
        Exact, // PV Node
        Fail_Low_Alpha,  // a better move was not found
        Fail_High_Beta, // beta cut off (the opponent could play a better move)
    }

    internal class TTEntry // use a class because a struct is a value type and is not nullable
    {
        public ulong zobrist; // the hash of the position
        public int depth; // the depth that it was search at (the accuracy of the value)
        public TTFlag flag; // what type of node it is
        public double eval; // the evaluation of the position
        public Move bestMove; // the next best move that should be played for the position

        public TTEntry(ulong zobrist, int depth, TTFlag flag, double eval, Move bestMove)
        {
            this.zobrist = zobrist;
            this.depth = depth;
            this.flag = flag;
            this.eval = eval;
            this.bestMove = bestMove;
        }
    }

    internal class TranspositionTable
    {
        TTEntry[] TTable; // the array of transposition entries
        uint Size; // the number of entries int the transposition table

        int sizeMB; // the approximation of how many MB the table should occupy when full

        // debugging vars
        ulong successfulAccess = 0;
        ulong unsuccessfulAccess = 0;
        ulong timesAccessed = 0;
        ulong writes = 0;
        ulong rewrites = 0;
        ulong rewriteCollisions = 0;

        public TranspositionTable(int sizeMb)
        {
            sizeMB = sizeMb;
            int sizeBytes = sizeMB * 1000 * 1000;
            int EntrySizeBytes = 32; // rough estimate
            Size = (uint)(sizeBytes / EntrySizeBytes);
            TTable = new TTEntry[Size];
        }

        public void add(ulong zobrist, int depth, TTFlag flag, double eval, Move bestMove)
        {
            // calculate the hash index
            int hashIndex = (int)(zobrist % Size);
            // retrive the current item at the hash index
            TTEntry current = TTable[hashIndex];

            // check if it is is free
            if (current == null || depth > current.depth)
            {
                // check for collision
                if (current != null)
                {
                    rewrites++;
                    if (current.zobrist != zobrist)
                    {
                        rewriteCollisions++;
                    }
                }

                writes++;
                TTable[hashIndex] = new TTEntry(zobrist, depth, flag, eval, bestMove);
            }
        }

        public bool get(ulong zobrist, double alpha, double beta, int depth, out double value) // returns the lookup value of the hash, if not found then returns false
        {
            timesAccessed++;
            value = 0;
            int hashIndex = (int)(zobrist % Size);
            TTEntry entry = TTable[hashIndex];

            // check for a collision
            if (entry != null && entry.zobrist == zobrist)
            {
                // check to see if the position is evaluated to a higher depth than given
                if (entry.depth >= depth)
                {
                    successfulAccess++;
                    if (entry.flag == TTFlag.Exact)
                    {
                        value = entry.eval;
                    }

                    else if (entry.flag == TTFlag.Fail_Low_Alpha && entry.eval <= alpha)
                    {
                        value = alpha;
                    }
                    // caused a beta cut off (there was a better move)
                    else if (entry.flag == TTFlag.Fail_High_Beta && alpha >= beta)
                    {
                        value = entry.eval;
                    }

                    return true;
                }
            }

            unsuccessfulAccess++;
            return false;
        }


        public bool get(ulong zobrist, out TTEntry value) // a overload which is used for the PV
        {
            timesAccessed++;
            int hashIndex = (int)(zobrist % Size);
            TTEntry entry = TTable[hashIndex];

            // check for a collision
            if (entry != null && entry.zobrist == zobrist && entry.flag == TTFlag.Exact)
            {
                value = entry;
                return true;
            }

            unsuccessfulAccess++;
            value = null;
            return false;
        }

        public void ClearTable()
        {

            Array.Clear(TTable, 0, (int)Size);
        }

        public void resetDebuggingVars()
        {
            successfulAccess = 0;
            unsuccessfulAccess = 0;
            timesAccessed = 0;
            writes = 0;
            rewrites = 0;
            rewriteCollisions = 0;
        }

        // gets stuck in a loop
        public string getPV(ulong rootZobrsit, Bitboard board)
        {

            string PrincipalVariation = "";
            // create a copy of the position
            Bitboard copy = board.DeepClone();

            ulong currentZobrist = rootZobrsit;
            // go down the PV until we find the leaf node
            while (get(currentZobrist, out TTEntry entry) && !entry.bestMove.isEmpty)
            {
                // check if the move is in the PV, if it is then it will cause a loop
                if (PrincipalVariation.Contains(Tools.MoveToString(entry.bestMove)))
                {
                    break;
                }
                // convert the move to a string to add to the PV
                PrincipalVariation += Tools.MoveToString(entry.bestMove) + " ";
                copy.MakeMove(entry.bestMove);
                // get the new zobrist hash to lookup and find the new best move down the PV
                currentZobrist = copy.zobristKey.Key;
            }

            return PrincipalVariation;
        }


        public void printStats()
        {
            Console.WriteLine($"info Size of table: {sizeMB}");
            Console.WriteLine($"info Times Accessed: {timesAccessed}");
            Console.WriteLine($"info Successful Accesses: {successfulAccess}");
            Console.WriteLine($"info Unsuccessful Accesses: {unsuccessfulAccess}");
            Console.WriteLine($"info Writes: {writes}");
            Console.WriteLine($"info Rewrites: {rewrites}");
            Console.WriteLine($"info Rewrite Collisions: {rewriteCollisions}");
            Console.WriteLine($"info TTEntries: {TTable.Count(s => s != null)}");
            Console.WriteLine($"info % of TT used: {Decimal.Divide(TTable.Count(s => s != null), TTable.Length) * 100}\n\n");
        }
    }
}
