using System.Windows;
using System.Windows.Controls;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for EngineAnalysisView.xaml
    /// </summary>
    public partial class EngineAnalysisView : UserControl
    {
        public EngineAnalysisView()
        {
            InitializeComponent();
            EngineNameTextBlock.Text = User.Engines[User.Settings.DefaultEngine].name;
        }

        public void Clear()
        {
            PVLinesStackPanel.Children.Clear();

        }

        public void AddInfo(string arg)
        {
            // scrape the data

            // nodes number_of_nodes_searched
            if (arg.Contains("nodes"))
            {
                NodesSearchedTextBlock.Text = DepthTextBlock.Text = "Depth: " + arg.Split("nodes ")[1].Split(" ")[0];
            }
            // depth
            if (arg.Contains("depth"))
            {
                DepthTextBlock.Text = "Depth: " + arg.Split("depth ")[1].Split(" ")[0];
            }
            // nps
            if (arg.Contains("nps"))
            {
                NPSTextBlock.Text = "MNPS: " + arg.Split("nps ")[1].Split(" ")[0];
            }
            // score <cp | mate | lowerbound | upperbound> <value>
            if (arg.Contains("score"))
            {
                string typeofScore = arg.Split("score ")[1].Split(" ")[0];
                if (typeofScore == "mate")
                {
                    EvaluationTextBlock.Text = "#" + arg.Split("mate ")[1].Split(' ')[0];
                }
                else if (typeofScore == "cp")
                {
                    double evalInCp = double.Parse(arg.Split("cp ")[1].Split(' ')[0]);
                    double eval = evalInCp / 100;
                    EvaluationTextBlock.Text = eval.ToString();
                }

                else
                {
                    EvaluationTextBlock.Text = arg.Split(typeofScore + " ")[1].Split(' ')[0];

                }
            }

            // currmove

            // currmovenumber

            //hashful 

            if (arg.Contains("pv"))
            {
                string pv = arg.Split("pv ")[1];
                TextBlock textBlock = new TextBlock()
                {
                    Text = pv,
                    Foreground = User.Settings.TextColour.ColourBrush,
                    TextWrapping = TextWrapping.Wrap,
                };

                PVLinesStackPanel.Children.Add(textBlock);

                // add arrow for the best move
                string best = pv.Split(' ')[0];
                //
            }
        }


    }
}
