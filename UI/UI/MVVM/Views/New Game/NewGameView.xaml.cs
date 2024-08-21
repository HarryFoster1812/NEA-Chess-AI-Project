using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.MVVM.Models;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for NewGameView.xaml
    /// </summary>
    public partial class NewGameView : Window
    {
        public NewGameView()
        {
            InitializeComponent();
            RoundTextBox.IsUndoEnabled = true;
        }



        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox playerSelect = (ComboBox)sender;
            Grid PlayerInformationContent;
            if (Grid.GetRow(playerSelect) == 0)
            {
                PlayerInformationContent = Player1Grid;
            }

            else
            {
                PlayerInformationContent = Player2Grid;
            }

            PlayerInformationContent.Children.Remove(PlayerInformationContent.Children[0]);

            if (playerSelect.SelectedIndex == 0)
            {
                PlayerInformationContent.Children.Add(new NewGamePlayerSelectView());
            }
            else
            {
                PlayerInformationContent.Children.Add(new NewGameBotSelectView());
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int player1Type = Player1Combo.SelectedIndex;
            int player2Type = Player2Combo.SelectedIndex;
            int[] playerTypes = { player1Type, player2Type };

            string player1Info = Player1Grid.Children[0].ToString();

            string player2Info = Player2Grid.Children[0].ToString();

            string[] playerArguments = { player1Info, player2Info };

            // validate the arguments

            // get the time control
            (TimeSpan, double) TimeControl = getTimeControl();

            if (TimeControl.Item1 == TimeSpan.Zero)
            { // the user did not enter any time
                MessageBox.Show("The playing time must be non-Zero");
                return;
            }

            // switch the playing screen
            Window mainWindow = App.Current.MainWindow;
            PlayingScreen usercontrol = new();
            usercontrol.Height = mainWindow.Height;
            usercontrol.Width = mainWindow.Width;
            ((Viewbox)mainWindow.Content).Child = usercontrol;

            string EventType = ((ComboBoxItem)EventCombo.SelectedItem).Content.ToString();
            string RoundNo = RoundTextBox.Text;
            DateTime date = ((DateTime)GameDate.SelectedDate).Date;
            string Site = SiteTextBox.Text;

            // create the game
            CurrentGame game = new CurrentGame(usercontrol.LocalBoardView.viewModel, EventType, int.Parse(RoundNo), Site);

            // set the game information panel
            usercontrol.LocalGameInformationView.ChangeGameinfo(EventType, Site, RoundNo, date);

            // get if player 1 was black or white (true - white , false black)
            bool player1White = getPlayer1Colour();

            // if player 1 is black then swap the player types and arguments
            if (!player1White)
            {
                playerTypes = new int[] { player2Type, player1Type };
                playerArguments = new string[] { player2Info, player1Info };
            }

            // create the players
            for (int i = 0; i < 2; i++)
            {
                if (playerTypes[i] == 0)
                { // it is a player 
                    game.players[i] = new Models.Players.Player(game, playerArguments[i]);
                    game.players[i].isWhite = i == 0;
                    game.players[i].timeOnClock = TimeControl.Item1;
                    game.players[i].clockIncrementMS = TimeControl.Item2;

                }
                else
                { // it is a bot 
                    int engineIndex = int.Parse(playerArguments[i].Split("Index:")[1].Split(' ')[0]);
                    string path = User.Engines[engineIndex].path;
                    string depth = playerArguments[i].Split("Level:")[1];
                    game.players[i] = new Models.Players.Bot(game, path, depth);
                    game.players[i].isWhite = i == 0;
                    game.players[i].timeOnClock = TimeControl.Item1;
                    game.players[i].clockIncrementMS = TimeControl.Item2;

                    if (i == 0)
                    { // it is the starting player
                        Thread.Sleep(200); // wait for the communication thread to start
                        game.players[i].SendMove(); // make the bot play the first move
                    }

                }
            }

            // set the game information
            usercontrol.LocalBoardView.viewModel.game = game;
            usercontrol.PlayerInfoView.Game = game;
            ((CurrentGame)(usercontrol.LocalBoardView.viewModel.game)).playerInfo = usercontrol.PlayerInfoView;

            // set the player information
            usercontrol.PlayerInfoView.ChangePlayerInfo(0, playerArguments[0], 800, "UAE");
            usercontrol.PlayerInfoView.ChangePlayerInfo(1, playerArguments[1], 1000, "UAE");

            if (((ComboBoxItem)EventCombo.SelectedItem).Content.ToString() == "Competitive")
            {
                usercontrol.PlayerInfoView.CompetitiveMode();
            }

            this.Close();

        }


        private (TimeSpan, double) getTimeControl()
        {
            UserControl selection = (UserControl)TimeControlSelectGrid.Children[0];
            string time = selection.ToString();

            if (time.Contains("Unlimited"))
            {
                return (TimeSpan.MaxValue, 0);
            }

            string[] args = time.Split(' '); // in the format "Time {inMs} Inc {inMs}"
            double inc = 0;
            TimeSpan timeSpan = TimeSpan.Zero;
            try
            {
                timeSpan = TimeSpan.FromMilliseconds(double.Parse(args[1]));
                inc = double.Parse(args[3]);

            }
            catch
            {

            }

            return (timeSpan, inc);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            int button = int.Parse(((RadioButton)(sender)).Tag.ToString());
            foreach (RadioButton radio in RadioButtonsStackPanel.Children.OfType<RadioButton>())
            { // loop through each button and deselct each but the currently selected one
                if (radio != sender)
                {
                    radio.IsChecked = false;
                }
            }

            // change the time control grid
            UserControl selection = (UserControl)TimeControlSelectGrid.Children[0];
            TimeControlSelectGrid.Children.Remove(selection);
            switch (button)
            {
                case 0:
                    TimeControlSelectGrid.Children.Add(new UnlimtedTime() { HorizontalAlignment = HorizontalAlignment.Center });
                    break;

                case 1:
                    TimeControlSelectGrid.Children.Add(new TimePerGame() { HorizontalAlignment = HorizontalAlignment.Center });
                    break;
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime newDate = (DateTime)e.AddedItems[0];
            if (newDate.Date > DateTime.Today)
            {
                DateTime original = (DateTime)e.RemovedItems[0];
                MessageBox.Show("You can not pick a date in the future");
                ((DatePicker)sender).SelectedDate = original;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox source = (TextBox)sender;
            string newText = (source).Text;
            bool succesful = int.TryParse(newText, out int result);
            if (!succesful && newText != "" || (result < 1 && succesful))
            {

                // we need to call a dispatcher as the undo operation is still being prepared. https://stackoverflow.com/questions/11115777/unable-to-use-undo-in-textchanged
                Dispatcher.BeginInvoke(new Action(() => source.Undo()));
            }
        }

        private void RoundTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = (TextBox)sender;
            string newText = (source).Text;
            bool succesful = int.TryParse(newText, out int result);
            if (!succesful || result < 1)
            {
                source.Text = "1";
            }
        }

        private void TextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = (TextBox)sender;
            string newText = (source).Text;
            if (newText == "")
            {
                source.Text = "Online";
            }
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Border player1Colour = (Border)sender;
            if (player1Colour.Background == Brushes.White)
                player1Colour.Background = Brushes.Black;

            else if (player1Colour.Background == Brushes.Black)
            {
                player1Colour.Background = null;
                // add ?
                player1Colour.Child = new TextBlock() { Text = "?", Foreground = User.Settings.TextColour.ColourBrush, FontSize = 10, TextAlignment = TextAlignment.Center };
            }

            else if (player1Colour.Background == null)
            {
                // remove ?
                player1Colour.Child = null;
                player1Colour.Background = Brushes.White;
            }
        }

        private bool getPlayer1Colour()
        {

            if (Player1Border.Background == Brushes.White) return true;

            else if (Player1Border.Background == Brushes.Black) return false;

            // random chance that player 1 is white
            Random random = new Random();

            if (random.Next(1, 100) > 50) return true;

            return false;
        }
    }
}
