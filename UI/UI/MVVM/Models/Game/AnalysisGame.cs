using Engine;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UI.MVVM.Models.Players;
using UI.MVVM.ViewModels;
using UI.MVVM.Views;

namespace UI.MVVM.Models
{
    internal class AnalysisGame : Game
    {

        internal new AnalysisBoardViewModel boardModel;
        internal AnalysisBot bot;

        public new int currentMoveNo
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

        internal AnalysisGame(AnalysisBoardViewModel model, string _fen, string _event = "Casual", int _round = 1, string _site = "Online")
        {
            GameInformationView.movesStackPanel.Children.Clear();

            try
            {
                board = Tools.FENtoBitboard(_fen);
            }

            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            movesPlayed = new List<UIMove>();
            movesPlayed.Add(new UIMove(new Move(), board));
            this.startFromFEN = true;
            FEN = _fen;
            Event = _event;
            Round = _round;
            Site = _site;
            boardModel = model;
            bot = new AnalysisBot(this, User.Engines[User.Settings.DefaultEngine].path);


        }

        internal AnalysisGame(AnalysisBoardViewModel model, string _event = "Casual", int _round = 1, string _site = "Online")
        {
            GameInformationView.movesStackPanel.Children.Clear();
            board = new Bitboard();
            boardModel = model;
            bot = new AnalysisBot(this, User.Engines[User.Settings.DefaultEngine].path);
            movesPlayed = new List<UIMove>();
            movesPlayed.Add(new UIMove(new Move(), board));
            Event = _event;
            Round = _round;
            Site = _site;
        }

        public override void MakeMove(Move move)
        {
            string uciposition = "";
            try
            {
                uciposition = movesPlayed[currentMoveNo].uciCommand;
            }
            catch
            {
                uciposition = "";
            }
            movesPlayed.Add(new UIMove(move, board, uciposition));
            board.MakeMove(move);
            CurrentMoveNo = movesPlayed.Count - 1;
            DisplayMove(movesPlayed[movesPlayed.Count - 1].MoveStringAN, board.moveCount);


            // set the bot to the new position
            bot.SendMove();

            // clear the old analysis
            AnalysisScreen.analysisView.Clear();

        }

        private void DisplayMove(string moveString, int moveNo) // add the move to the Move list that will display the move history
        {
            StackPanel movesStack = GameInformationView.movesStackPanel;

            if (movesStack.Children.Count != 0 && ((Grid)(movesStack.Children[movesStack.Children.Count - 1])).Children.Count != 3)
            { // it is blacks move on the panel and it will add it to the existing grid

                Grid moveGrid = ((Grid)(movesStack.Children[movesStack.Children.Count - 1]));
                Button tempMoveButton = new Button();
                tempMoveButton.Tag = movesPlayed.Count - 1;
                tempMoveButton.Click += (sender, e) => { changeBoardViewToMove((int)((Button)sender).Tag); };
                tempMoveButton.Content = moveString;
                Grid.SetColumn(tempMoveButton, 2);
                moveGrid.Children.Add(tempMoveButton);
                return;
            }

            // create a new move grid
            Grid movegrid = new Grid()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                }
            };

            movegrid.ColumnDefinitions[0].Width = new GridLength(0.333333333333, GridUnitType.Star);

            TextBlock moveNoTextBlock = new TextBlock();
            moveNoTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            moveNoTextBlock.Foreground = User.Settings.TextColour.ColourBrush;
            moveNoTextBlock.Text = moveNo.ToString();
            Grid.SetColumn(moveNoTextBlock, 0);

            Button moveButton = new Button();
            moveButton.Tag = movesPlayed.Count - 1;
            moveButton.Click += (sender, e) => { changeBoardViewToMove((int)((Button)sender).Tag); };
            moveButton.Content = moveString;
            Grid.SetColumn(moveButton, 1);

            movegrid.Children.Add(moveNoTextBlock);
            movegrid.Children.Add(moveButton);
            movesStack.Children.Add(movegrid);
        }

        public new void changeBoardViewToMove(int index)
        {
            CurrentMoveNo = index;
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

            // draw the board in the new position
            boardModel.DrawBoard();
            boardModel.DrawPieces(board);
            boardModel.DrawComments(movesPlayed[index].Comments);

            // clear the analysis view for the new position
            AnalysisScreen.analysisView.Clear();
            bot.SendMove();
        }

        public void addCommentToCurrentPosition(string start, string end, string type)
        {
            string Comment = $"[{start},{end},{type}]";
            // we are makeing sure that the current game is greater than zero otherwise the comment is not related to any move
            if (movesPlayed[currentMoveNo].Comments.Contains(Comment))
            {
                // the comment is already in the move
                return;
            }
            movesPlayed[currentMoveNo].Comments.Add(Comment);
            boardModel.DrawBoard();
            boardModel.DrawPieces(board);
            boardModel.DrawComments(movesPlayed[currentMoveNo].Comments);
        }

    }
}
