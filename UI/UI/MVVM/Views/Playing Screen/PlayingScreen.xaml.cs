using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.MVVM.Models;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for PlayingScreen.xaml
    /// </summary>
    public partial class PlayingScreen : UserControl
    {
        public static GameInformationView gameinfo;
        public PlayingScreen()
        {
            InitializeComponent();
            LocalGameInformationView._FlipButton.Click += FlipBoard_Click;
            LocalGameInformationView._FirstMove.Click += BoardStateNavigation_Click;
            LocalGameInformationView._LastMove.Click += BoardStateNavigation_Click;
            LocalGameInformationView._NextMove.Click += BoardStateNavigation_Click;
            LocalGameInformationView._PreviousMove.Click += BoardStateNavigation_Click;
            gameinfo = LocalGameInformationView;
            PlayerInfoView.Game = (CurrentGame)LocalBoardView.viewModel.game;
            ((CurrentGame)(LocalBoardView.viewModel.game)).playerInfo = PlayerInfoView;
            MainMenu.ExitButton.MouseUp += ShowMenu;
            GameOverOverlay.OkButton.MouseUp += hideGameOver;
        }

        private void ShowMenu(object sender, MouseButtonEventArgs e)
        {
            if (MainMenu.Visibility == Visibility.Hidden)
                MainMenu.Visibility = Visibility.Visible;

            else
                MainMenu.Visibility = Visibility.Hidden;

            e.Handled = true;
        }

        private void FlipBoard_Click(object sender, RoutedEventArgs e)
        {
            LocalBoardView.viewModel.WhiteView = !LocalBoardView.viewModel.WhiteView;
        }

        private void BoardStateNavigation_Click(object sender, RoutedEventArgs e)
        {
            string senderTag = ((Button)sender).Tag.ToString();
            try
            {
                switch (senderTag)
                {
                    case "0":
                        LocalBoardView.viewModel.game.currentMoveNo = 0;
                        break;
                    case "1":
                        LocalBoardView.viewModel.game.currentMoveNo -= 1;
                        break;
                    case "2":
                        LocalBoardView.viewModel.game.currentMoveNo += 1;

                        break;
                    case "3":
                        LocalBoardView.viewModel.game.currentMoveNo = CurrentGame.movesPlayed.Count - 1;

                        break;
                }
            }
            catch
            {
                // do nothing
            }
        }

        public void displayGameOver(string result)
        {

            GameOverOverlay.changeMesage(result);
            GameOverOverlay.Visibility = Visibility.Visible;
            PlayerInfoView.endgame();
        }

        void hideGameOver(object sender, RoutedEventArgs e)
        {
            GameOverOverlay.Visibility = Visibility.Hidden;
        }

    }
}
