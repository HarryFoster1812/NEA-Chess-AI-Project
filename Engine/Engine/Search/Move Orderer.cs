using System.Collections.Generic;

namespace Engine.Search
{


    internal static class Move_Orderer
    {
        // get each move and give it a rating on how good it might be
        // apply a sorting algorithm (merge sort maybe / quick sort / heap sort)

        // general move ordering should be:
        // PV move
        // captures (mvv-lva)
        // killers
        // history score

        static readonly int[] VictimScores = { 100, 200, 300, 400, 500, 600 };

        static int[][] MvvLvaScores = new int[6][]; // {pawn[], knight[], bishop[], rook[], queen[], king[]} [victim][attacker]

        static Move_Orderer()
        {
            initMvvLva();
        }

        internal static void initMvvLva()
        {
            for (int victim = 0; victim < MvvLvaScores.Length; victim++)
            {
                MvvLvaScores[victim] = new int[6];
                for (int attacker = 0; attacker < MvvLvaScores[victim].Length; attacker++)
                {
                    MvvLvaScores[victim][attacker] = VictimScores[victim] + 6 - VictimScores[attacker] / 100;
                }
            }
        }

        static void swap(double[] arr, ref List<Move> list, int i, int j)
        {
            double temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;

            Move tempmove = list[i];
            list[i] = list[j];
            list[j] = tempmove;
        }

        static int partition(double[] arr, ref List<Move> moves, int low, int high)
        {
            // Choosing the pivot
            double pivot = arr[high];

            // Index of smaller element and indicates
            // the right position of pivot found so far
            int i = (low - 1);

            for (int j = low; j <= high - 1; j++)
            {

                // If current element is smaller than the pivot
                if (arr[j] > pivot)
                {

                    // Increment index of smaller element
                    i++;
                    swap(arr, ref moves, i, j);
                }
            }
            swap(arr, ref moves, i + 1, high);
            return (i + 1);
        }

        public static void quickSort(double[] moveScores, ref List<Move> moves, int left, int right)
        {

            if (left < right)
            {

                // pi is partitioning index, arr[p]
                // is now at right place
                int pi = partition(moveScores, ref moves, left, right);

                // Separately sort elements before
                // and after partition index
                quickSort(moveScores, ref moves, left, pi - 1);
                quickSort(moveScores, ref moves, pi + 1, right);
            }
            //
        }

        public static void orderMoves(ref List<Move> moves, Bitboard board)
        {
            // for each capture
            // calculate the mvv-lva score
            // sort the captures
            double[] moveScores = new double[moves.Count];

            for (int i = 0; i < moves.Count; i++)
            {
                moveScores[i] = EvaluateMove(moves[i], board);
            }

            // sort moves
            quickSort(moveScores, ref moves, 0, moveScores.Length - 1);
        }

        private static double EvaluateMove(Move move, Bitboard b)
        {
            // unpack move information
            byte startindex = move.startIndex;
            ulong startBitboard = BitBoardTools.IndexToBitboard(startindex);
            byte destinationindex = move.destinationIndex;
            ulong destinationBitboard = BitBoardTools.IndexToBitboard(destinationindex);
            int typeOfPieceMoving = Bitboard.getPieceType(b.Board, startBitboard) - 2;
            int enemyColour = b.WhiteToPlay ? 1 : 0;

            // if pv

            // if capture
            if ((b.Board[enemyColour] & destinationBitboard) != 0)
            {
                int enemyPieceType = Bitboard.getPieceType(b.Board, destinationBitboard) - 2;
                return MvvLvaScores[enemyPieceType][typeOfPieceMoving] + 1000;
            }

            // if killer

            // history

            return 0;
        }

    }
}
