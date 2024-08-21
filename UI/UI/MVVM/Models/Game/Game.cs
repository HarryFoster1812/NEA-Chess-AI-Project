using Engine;
using System;
using System.Collections.Generic;
using UI.MVVM.Models.Players;
using UI.MVVM.ViewModels;

namespace UI.MVVM.Models
{
    internal abstract class Game
    {
        internal Bitboard board;
        internal Generation generator = new Generation();
        internal bool startFromFEN = false;
        internal string FEN = "";
        internal string Event;
        internal int Round;
        internal string Site;
        internal DateTime dateTime = DateTime.Now;
        protected int CurrentMoveNo = 0;
        public int currentMoveNo
        {
            get { return CurrentMoveNo; }

            set
            {
                if (value < 0 || value >= movesPlayed.Count)
                {
                    throw new Exception("Index Execption, the move number given was not within range");
                }
                CurrentMoveNo = value;
                changeBoardViewToMove(CurrentMoveNo);

            }
        }
        public int MovesPlayedCount
        {
            get { return movesPlayed.Count; }
            set { }
        }

        internal static List<UIMove> movesPlayed = new List<UIMove>();
        internal Player[] players = new Player[2];
        internal BoardViewModel boardModel;


        public string genUCIPositionCommand(int stopindex = -1)
        {

            string Command = "position ";
            if (startFromFEN)
            {
                Command += "fen " + FEN;
            }

            else Command += "startpos ";

            if (movesPlayed.Count == 0 || stopindex == 0) { return Command; }

            Command += "moves ";

            Command += movesPlayed[currentMoveNo].uciCommand;

            return Command;
        }

        public virtual void MakeMove(Move move)
        {

        }


        // end the game
        public virtual void endGame()
        {

        }

        public void changeBoardViewToMove(int index)
        {
            CurrentMoveNo = index;
            board = new Bitboard();

            for (int i = 1; i <= index; i++)
            {
                board.MakeMove(movesPlayed[i].Move);
            }

            boardModel.DrawBoard();
            boardModel.DrawPieces(board);
            boardModel.DrawComments(movesPlayed[index].Comments);
        }

        public List<Move> genMoves()
        {
            return generator.GenerateMoves(this.board);
        }

        virtual public Move createMove(int start, int destination)
        {
            board = new Bitboard();
            if (currentMoveNo != 0)
            {
                string uci = movesPlayed[currentMoveNo].uciCommand;
                string[] moveslist = uci.Split(' ');
                foreach (string move in moveslist)
                {
                    if (move == "")
                    {
                        break;
                    }
                    // try and convert the move from a string
                    if (CommandHandler.stringToMove(move, board, out Move target))
                    {
                        board.MakeMove(target);
                    }
                }
            }


            foreach (Move i in generator.GenerateMoves(board))
            {
                if (i.startIndex == start && i.destinationIndex == destination)
                {
                    return i;
                }
            }

            throw new Exception("Move Not Found");
        }

        public void SaveAsPGN()
        {

        }

    }

}
