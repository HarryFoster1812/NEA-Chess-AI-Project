using System;
using System.Windows;
using UI.MVVM.Models.Players;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Bot.connectionEnded = true;
            OnlinePlayer.connectionEnded = true;
            Application.Current.Shutdown();
        }
    }
}
