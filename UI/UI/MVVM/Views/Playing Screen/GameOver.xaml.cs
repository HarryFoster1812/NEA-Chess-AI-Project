using System.Windows.Controls;

namespace UI.MVVM.Views.Playing_Screen
{
    /// <summary>
    /// Interaction logic for GameOver.xaml
    /// </summary>
    public partial class GameOver : UserControl
    {
        public GameOver()
        {
            InitializeComponent();
        }

        public void changeMesage(string message)
        {
            MessageTextBlock.Text = message;
        }
    }
}
