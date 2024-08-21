using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UI.MVVM.Models;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            NewGameView newGameView = new();
            newGameView.ShowDialog();
        }

        private void PlayOnline_Click(object sender, RoutedEventArgs e)
        {
            // show the server browser
        }

        private void Analysis_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = App.Current.MainWindow;
            AnalysisScreen usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
            Thread.Sleep(500);
            ((AnalysisGame)usercontrol.LocalBoardView.viewModel.game).bot.SendMove();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = App.Current.MainWindow;
            Settings usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
        }

        private void Exit_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindow.GetWindow(this).Close();
        }
    }
}
