using Engine;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for Promotion.xaml
    /// </summary>

    public partial class Promotion : UserControl
    {
        internal char promotionClick = 'q';
        internal StackPanel promotionStack;
        internal Move move;
        internal string colour;

        public Promotion(Move move, string colour)
        {
            InitializeComponent();
            promotionStack = promotion;
            this.move = move;
            this.colour = colour;
            setImageSource();
        }

        private void PieceClick(object sender, MouseEventArgs e)
        {
            char type = ((string)((SVGImage.SVG.SVGImage)sender).Tag)[0];
            promotionClick = type;
            genMove();
        }

        private void setImageSource()
        {

            string filePath = User.assetsFolder + $"\\Pieces\\{User.Settings.PieceFolder}";

            foreach (SVGImage.SVG.SVGImage i in promotion.Children.OfType<SVGImage.SVG.SVGImage>())
            {
                i.HorizontalContentAlignment = HorizontalAlignment.Center;
                i.VerticalContentAlignment = VerticalAlignment.Center;
                i.UriSource = new Uri(filePath + $"\\{colour + ((string)i.Tag).ToUpper()}.svg");
            }
        }

        private void genMove()
        {
            byte from = move.startIndex;
            byte to = move.destinationIndex;
            byte flag = 0;
            switch (promotionClick)
            {
                case 'q':
                    flag = 0b0111;
                    break;

                case 'r':
                    flag = 0b0110;
                    break;

                case 'b':
                    flag = 0b0101;
                    break;

                case 'n':
                    flag = 0b0100;
                    break;
            }

            move = new Move(from, to, flag);
        }

    }
}
