using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UI.MVVM.Models;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : UserControl
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        private void CloseThreads()
        {
            Bot.connectionEnded = true;
            OnlinePlayer.connectionEnded = true;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = App.Current.MainWindow;
            HomeView usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
            CloseThreads();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = App.Current.MainWindow;
            Settings usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
            CloseThreads();
        }

        private void Analysis_Click(object sender, RoutedEventArgs e)
        {
            CloseThreads();
            Window mainWindow = App.Current.MainWindow;
            AnalysisScreen usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
            Thread.Sleep(500);
            ((AnalysisGame)usercontrol.LocalBoardView.viewModel.game).bot.SendMove();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            NewGameView usercontrol = new();
            usercontrol.ShowDialog();
            CloseThreads();
        }
    }
}
