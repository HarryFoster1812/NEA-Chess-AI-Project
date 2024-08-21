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
    internal class CurrentGame : Game
    {

        internal PlayerInfoView playerInfo;

        int winner = -1; // 0 for white 1 for black, 2 draw, -1 no winner

        internal CurrentGame(BoardViewModel model, string _fen, string _event = "Casual", int _round = 1, string _site = "Online")
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
            players[0] = new Player(this);
            players[1] = new Player(this);


        }

        internal CurrentGame(BoardViewModel model, string _event = "Casual", int _round = 1, string _site = "Online")
        {
            GameInformationView.movesStackPanel.Children.Clear();
            board = new Bitboard();
            boardModel = model;
            players[0] = new Player(this);
            players[1] = new Player(this);
            movesPlayed = new List<UIMove>();
            movesPlayed.Add(new UIMove(new Move(), board));
            Event = _event;
            Round = _round;
            Site = _site;
        }


        public override void MakeMove(Move move)
        {
            if (winner == -1) // check if the game is over
            {
                string lastMovePlayed = "";
                try
                {
                    lastMovePlayed = movesPlayed[movesPlayed.Count - 1].uciCommand;
                }
                catch
                {
                    lastMovePlayed = "";
                }

                movesPlayed.Add(new UIMove(move, board, lastMovePlayed));
                board.MakeMove(move);
                CurrentMoveNo = movesPlayed.Count - 1;
                DisplayMove(movesPlayed[movesPlayed.Count - 1].MoveStringAN, board.moveCount);
                int turn = board.WhiteToPlay ? 0 : 1;

                // check for checkmate
                if (board.isMate())
                {
                    MessageBox.Show("CheckMate");
                    endGame(turn ^ 1);
                    return;
                }


                else if (board.isStalemate())
                {
                    endGame(2);
                    return;
                }

                players[turn].SendMove();

                playerInfo.switchClocks();
            }
        }

        private void DisplayMove(string moveString, int moveNo) // add the move to the Move list that will display the move history
        {
            StackPanel movesStack = GameInformationView.movesStackPanel;

            if (movesStack.Children.Count != 0 && ((Grid)(movesStack.Children[movesStack.Children.Count - 1])).Children.Count != 3) // in a full grid the moveNo, white move and black move is displayed
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

        public void takeBack()
        {
            // decrement the variables which store the move count information
            CurrentMoveNo--;

            // undo the move on the board
            Move lastMove = movesPlayed[movesPlayed.Count - 1].Move;
            board.UndoMove(lastMove);
            movesPlayed.RemoveAt(movesPlayed.Count - 1);
            playerInfo.switchClocks();

            // get the move on the move grid
            StackPanel movesStack = GameInformationView.movesStackPanel;
            Grid LastMoveGrid = (Grid)(movesStack.Children[movesStack.Children.Count - 1]);

            // check if the last move played was whites move
            if (LastMoveGrid.Children.Count != 3)
            {
                // we can just remove the last grid from the stack
                int lastIndex = movesStack.Children.Count - 1;
                movesStack.Children.RemoveAt(lastIndex);
            }

            else
            { // it was blacks turn, we need to remove the last element in the grid 
                int lastIndex = LastMoveGrid.Children.Count - 1;
                LastMoveGrid.Children.RemoveAt(lastIndex);
            }

            currentMoveNo = CurrentMoveNo;

        }

        // end the game
        // winner: 2 Draw, 0 White, 1 Black
        public void endGame(int winningcolour)
        {
            winner = winningcolour;
            PlayingScreen playingscreen = (PlayingScreen)((Grid)((BoardView)(boardModel.boardCanvas.Parent)).Parent).Parent;

            // draw
            if (winner == 2)
            {
                // show draw
                playingscreen.displayGameOver("Draw");
            }
            else if (winner == 0)
            {
                // white wins
                playingscreen.displayGameOver("White Wins");
            }

            else
            {
                // black wins
                playingscreen.displayGameOver("Black wins");
            }

            // diable the users from making any more moves
            players[0].canMove = false;
            players[1].canMove = false;

            // end the connection with the bot/ the online player
            Bot.connectionEnded = true;
            OnlinePlayer.connectionEnded = true;


        }

    }
}
