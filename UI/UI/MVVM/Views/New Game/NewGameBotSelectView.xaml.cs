using System.Windows.Controls;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for NewGameBotSelectView.xaml
    /// </summary>
    public partial class NewGameBotSelectView : UserControl
    {
        public NewGameBotSelectView()
        {
            InitializeComponent();
            EnginePathCombo.SelectedIndex = User.Settings.DefaultEngine;
        }

        public override string ToString()
        {
            string difficultyLevel = (EngineDifficulty.SelectedIndex + 1).ToString();
            return $"Name:{EnginePathCombo.Text} Index:{EnginePathCombo.SelectedIndex} Level:{difficultyLevel}";
        }
    }
}
