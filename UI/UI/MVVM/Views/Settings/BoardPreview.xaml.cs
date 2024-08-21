using System.Windows.Controls;
using UI.MVVM.ViewModels;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for BoardPreview.xaml
    /// </summary>
    public partial class BoardPreview : UserControl
    {
        internal BoardPreiewModel viewModel;

        public BoardPreview()
        {
            InitializeComponent();
            viewModel = new BoardPreiewModel(BoardCanvas);
        }
    }
}
