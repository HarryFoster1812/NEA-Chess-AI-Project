using Iced.Intel;
using NEA_Chess_Ai_Project.Evaluation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace NEA_Chess_Ai_Project.Search
{
    internal class Searcher
    {
        Generation generator = new Generation();
        Evaluator evaluator = new Evaluator();
        Bitboard board;
        Random random= new Random();

        public (Move, string, double) rootAlphaBetaSearch(Bitboard b, int depth, bool isMaximiser) {
            this.board = b;
            List<Move> moves = generator.GenerateMoves(board);
            double bestEval = double.NegativeInfinity;
            Move bestMoveFound = new Move(0,0);
            double alpha = double.NegativeInfinity;
            double beta = double.PositiveInfinity;
            foreach (Move i in moves) {
                board.MakeMove(i);
                if (isMaximiser)
                {
                    double value = alphaBetaMin(ref alpha, ref beta, depth - 1);
                    if (value >= bestEval)
                    {
                        bestEval = value;
                        bestMoveFound = i;
                    }
                }
                else{
                    double value = alphaBetaMax(ref alpha, ref beta, depth - 1);
                    if (value <= bestEval)
                    {
                        bestEval = value;
                        bestMoveFound = i;
                    }
                }
                board.UndoMove(i);
            }
            if (bestMoveFound.startIndex == bestMoveFound.destinationIndex) {
                bestMoveFound = moves[random.Next(moves.Count)];
            }

            return (bestMoveFound, Tools.MoveToString(bestMoveFound), bestEval);
        }

        public double alphaBetaMax(ref double alpha, ref double beta, int depth)
        {
            if (depth == 0)
            {
                double value = evaluator.Evaluate(this.board);
                return value;
            }
            List<Move> moves = generator.GenerateMoves(this.board);
            foreach (Move i in moves)
            {

                board.MakeMove(i);
                double score = alphaBetaMin(ref alpha, ref beta, depth - 1);
                Console.WriteLine($"Minimising {score}\nAlpha: {alpha}\nBeta: {beta}\n\n");

                board.UndoMove(i);
                if (score >= beta)
                {
                    return beta;   // fail hard beta-cutoff
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }
            return alpha;
        }

        public double alphaBetaMin(ref double alpha, ref double beta, int depth)
        {
            if (depth == 0)
            {
                double value = evaluator.Evaluate(this.board);
                return value;
            }
            List<Move> moves = generator.GenerateMoves(this.board);
            foreach (Move i in moves)
            {
                
                board.MakeMove(i);
                double score = alphaBetaMax(ref alpha, ref beta, depth - 1);
                Console.WriteLine($"Minimising {score}\nAlpha: {alpha}\nBeta: {beta}\n\n");
                board.UndoMove(i);
                if (score <= alpha)
                {
                    return alpha;   // fail hard beta-cutoff
                }
                if (score < beta)
                {
                    beta = score; // alpha acts like max in MiniMax
                }
            }
            return beta;
        }
    }
}
