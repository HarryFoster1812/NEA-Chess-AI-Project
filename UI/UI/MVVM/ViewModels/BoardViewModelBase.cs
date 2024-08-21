using Engine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UI.MVVM.Models;
using UI.MVVM.Models.Players;
using UI.MVVM.Views;

namespace UI.MVVM.ViewModels
{
    public abstract class BoardViewModelBase
    {
        #region White View Bool
        protected static bool whiteView = true;

        public bool WhiteView
        {
            get { return whiteView; }

            set
            {
                whiteView = value;
                DrawBoard();
                DrawPieces(this.game.board);
                if (CurrentGame.movesPlayed.Count != 0)
                {
                    DrawComments(CurrentGame.movesPlayed[game.currentMoveNo].Comments);
                }
                selectedIndexLeft = -1;
                selectedIndexRight = -1;
            }
        }
        #endregion

        internal Game game;

        #region Private Variables
        public Canvas boardCanvas;
        protected bool LeftMouse;
        protected bool RightMouse;
        protected SVGImage.SVG.SVGImage MovingPiece;
        protected int selectedIndexLeft = -1;  // use two different variables so that both of the buttons will work without breaking the other
        protected int selectedIndexRight = -1;
        protected List<int> LegalMoves = new List<int>();
        protected Polygon arrow;
        protected Promotion promotionWindow;
        #endregion

        #region Static Variables
        static readonly string[] pieceNames = { "P", "N", "B", "R", "Q", "K" };

        protected string appFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        protected static Square[] squares = new Square[64];

        protected static double SquareLength = 0;

        protected static string assetsPath = "";
        #endregion


        public BoardViewModelBase()
        {

        }

        public virtual void OnRightMouseDown(object sender, MouseEventArgs e)
        {
            RightMouse = true;
            Border SquareClicked = (Border)sender;
            int index = (int)SquareClicked.Tag;
            int boardIndex = getBitboardIndex(index, whiteView);
            selectedIndexRight = index;
            // create arrow
            double borderX = Canvas.GetRight(squares[selectedIndexRight].border);
            double startX = boardCanvas.Width - (borderX + SquareLength / 2);
            double startY = boardCanvas.Height - (Canvas.GetBottom(squares[selectedIndexRight].border) + SquareLength / 2);
            Point mousePos = Mouse.GetPosition(boardCanvas);
            PointCollection arrowPoints = calculateArrowPoints(startX, startY, mousePos.X, mousePos.Y);
            arrow = new Polygon();
            arrow.Points = arrowPoints;
            arrow.Fill = Brushes.Red;

            boardCanvas.Children.Add(arrow);
        }

        public abstract void OnLeftMouseDown(object sender, MouseEventArgs e);

        public virtual void OnRightMouseUp(object sender, MouseEventArgs e)
        {
            int index = calculateBitBoardIndexAtMousePosition();
            int boardViewIndex = getBitboardIndex(index, whiteView);
            int selectedBitboardIndex = getBitboardIndex(selectedIndexRight, whiteView);
            RightMouse = false;
            boardCanvas.Children.Remove(arrow);
            if (boardViewIndex == selectedIndexRight)
            {

                // add to move notes - if there has been no moves/startpos then do nothing
                string squareFrom = Engine.Tools.IndexToSquare((byte)selectedBitboardIndex);
                string squareTo = Engine.Tools.IndexToSquare((byte)index);
                string Comment = $"[{squareFrom},{squareTo},SolidColour]";
                if (Game.movesPlayed.Count != 0)
                {
                    if (Game.movesPlayed[game.currentMoveNo].Comments.Contains(Comment))
                    {
                        Game.movesPlayed[game.currentMoveNo].Comments.Remove(Comment);
                        DrawBoard();
                        DrawPieces(game.board);
                        DrawComments(Game.movesPlayed[game.currentMoveNo].Comments);
                        if (selectedIndexLeft != -1)
                        {
                            DrawAvailibleMoves(LegalMoves);
                        }
                        selectedIndexRight = -1;
                        return;
                    }

                    Game.movesPlayed[game.currentMoveNo].Comments.Add(Comment);
                }
                squares[boardViewIndex].border.Background = User.Settings.ArrowColour.ColourBrush;
            }

            else if (selectedIndexRight != -1)
            {
                // create the arrow comment
                // Comment in the format of "[SqaureFrom,SquareTo,Arrow/SolidColour]" 
                string squareFrom = Engine.Tools.IndexToSquare((byte)selectedBitboardIndex);
                string squareTo = Engine.Tools.IndexToSquare((byte)index);
                string Comment = $"[{squareFrom},{squareTo},Arrow]";
                // we are makeing sure that the current game is greater than zero otherwise the comment is not related to any move
                if (Game.movesPlayed.Count != 0)
                {
                    // check if the user is removing the comment
                    if (Game.movesPlayed[game.currentMoveNo].Comments.Contains(Comment))
                    {
                        Game.movesPlayed[game.currentMoveNo].Comments.Remove(Comment);
                        DrawBoard();
                        DrawPieces(game.board);
                        DrawComments(Game.movesPlayed[game.currentMoveNo].Comments);
                        selectedIndexLeft = -1;
                        selectedIndexRight = -1;
                        return;
                    }

                    // else add the arrow comment and the new arrow
                    Game.movesPlayed[game.currentMoveNo].Comments.Add(Comment);
                }

                double startX = boardCanvas.Width - (Canvas.GetRight(squares[selectedIndexRight].border) + SquareLength / 2);
                double startY = boardCanvas.Height - (Canvas.GetBottom(squares[selectedIndexRight].border) + SquareLength / 2);
                double endX = boardCanvas.Width - (Canvas.GetRight(squares[boardViewIndex].border) + SquareLength / 2);
                double endY = boardCanvas.Height - (Canvas.GetBottom(squares[boardViewIndex].border) + SquareLength / 2);
                PointCollection arrowPoints = calculateArrowPoints(startX, startY, endX, endY);
                Polygon arrow1 = new Polygon();
                arrow1.Points = arrowPoints;
                arrow1.Fill = User.Settings.ArrowColour.ColourBrush;

                boardCanvas.Children.Add(arrow1);
            }

            selectedIndexRight = -1;
        }

        public abstract void OnLeftMouseUp(object sender, MouseEventArgs e);


        public void ShowPromotion(Move move, int index)
        {
            string colour = game.board.WhiteToPlay ? "w" : "b";
            promotionWindow = new Promotion(move, colour);

            promotionWindow.promotionStack.MouseUp += PromotionStack_MouseUp;

            promotionWindow.Height = SquareLength * 4;
            promotionWindow.Width = SquareLength;

            if (index < 8) // promotion at the bottom of the screen
            {
                index += 8 * 3; // set it three sqaures higher so it is still on the board
            }

            Canvas.SetTop(promotionWindow, (7 - (index / 8)) * SquareLength); // set the x and y of the screen to be originating from the square
            Canvas.SetRight(promotionWindow, (index % 8) * SquareLength);
            boardCanvas.Children.Add(promotionWindow);
        }

        public void RemovePromotion()
        {
            try
            {
                boardCanvas.Children.Remove(promotionWindow);
                promotionWindow = null;
            }
            catch
            {
                return;
            }
        }

        protected void PromotionStack_MouseUp(object sender, MouseButtonEventArgs e)
        {

            game.MakeMove(promotionWindow.move);
            DrawBoard();
            DrawPieces(game.board);
            selectedIndexLeft = -1;
        }

        public int calculateBitBoardIndexAtMousePosition()
        {
            Point point = Mouse.GetPosition(boardCanvas);
            int y = (int)(point.Y / SquareLength);
            int x = (int)(point.X / SquareLength);
            int index = ((x + y * 8));
            return getBoardViewIndex(index, !whiteView);
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (LeftMouse)
            {
                Point position = Mouse.GetPosition(boardCanvas);
                Canvas.SetLeft(MovingPiece, position.X - MovingPiece.Width / 2);
                Canvas.SetTop(MovingPiece, position.Y - MovingPiece.Height / 2);
            }

            if (RightMouse)
            {
                double startX = boardCanvas.Width - (Canvas.GetRight(squares[selectedIndexRight].border) + SquareLength / 2);
                double startY = boardCanvas.Height - (Canvas.GetBottom(squares[selectedIndexRight].border) + SquareLength / 2);
                Point mousePos = Mouse.GetPosition(boardCanvas);
                PointCollection arrowPoints = calculateArrowPoints(startX, startY, mousePos.X, mousePos.Y);
                arrow.Points = arrowPoints;
            }
        }

        public List<int> filterMoves(List<Move> moves, int targetSource)
        {
            List<int> destinations = new List<int>();
            foreach (Move move in moves)
            {
                if (move.startIndex == targetSource)
                {
                    destinations.Add(move.destinationIndex);
                }
            }

            return destinations;
        }

        public int getBitboardIndex(int clickIndex, bool whiteView)
        {
            if (whiteView) return clickIndex;

            return (Math.Abs(clickIndex - 63));
        }

        public int getBoardViewIndex(int clickIndex, bool whiteView)
        {
            if (whiteView) return clickIndex;

            return (Math.Abs(clickIndex - 63));
        }

        public void DrawBoard()
        {
            boardCanvas.Children.Clear();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int index = ((j + i * 8));
                    squares[index] = new Square();
                    squares[index].border = new Border
                    {
                        Tag = index,
                        Height = SquareLength,
                        Width = SquareLength,
                        BorderBrush = Brushes.Red
                    };
                    squares[index].border.MouseLeftButtonDown += OnLeftMouseDown;
                    squares[index].border.MouseRightButtonDown += OnRightMouseDown;

                    squares[index].border.BorderThickness = new Thickness();

                    if ((index + (-1 * (i))) % 2 == 0)
                    {
                        squares[index].border.Background = User.Settings.LightSquareColour.ColourBrush;
                    }
                    else
                    {
                        squares[index].border.Background = User.Settings.DarkSquareColour.ColourBrush;
                    }
                    boardCanvas.Children.Add(squares[index].border);
                    Canvas.SetRight(squares[index].border, j * squares[index].border.Height);
                    Canvas.SetBottom(squares[index].border, i * squares[index].border.Height);
                }
            }

        }

        public void clearBoard()
        {
            boardCanvas.Children.RemoveRange(64, boardCanvas.Children.Count - 64);
            foreach (Square i in squares)
            {
                i.border.Child = null;
            }
        }

        public void DrawPieces(Engine.Bitboard b)
        {

            this.clearBoard();
            ulong allPieces = b.Board[0] | b.Board[1];
            while (allPieces != 0)
            {
                ulong pieceLoc = BitBoardTools.popLSB(ref allPieces);
                int Piecetype = Bitboard.getPieceType(b.Board, pieceLoc);
                char isWhite = (pieceLoc & b.Board[0]) != 0 ? 'w' : 'b';

                int index = whiteView ? (BitBoardTools.BitboardToIndex(pieceLoc)) : (63 - BitBoardTools.BitboardToIndex(pieceLoc));

                squares[index].border.Child = createPieceImage(Piecetype, isWhite);
            }

            DrawLastMove();
        }

        public virtual void DrawAvailibleMoves(List<int> moves)
        {
            foreach (Square square in squares)
            {
                square.border.BorderThickness = new Thickness(0);
            }

            foreach (int move in moves)
            {
                int index = getBoardViewIndex(move, whiteView);
                squares[index].border.BorderThickness = new Thickness(2, 2, 2, 2);
                squares[index].border.BorderBrush = User.Settings.AvailibleMoveColour.ColourBrush;
            }
        }

        public virtual void DrawLastMove()
        {
            if (game.currentMoveNo != 0)
            {
                Move LastMove = CurrentGame.movesPlayed[game.currentMoveNo].Move;
                int StartIndex = getBoardViewIndex(LastMove.startIndex, whiteView);
                int DestinationIndex = getBoardViewIndex(LastMove.destinationIndex, whiteView);
                squares[StartIndex].border.Background = User.Settings.LastMoveColour.ColourBrush;
                squares[DestinationIndex].border.Background = User.Settings.LastMoveColour.ColourBrush;
            }
        }

        public void DrawComments(List<string> comments)
        {
            foreach (string comment in comments)
            {
                // remove [ ]
                string Comment;
                Comment = comment.Substring(1, comment.Length - 2);

                // split into its components
                string[] commentComponents = Comment.Split(',');
                string startSqaure = commentComponents[0];
                string endSqaure = commentComponents[1];
                string Type = commentComponents[2];

                if (Type == "Arrow")
                {
                    // get the 
                    int startSquareIndex = getBoardViewIndex(Engine.Tools.SquareToIndex(startSqaure), whiteView);
                    int endSquareIndex = getBoardViewIndex(Engine.Tools.SquareToIndex(endSqaure), whiteView);
                    double startX = boardCanvas.Width - (Canvas.GetRight(squares[startSquareIndex].border) + SquareLength / 2);
                    double startY = boardCanvas.Height - (Canvas.GetBottom(squares[startSquareIndex].border) + SquareLength / 2);
                    double endX = boardCanvas.Width - (Canvas.GetRight(squares[endSquareIndex].border) + SquareLength / 2);
                    double endY = boardCanvas.Height - (Canvas.GetBottom(squares[endSquareIndex].border) + SquareLength / 2);
                    PointCollection arrowPoints = calculateArrowPoints(startX, startY, endX, endY);

                    Polygon arrow1 = new Polygon();
                    arrow1.Points = arrowPoints;
                    arrow1.Fill = arrow1.Fill = User.Settings.ArrowColour.ColourBrush;

                    boardCanvas.Children.Add(arrow1);
                }

                else if (Type == "SolidColour")
                {
                    squares[getBoardViewIndex(Engine.Tools.SquareToIndex(startSqaure), whiteView)].border.Background = User.Settings.ArrowColour.ColourBrush;
                }
            }
        }

        public PointCollection calculateArrowPoints(double startX, double startY, double endX, double endY)
        {
            Point end = new Point(endX, endY);
            Point start = new Point(startX, startY);

            // Code from https://stackoverflow.com/questions/16714011/how-to-draw-an-arrow-in-wpf-programatically

            Vector direction = end - start;

            Vector normalizedDirection = direction;
            normalizedDirection.Normalize();

            Vector normalizedlineWidenVector = new Vector(-normalizedDirection.Y, normalizedDirection.X); // Rotate by 90 degrees
            Vector lineWidenVector = normalizedlineWidenVector * 1.5;

            double lineLength = direction.Length;

            double defaultArrowLength = 3 * 3.73205081;

            // Prepare usedArrowLength
            // if the length is bigger than 1/3 (_maxArrowLengthPercent) of the line length adjust the arrow length to 1/3 of line length

            double usedArrowLength;
            if (lineLength * 0.3 < defaultArrowLength)
                usedArrowLength = lineLength * 0.3;
            else
                usedArrowLength = defaultArrowLength;

            // Adjust arrow thickness for very thick lines
            double arrowWidthFactor = 6;

            Vector arrowWidthVector = normalizedlineWidenVector * arrowWidthFactor;
            Point endArrowCenterPosition = end - (normalizedDirection * usedArrowLength);

            PointCollection pointCollection = new PointCollection(7)
            {
                end,
                endArrowCenterPosition + arrowWidthVector,
                endArrowCenterPosition + lineWidenVector,
                start + lineWidenVector,
                start - lineWidenVector,
                endArrowCenterPosition - lineWidenVector,
                endArrowCenterPosition - arrowWidthVector
            };

            return pointCollection;
        }

        public static SVGImage.SVG.SVGImage createPieceImage(int PieceType, char isWhite)
        {
            string filePath = assetsPath + $"\\Pieces\\{User.Settings.PieceFolder}\\{isWhite + pieceNames[PieceType - 2]}.svg";
            SVGImage.SVG.SVGImage piece = new SVGImage.SVG.SVGImage();
            piece.Width = SquareLength - 2;
            piece.Height = SquareLength - 2;
            piece.HorizontalContentAlignment = HorizontalAlignment.Center;
            piece.VerticalContentAlignment = VerticalAlignment.Center;
            piece.VerticalAlignment = VerticalAlignment.Center;
            piece.HorizontalAlignment = HorizontalAlignment.Center;
            piece.SizeType = SVGImage.SVG.SVGImage.eSizeType.ContentToSizeNoStretch;

            piece.UriSource = new Uri(filePath);


            return piece;
        }
    }
}
