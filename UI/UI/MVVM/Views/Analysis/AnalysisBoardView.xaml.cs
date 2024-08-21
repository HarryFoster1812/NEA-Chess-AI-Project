using System.Windows.Controls;
using UI.MVVM.ViewModels;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for AnalysisBoardView.xaml
    /// </summary>
    public partial class AnalysisBoardView : UserControl
    {
        internal AnalysisBoardViewModel viewModel;

        public AnalysisBoardView()
        {
            InitializeComponent();
            viewModel = new AnalysisBoardViewModel(BoardCanvas);
        }
    }
}
