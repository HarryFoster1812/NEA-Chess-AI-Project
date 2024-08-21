using Engine;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using UI.MVVM.Models;
using UI.MVVM.Models.Players;

namespace UI.MVVM.ViewModels
{
    public class BoardPreiewModel : BoardViewModelBase
    {

        static Bitboard b = Tools.FENtoBitboard("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1");


        public BoardPreiewModel(Canvas root)
        {

            boardCanvas = root;
            squares = new Square[64];
            assetsPath = System.IO.Path.Combine(Directory.GetParent(appFolderPath).Parent.Parent.FullName, "Assets");
            SquareLength = boardCanvas.Height / 8;
            UpdatePreview();
        }

        public void UpdatePreview()
        {
            DrawBoard();
            DrawPieces(b);
            DrawAvailibleMoves(new List<int> { 43, 35 });
            DrawLastMove();
            DrawComments(new List<string> { "[g8,f6,Arrow]" });
        }

        public override void DrawLastMove()
        {
            squares[11].border.Background = User.Settings.LastMoveColour.ColourBrush;
            squares[27].border.Background = User.Settings.LastMoveColour.ColourBrush;
        }

        public override void OnLeftMouseDown(object sender, MouseEventArgs e)
        {

        }

        public override void OnLeftMouseUp(object sender, MouseEventArgs e)
        {

        }
    }
}
