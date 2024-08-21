using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI.MVVM.Models;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for PlayerInfoView.xaml
    /// </summary>
    public partial class PlayerInfoView : UserControl
    {
        bool timeUnlimited = true;

        bool competitive = false;

        private CurrentGame game;

        DispatcherTimer clockTimer;
        Stopwatch timer = new Stopwatch();

        TextBlock[] clocks;

        internal CurrentGame Game
        {
            get { return game; }
            set
            {
                game = value;
                if (game.players[0].timeOnClock != TimeSpan.MaxValue)
                {
                    timeUnlimited = false;
                    showClocks();
                }
            }
        }

        public PlayerInfoView()
        {
            InitializeComponent();
            clocks = new TextBlock[] { player1Clock, player2Clock };
        }

        public void switchClocks()
        { // adds the clock increment and starts the clock on the first move
            if (!timeUnlimited)
            {
                Player CurrentPlayer = game.players[game.board.WhiteToPlay ? 1 : 0];
                TextBlock ClockTextBlock = clocks[game.board.WhiteToPlay ? 1 : 0];
                CurrentPlayer.timeOnClock += TimeSpan.FromMilliseconds(CurrentPlayer.clockIncrementMS);
                ClockTextBlock.Text = $"{CurrentPlayer.timeOnClock.Hours.ToString("D2")}:{CurrentPlayer.timeOnClock.Minutes.ToString("D2")}:{CurrentPlayer.timeOnClock.Seconds.ToString("D2")}:{CurrentPlayer.timeOnClock.Milliseconds.ToString("D2")}";
                if (game.MovesPlayedCount == 2)
                {
                    ToggleTimer();
                }
            }
        }

        private void ToggleTimer()
        {
            if (clockTimer.IsEnabled)
            {
                clockTimer.Stop();
                timer.Stop();
                timer.Reset();
            }
            else
            {
                timer.Start();
                clockTimer.Start();
            }
        }

        public void showClocks()
        {
            clocks[0].Visibility = Visibility.Visible;
            TimeSpan player1clock = game.players[0].timeOnClock;
            clocks[0].Text = $"{player1clock.Hours.ToString("00")}:{player1clock.Minutes.ToString("D2")}:{player1clock.Seconds.ToString("D2")}:{player1clock.Milliseconds.ToString("D2")}";

            clocks[1].Visibility = Visibility.Visible;
            TimeSpan player2clock = game.players[1].timeOnClock;
            clocks[1].Text = $"{player2clock.Hours.ToString("D2")}:{player2clock.Minutes.ToString("D2")}:{player2clock.Seconds.ToString("D2")}:{player2clock.Milliseconds.ToString("D2")}";

            clockTimer = new DispatcherTimer();
            clockTimer.Tick += updateClock;
            clockTimer.Interval = TimeSpan.FromMilliseconds(10);
        }

        public void updateClock(object sender, EventArgs e)
        {
            // change time on clock
            bool isWhite = (game.MovesPlayedCount % 2) == 1;
            Player CurrentPlayer = game.players[isWhite ? 0 : 1];
            TextBlock ClockTextBlock = clocks[isWhite ? 0 : 1];
            CurrentPlayer.timeOnClock -= timer.Elapsed;

            ClockTextBlock.Text = $"{CurrentPlayer.timeOnClock.Hours.ToString("D2")}:{CurrentPlayer.timeOnClock.Minutes.ToString("D2")}:{CurrentPlayer.timeOnClock.Seconds.ToString("D2")}:{(CurrentPlayer.timeOnClock.Milliseconds / 10).ToString("00")}";
            timer.Restart();

            if (CurrentPlayer.timeOnClock.TotalMilliseconds <= 0)
            {
                clockTimer.Stop();
                timer.Stop();
                ((CurrentGame)game).endGame(isWhite ? 1 : 0);
                CurrentPlayer.timeOnClock = new TimeSpan(0);
                ClockTextBlock.Text = $"{CurrentPlayer.timeOnClock.Hours.ToString("D2")}:{CurrentPlayer.timeOnClock.Minutes.ToString("D2")}:{CurrentPlayer.timeOnClock.Seconds.ToString("D2")}:{(CurrentPlayer.timeOnClock.Milliseconds / 10).ToString("00")}";

            }
        }

        public void ChangePlayerInfo(int playerNo, string UserName, int Elo, string flag)
        {
            if (playerNo == 0)
            {
                //player1Colour;
                //player1Flag;
                player1Name.Text = UserName;
                player1Name.ToolTip = UserName;
                player1Rating.Text = "Rating: " + Elo.ToString();
            }

            else
            {
                player2Rating.Text = "Rating: " + Elo.ToString();
                player2Name.Text = UserName;
            }

        }

        public void CompetitiveMode()
        {
            TakeBackButton.IsEnabled = false;
            competitive = true;
        }

        private void Resign_Click(object sender, RoutedEventArgs e)
        {
            bool isWhite = (game.MovesPlayedCount % 2) == 1;
            Player CurrentPlayer = game.players[isWhite ? 0 : 1];
            Player OtherPlayer = game.players[isWhite ? 1 : 0];

            if (CurrentPlayer is Player) // it is the players turn
            {
                ((CurrentGame)game).endGame(isWhite ? 1 : 0);
            }
            else if (OtherPlayer is Player)
            {
                game.endGame(isWhite ? 1 : 0);
            }
        }

        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            // get current player
            bool isWhite = (game.MovesPlayedCount % 2) == 1;
            Player CurrentPlayer = game.players[isWhite ? 0 : 1];
            Player OtherPlayer = game.players[isWhite ? 1 : 0];

            if (CurrentPlayer is Player) // it is the players turn
            {
                if (OtherPlayer is Player)
                {
                    ((CurrentGame)game).endGame(2);
                }

                else
                { // the other player is the engine 
                    if (competitive)
                    { // reject if it is competitive
                        return;
                    }
                    ((Bot)OtherPlayer).SendRequest("draw");
                }
            }

            else
            { // it is not the players turn 
                if (competitive)
                { // reject if it is competitive
                    return;
                }
                    ((Bot)OtherPlayer).SendRequest("draw");
            }
        }

        private void TakeBack_Click(object sender, RoutedEventArgs e)
        {
            Player CurrentPlayer = game.players[game.board.WhiteToPlay ? 0 : 1];
            Player OtherPlayer = game.players[game.board.WhiteToPlay ? 1 : 0];

            if (CurrentPlayer is Bot)
            { // the player can not request a takeback when it is not their turn
                return;
            }

            if (CurrentPlayer is Player && OtherPlayer is Bot)
            {
                CurrentPlayer.SendRequest("TakeBack1");
                return;
            }

            if (CurrentPlayer is Player && OtherPlayer is Player)
            {
                CurrentPlayer.SendRequest("TakeBack2");
                return;
            }


        }

        private void Analysis_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = App.Current.MainWindow;
            List<UIMove> moves = UI.MVVM.Models.Game.movesPlayed;
            Grid[] gameInformationView = GameInformationView.movesStackPanel.Children.OfType<Grid>().ToArray();
            GameInformationView.movesStackPanel.Children.Clear();
            AnalysisScreen usercontrol = new();
            usercontrol.Height = 450;
            usercontrol.Width = 800;
            ((Viewbox)mainWindow.Content).Child = usercontrol;
            UI.MVVM.Models.Game.movesPlayed = moves;


            for (int i = 0; i < gameInformationView.Length; i++)
            {
                // add a new event handler to be handled by the analysis board
                foreach (Button j in gameInformationView[i].Children.OfType<Button>())
                {
                    j.Click += (sender, e) =>
                    {
                        ((AnalysisGame)usercontrol.LocalBoardView.viewModel.game).changeBoardViewToMove((int)((Button)sender).Tag);
                    };
                }
                // aad each element to the stack panel
                GameInformationView.movesStackPanel.Children.Add(gameInformationView[i]);
            }

            // wait for the bot to set up
            Thread.Sleep(500);
            // start the analysis
            ((AnalysisGame)usercontrol.LocalBoardView.viewModel.game).bot.SendMove();
        }

        public void ShowTakeBackRequest() // for online
        {

        }

        public void ShowDrawRequest() // for online
        {

        }

        public void endgame()
        {

            if (clockTimer != null && clockTimer.IsEnabled)
            {
                ToggleTimer();
            }

            ResignButton.IsEnabled = false;
            TakeBackButton.IsEnabled = false;
            DrawButton.IsEnabled = false;
            AnalysisButton.IsEnabled = true;
        }
    }
}
