using System.Windows.Controls;
using UI.MVVM.ViewModels;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for BoardView.xaml
    /// </summary>
    public partial class BoardView : UserControl
    {
        internal BoardViewModel viewModel;

        public BoardView()
        {
            InitializeComponent();
            viewModel = new BoardViewModel(BoardCanvas);
        }


    }
}
