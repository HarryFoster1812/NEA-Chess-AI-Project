using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for TimePerGame.xaml
    /// </summary>
    public partial class TimePerGame : UserControl
    {

        TextBox[] textBoxes;
        string OldText = string.Empty;

        public TimePerGame()
        {
            InitializeComponent();
            textBoxes = new TextBox[] { TimeHH, TimeMM, TimeSS, IncHH, IncMM, IncSS, IncMS };
        }

        public override string ToString()
        {
            string time = "Time";
            int timeinMs = 0;
            timeinMs += int.Parse(TimeHH.Text) * 3600000;
            timeinMs += int.Parse(TimeMM.Text) * 60000;
            timeinMs += int.Parse(TimeSS.Text) * 1000;

            time += $" {timeinMs} Inc";

            int incinMs = 0;
            incinMs += int.Parse(IncHH.Text) * 3600000;
            incinMs += int.Parse(IncMM.Text) * 60000;
            incinMs += int.Parse(IncSS.Text) * 1000;
            incinMs += int.Parse(IncMS.Text);

            time += $" {incinMs}";
            return time;
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            string[] tag = ((string)((RepeatButton)sender).Tag).Split(' ');
            int index = int.Parse(tag[0]);

            TextBox box = textBoxes[index];
            int current = int.Parse(box.Text);
            if (tag[1] == "U") // inc
            {
                if (current == 99)
                {
                    MessageBox.Show("99 is the max");
                    return;
                }

                current++;
                box.Text = current.ToString("00");

            }
            else
            { // dec
                if (current == 0)
                {
                    MessageBox.Show("Cannot go below 0");
                    return;
                }
                current--;
                box.Text = current.ToString("00");

            }
        }

        private void TextBox_Focus(object sender, EventArgs e)
        {
            TextBox box = (TextBox)sender;
            OldText = box.Text;
        }

        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "")
            {
                box.Text = "00";
            }
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            int _temp;
            if (int.TryParse(box.Text, out _temp) && !(_temp > 99 || _temp < 0) || box.Text == "")
            {
                return;
            }
            else
            {
                box.Text = OldText;
            }
        }

        // add inc/dec button


    }
}
