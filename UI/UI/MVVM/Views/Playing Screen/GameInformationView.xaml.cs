using System;
using System.Windows.Controls;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for GameInformationView.xaml
    /// </summary>
    public partial class GameInformationView : UserControl
    {
        public static StackPanel movesStackPanel = new StackPanel();
        public GameInformationView()
        {
            InitializeComponent();
            movesStackPanel = Moves;
        }


        public void ChangeGameinfo(string Event, string Site, string Round, DateTime Date)
        {
            EventBox.Text = Event;
            SiteBox.Text = Site;
            RoundBox.Text = Round;
            GameDate.SelectedDate = Date;
        }
    }
}
