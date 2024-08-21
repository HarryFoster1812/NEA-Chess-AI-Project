using System.Windows.Controls;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for NewGamePlayerSelectView.xaml
    /// </summary>
    public partial class NewGamePlayerSelectView : UserControl
    {
        public NewGamePlayerSelectView()
        {
            InitializeComponent();
        }

        public override string ToString()
        {
            return NameTextBox.Text;
        }
    }
}
