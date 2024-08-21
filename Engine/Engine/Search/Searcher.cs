using Engine.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.Search
{
    internal class Searcher
    {
        Generation generator = new Generation();
        Evaluator evaluator = new Evaluator();
        TranspositionTable transpositionTable = new TranspositionTable(200);
        Bitboard board;
        Random random = new Random();
        int MaxDepth;
        Move bestMove = new Move();
        Move bestMoveIteration = new Move();

        double bestScoreSofar;
        int currentIterationDepth;

        bool searchStopped = false;
        bool debug = false;
        ulong nodescount = 0;
        string debugpath = Path.Combine(Directory.GetCurrentDirectory(), "Debug.txt");


        /// <summary>
        /// Given the states of the two players it will calculate the amount of time the program should search for os that the player will not run out of time
        /// </summary>
        /// <param name="whiteTime">the amount of time in ms that white has left on the clock</param>
        /// <param name="blackTime">the amount of time in ms that black has left on the clock</param>
        /// <param name="whiteInc"> the amount of time in ms that white will add every move played</param>
        /// <param name="blackInc"> the amount of time in ms that black will add every move played</param>
        public static int CalculateSearchTime(int whiteTime, int blackTime, int whiteInc, int blackInc, bool WhiteToPlay)
        {
            int remainingTime = WhiteToPlay ? whiteTime : blackTime;

            // If remaining time is less than or equal to zero, return a default value
            if (remainingTime <= 0)
            {
                return 1000;
            }

            // Calculate search time based on remaining time and increments
            int searchTime = remainingTime / 30;

            // Add increment time if applicable
            if (WhiteToPlay)
            {
                searchTime += whiteInc;
            }
            else
            {
                searchTime += blackInc;
            }

            // Ensure search time is at least a minimum value (adjust as needed)
            int minSearchTime = 100;
            if (searchTime < minSearchTime)
            {
                searchTime = minSearchTime;
            }

            return searchTime;
        }

        internal void stopSearch()
        {
            searchStopped = true;
        }

        private void resetSearchVars()
        {
            searchStopped = false;
            nodescount = 0;
            transpositionTable.resetDebuggingVars();
        }

        public void Search(Bitboard b, int maxDepth = 256)
        {
            this.board = b;
            resetSearchVars();
            double score = 0;
            string pv = "";

            double alpha = -500000;
            double beta = 500000;


            for (int depth = 1; depth <= maxDepth; depth++)
            {
                currentIterationDepth = depth;
                score = alphaBetaNega(depth, alpha, beta);

                if ((score <= alpha) || (score >= beta))
                {
                    alpha = -50000;
                    beta = 50000;
                    continue;
                }

                // set up the window for the next iteration
                alpha = score - 50;
                beta = score + 50;

                pv = transpositionTable.getPV(board.zobristKey.Key, board);

                Console.WriteLine($"info score cp {Math.Round(score)} depth {depth} nodes {nodescount} pv {pv}");

                if (searchStopped)
                {
                    break;
                }
                else
                {
                    bestMove = bestMoveIteration;
                }

            }

            transpositionTable.printStats();
            pv = transpositionTable.getPV(board.zobristKey.Key, board);
            Console.WriteLine($"info score cp {Math.Round(score)} depth {maxDepth} nodes {nodescount} pv {pv}");
            Console.WriteLine("bestmove " + Tools.MoveToString(bestMove));

        }

        // search the position until the position has no more captures (a quiet position)
        // removes the horizion effect
        double Quiescence(double alpha, double beta)
        {
            double staticEval = evaluator.EvaluateFromPov(board);

            nodescount++;

            if (searchStopped)
            {
                return staticEval;
            }

            // fail hard beta cutoff
            if (staticEval >= beta)
            {
                return beta;
            }

            // better move found
            if (staticEval > alpha)
            {
                alpha = staticEval;
            }

            List<Move> moves = generator.GenerateMoves(board, true);
            // loop each capture
            foreach (Move i in moves)
            {
                board.MakeMove(i);
                double score = -Quiescence(-beta, -alpha);
                board.UndoMove(i);

                // better move found
                if (score > alpha)
                {
                    alpha = score;
                }

                // fail hard beta cutoff
                if (score >= beta)
                {
                    return beta;
                }
            }

            return alpha;

        }

        double alphaBetaNega(int depth, double alpha, double beta)
        {
            bool isNotRootNode = (currentIterationDepth - depth) == 0 ? false : true;

            // probe transposition table if not at root node
            if (isNotRootNode && transpositionTable.get(board.zobristKey.Key, alpha, beta, depth, out double LookUpResult))
            {
                return LookUpResult;
            }

            if (searchStopped)
            {
                return 0;
            }

            // if we reach the maximum search depth or the search is stopped
            if (depth == 0)
            {
                double QuisenceEval = Quiescence(alpha, beta);
                return QuisenceEval;
            }

            // increment nodes count
            nodescount++;

            // check for the 50 move rule
            if (board.plyCount >= 100)
            {
                return 0;
            }

            List<Move> moves = generator.GenerateMoves(this.board);

            if (moves.Count == 0)
            {
                if (generator.isCheck) // opponent has checkmated
                {
                    double plysSearched = currentIterationDepth - depth;
                    return -10000 + plysSearched; // return negative as it is always bad from our pov
                }

                return 0; // stalemate
            }

            double originalAlpha = alpha;
            double recursiveScore;
            Move bestMove = new Move(true);
            TTFlag tFlag = TTFlag.Fail_Low_Alpha;

            // sort moves
            Move_Orderer.orderMoves(ref moves, board);
            foreach (Move i in moves)
            {
                board.MakeMove(i);
                // calculate check extentions
                recursiveScore = -alphaBetaNega(depth - 1, -beta, -alpha);
                board.UndoMove(i);

                // fail hard beta cutoff
                if (recursiveScore >= beta)
                {
                    transpositionTable.add(board.zobristKey.Key, depth, TTFlag.Fail_High_Beta, beta, i);
                    return beta;
                }

                // better move found
                if (recursiveScore > alpha)
                {
                    alpha = recursiveScore;
                    tFlag = TTFlag.Exact;
                    bestMove = i;
                }
            }

            // alpha has been improved
            // this is the new pv node
            if (alpha > originalAlpha)
            {
                bestMoveIteration = bestMove;
            }

            if (!searchStopped) // if the search has been stopped the value is likely to be inaccurate
            {
                transpositionTable.add(board.zobristKey.Key, depth, tFlag, alpha, bestMove);
            }

            return alpha;

        }

        int CalculateExtentions()
        {
            return 0;
        }
    }
}

/* Notes:
    When the search is stopped and all of the nodes are being searched from the TT then the best move does not change from the last search.
 */
