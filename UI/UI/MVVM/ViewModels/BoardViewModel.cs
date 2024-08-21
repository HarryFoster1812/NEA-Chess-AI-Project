using Engine;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.MVVM.Models;

namespace UI.MVVM.ViewModels
{
    public class BoardViewModel : BoardViewModelBase
    {
        public BoardViewModel(Canvas root)
        {
            boardCanvas = root;
            game = new CurrentGame(this);
            boardCanvas.MouseMove += MouseMove;
            boardCanvas.MouseRightButtonUp += OnRightMouseUp;
            boardCanvas.MouseLeftButtonUp += OnLeftMouseUp;
            squares = new Square[64];
            assetsPath = System.IO.Path.Combine(Directory.GetParent(appFolderPath).Parent.Parent.FullName, "Assets");
            SquareLength = boardCanvas.Height / 8;
            DrawBoard();
            DrawPieces(this.game.board);
        }


        public override void OnLeftMouseUp(object sender, MouseEventArgs e)
        {
            LeftMouse = false;
            int index = calculateBitBoardIndexAtMousePosition();
            int boardIndex = getBoardViewIndex(index, whiteView);
            boardCanvas.Children.Remove(MovingPiece);

            if (boardIndex == selectedIndexLeft) // user clicks on the same square
            {
                squares[boardIndex].border.Child = MovingPiece; // replace the piece
                if (promotionWindow != null)
                {
                    RemovePromotion();
                }
            }

            else if (LegalMoves.Contains(index) && selectedIndexLeft != -1 && game.currentMoveNo == (game.MovesPlayedCount - 1)) // it is a valid move and we are on the lastest board and a piece was originally selected
            {
                // make move
                Move moveMade = game.createMove(getBitboardIndex(selectedIndexLeft, whiteView), index);

                if (moveMade.isPromotion)
                {
                    if (promotionWindow != null) // the user selects a diffrent promotion move
                    {
                        RemovePromotion();
                    }

                    ShowPromotion(moveMade, getBitboardIndex(index, whiteView));
                    return;
                }

                game.MakeMove(moveMade);
                DrawBoard();
                DrawPieces(game.board);
                selectedIndexLeft = -1;
            }

            else if (selectedIndexLeft != -1) // the user did not select a valid move
            {

                try
                {
                    squares[selectedIndexLeft].border.Child = MovingPiece; // replace the piece
                    RemovePromotion();
                }

                catch (Exception)
                {

                }

            }
        }

        public override void OnLeftMouseDown(object sender, MouseEventArgs e)
        {
            Border SquareClicked = (Border)sender;
            int index = (int)SquareClicked.Tag;
            int boardIndex = getBitboardIndex(index, whiteView);
            ulong boardIndexBitboard = BitBoardTools.IndexToBitboard((byte)boardIndex);
            if ((boardIndexBitboard & game.board.Board[game.board.WhiteToPlay ? 0 : 1]) != 0 && game.players[game.board.WhiteToPlay ? 0 : 1].canMove)
            {
                if ((SVGImage.SVG.SVGImage)SquareClicked.Child != null)
                {  // this when the promotion screen is active and the player clicks on the original square
                    MovingPiece = (SVGImage.SVG.SVGImage)SquareClicked.Child; // set the moving piece to whatever is in the square
                    SquareClicked.Child = null; // empty the square

                    Point position = Mouse.GetPosition(boardCanvas);

                    Canvas.SetLeft(MovingPiece, position.X - MovingPiece.Width / 2); // set the position of the moving piece to the centre of the mouse
                    Canvas.SetTop(MovingPiece, position.Y - MovingPiece.Height / 2);
                    boardCanvas.Children.Add(MovingPiece);

                }

                LeftMouse = true;
                selectedIndexLeft = index;

                LegalMoves = filterMoves(game.genMoves(), boardIndex);
                DrawAvailibleMoves(LegalMoves);
            }

        }
    }
}
