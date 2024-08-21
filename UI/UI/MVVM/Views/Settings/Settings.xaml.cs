using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.MVVM.Models;
using UI.MVVM.Models.Players;
using Xceed.Wpf.Toolkit;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>


    public partial class Settings : UserControl
    {
        List<TextBlock> textElements = new List<TextBlock>();

        public Settings()
        {
            InitializeComponent();
            ExtractAllTextBlocks(MainGrid.Children);
            GetPieceFolders();
        }


        private void GetPieceFolders()
        {
            string RootFolder = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string PiecesPath = System.IO.Path.Combine(new string[] { RootFolder, "Assets", "Pieces" });

            string[] Folders = Directory.GetDirectories(PiecesPath);
            int selected = 0;
            List<string> acceptedFolders = new List<string>();
            for (int i = 0; i < Folders.Length; i++)
            {
                if (FolderContainsPieces(Folders[i]))
                {
                    Folders[i] = Folders[i].Split('\\')[Folders[i].Split('\\').Length - 1];
                    // make sure the folder contains all of the right pieces



                    if (Folders[i] == User.Settings.PieceFolder)
                    {
                        selected = i;
                    }
                    acceptedFolders.Add(Folders[i]);
                }

            }
            PieceFolders.ItemsSource = acceptedFolders;
            PieceFolders.SelectedIndex = selected;
        }

        private bool FolderContainsPieces(string folername)
        {
            return true;
        }

        private void ExtractAllTextBlocks(UIElementCollection Collection)
        {
            foreach (UIElement child in Collection)
            {
                if (child is TextBlock)
                {
                    textElements.Add((TextBlock)child);
                }

                else if (child is Grid)
                {
                    ExtractAllTextBlocks(((Grid)child).Children);
                }

                else if (child is StackPanel)
                {
                    ExtractAllTextBlocks(((StackPanel)child).Children);
                }
            }
        }

        private void UpdateScreenColours()
        {

            MainGrid.Background = User.Settings.BackgroundColour.ColourBrush;
            PanelExample.Background = User.Settings.PanelColour.ColourBrush;
            foreach (TextBlock i in textElements)
            {
                i.Foreground = User.Settings.TextColour.ColourBrush;
            }
            ManageEnginesButton.Background = User.Settings.ButtonColour.ColourBrush;
        }

        private void BackGround_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.BackgroundColour = pickedColour;
            MainGrid.Background = User.Settings.BackgroundColour.ColourBrush;
        }

        private void Panel_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.PanelColour = pickedColour;
            PanelExample.Background = User.Settings.PanelColour.ColourBrush;
        }

        private void Text_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.TextColour = pickedColour;
            foreach (TextBlock i in textElements)
            {
                i.Foreground = User.Settings.TextColour.ColourBrush;
            }
        }

        private void PieceFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            string selectedValue = combo.SelectedItem.ToString();
            User.Settings.PieceFolder = selectedValue;
            Boardpreview.viewModel.UpdatePreview();
        }

        private void Preset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            string selectedValue = ((ComboBoxItem)combo.SelectedItem).Content.ToString();
            if (selectedValue == "Dark Mode") User.Settings.resetDarkMode();
            else if (selectedValue == "Light Mode") User.Settings.resetLightMode();
            UpdateScreenColours();
        }

        private void LightSquare_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.LightSquareColour = pickedColour;
            Boardpreview.viewModel.UpdatePreview();
        }

        private void DarkSquare_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.DarkSquareColour = pickedColour;
            Boardpreview.viewModel.UpdatePreview();
        }

        private void Available_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.AvailibleMoveColour = pickedColour;
            Boardpreview.viewModel.UpdatePreview();
        }

        private void Arrow_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.ArrowColour = pickedColour;
            Boardpreview.viewModel.UpdatePreview();
        }

        private void Last_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.LastMoveColour = pickedColour;
            Boardpreview.viewModel.UpdatePreview();
        }

        private void Button_Select(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            Colour pickedColour = new Colour((Color)colorPicker.SelectedColor);
            User.Settings.ButtonColour = pickedColour;
            ManageEnginesButton.Background = User.Settings.ButtonColour.ColourBrush;
        }

        private void ManageEnginesButton_Click(object sender, RoutedEventArgs e)
        {
            ManageEnginesPopup window = new ManageEnginesPopup();
            window.ShowDialog();
        }

        private void Exit_Click(object sender, MouseButtonEventArgs e)
        {
            Window mainWindow = App.Current.MainWindow;
            HomeView usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
        }
    }
}
