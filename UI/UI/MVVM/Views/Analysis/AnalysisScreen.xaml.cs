using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.MVVM.Models;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for AnalysisScreen.xaml
    /// </summary>
    public partial class AnalysisScreen : UserControl
    {
        public static EngineAnalysisView analysisView;
        public AnalysisScreen()
        {
            InitializeComponent();
            analysisView = EngineInfoView;
            LocalGameInformationView._FlipButton.Click += FlipBoard_Click;
            LocalGameInformationView._FirstMove.Click += BoardStateNavigation_Click;
            LocalGameInformationView._LastMove.Click += BoardStateNavigation_Click;
            LocalGameInformationView._NextMove.Click += BoardStateNavigation_Click;
            LocalGameInformationView._PreviousMove.Click += BoardStateNavigation_Click;
            MainMenu.ExitButton.MouseUp += ShowMenu;
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
                        ((AnalysisGame)LocalBoardView.viewModel.game).currentMoveNo = 0;
                        break;
                    case "1":
                        ((AnalysisGame)LocalBoardView.viewModel.game).currentMoveNo -= 1;
                        break;
                    case "2":
                        ((AnalysisGame)LocalBoardView.viewModel.game).currentMoveNo += 1;
                        break;
                    case "3":
                        ((AnalysisGame)LocalBoardView.viewModel.game).currentMoveNo = AnalysisGame.movesPlayed.Count - 1;

                        break;
                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}
