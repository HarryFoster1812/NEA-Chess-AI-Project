using System.Windows.Media;

namespace UI.MVVM.Models
{
    public struct Colour
    {
        private static BrushConverter converter = new BrushConverter();
        private string _HexColourString;
        private Color _color;

        public string HexColourString
        {
            get { return _HexColourString; }
            set
            {
                _HexColourString = value;
                _ColourBrush = (Brush)converter.ConvertFromString(value);
                _color = ConvertBrushToColor(_ColourBrush);
            }
        }

        public Color ColorPickerData
        {
            get { return _color; }
            set
            {
                _color = value;
                _ColourBrush = ConvertColorToBrush(value);
                _HexColourString = converter.ConvertToString(value);
            }
        }

        private Brush _ColourBrush;

        public Brush ColourBrush
        {
            get { return _ColourBrush; }

            set
            {
                _ColourBrush = value;
                _HexColourString = converter.ConvertToString(value);
                _color = ConvertBrushToColor(value);

            }

        }

        public Colour(string hex)
        {
            _HexColourString = hex;
            _ColourBrush = (Brush)converter.ConvertFromString(hex);
            _color = ConvertBrushToColor(_ColourBrush);

        }

        public Colour(Brush colourBrush)
        {
            _HexColourString = converter.ConvertToString(colourBrush);
            _ColourBrush = colourBrush;
            _color = ConvertBrushToColor(colourBrush);
        }

        public Colour(Color colour)
        {
            _color = colour;
            _ColourBrush = ConvertColorToBrush(_color);
            _HexColourString = converter.ConvertToString(_ColourBrush);
        }

        public static Color ConvertBrushToColor(Brush brush)
        {
            SolidColorBrush newBrush = (SolidColorBrush)brush;
            return newBrush.Color;
        }
        public static Brush ConvertColorToBrush(Color color)
        {
            SolidColorBrush newBrush = new SolidColorBrush(color);
            return (Brush)newBrush;
        }

    }

    public class UISettings
    {
        public Colour _BackgroundColour;
        public Colour _ButtonColour;
        public Colour _TextBoxColour;
        public Colour _LightSquareColour;
        public Colour _DarkSquareColour;
        public Colour _PanelColour;
        public Colour _TextColour;
        public Colour _LastMoveColour;
        public Colour _ArrowColour;
        public Colour _AvailibleMoveColour;

        public Colour BackgroundColour { get { return _BackgroundColour; } set { _BackgroundColour = value; } }
        public Colour ButtonColour { get { return _ButtonColour; } set { _ButtonColour = value; } }
        public Colour TextBoxColour { get { return _TextBoxColour; } set { _TextBoxColour = value; } }
        public Colour LightSquareColour { get { return _LightSquareColour; } set { _LightSquareColour = value; } }
        public Colour DarkSquareColour { get { return _DarkSquareColour; } set { _DarkSquareColour = value; } }
        public Colour PanelColour { get { return _PanelColour; } set { _PanelColour = value; } }
        public Colour TextColour { get { return _TextColour; } set { _TextColour = value; } }
        public Colour LastMoveColour { get { return _LastMoveColour; } set { _LastMoveColour = value; } }
        public Colour ArrowColour { get { return _ArrowColour; } set { _ArrowColour = value; } }
        public Colour AvailibleMoveColour { get { return _AvailibleMoveColour; } set { _AvailibleMoveColour = value; } }

        internal string PieceFolder;

        internal int DefaultEngine;

        public UISettings()
        {
            resetDarkMode();
            resetBoard();
            PieceFolder = "_Default";
        }


        // _Default
        internal void resetDarkMode()
        {
            _BackgroundColour = new Colour("#121212");
            _TextColour = new Colour(Brushes.White);
            _TextBoxColour = new Colour("#76737E");
            _ButtonColour = new Colour("#76737E");
            _PanelColour = new Colour("#3F3F3F");
        }

        internal void resetLightMode()
        {
            _BackgroundColour = new Colour("#F5F5F5");
            _TextColour = new Colour(Brushes.Black);
            _TextBoxColour = new Colour("#FAF8FF");
            _ButtonColour = new Colour("#CCCCCC");
            _PanelColour = new Colour("#979797");
        }

        internal void resetBoard()
        {
            _LastMoveColour = new Colour(Brushes.BlueViolet);
            _AvailibleMoveColour = new Colour(Brushes.Red);
            _ArrowColour = new Colour(Brushes.Red);
            _LightSquareColour = new Colour(Brushes.Wheat);
            _DarkSquareColour = new Colour(Brushes.Green);
        }

    }
}
