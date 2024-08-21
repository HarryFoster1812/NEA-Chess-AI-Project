using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.MVVM.Models.Engine;
using UI.MVVM.Models.Players;

namespace UI.MVVM.Views
{
    /// <summary>
    /// Interaction logic for ManageEnginesPopup.xaml
    /// </summary>
    public partial class ManageEnginesPopup : Window
    {

        bool validEngine = false;
        bool connectionEnded = false;
        Process process;
        EngineInfo tempEngine = new EngineInfo();

        public ManageEnginesPopup()
        {
            InitializeComponent();
            for (int i = 0; i < User.Engines.Count; i++)
            {
                string engine = User.Engines[i].name;

                Engines.Items.Add(new ListBoxItem() { Content = engine });

                if (i == User.Settings.DefaultEngine)
                {
                    ListBoxItem item = (ListBoxItem)Engines.Items[i];
                    item.Background = Brushes.DarkGreen;
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileSelect = new();
            fileSelect.Filter = "Executable Files(*.EXE)|*.EXE|All files (*.*)|*.*";
            if (fileSelect.ShowDialog() == true) // user has selected a file  
            {
                //Get the path of specified file

                string filePath = fileSelect.FileName;

                // check if it is already in the list
                if (checkEngineContains(filePath))
                {
                    MessageBox.Show("The item you selected is already in the list");
                    return;
                }

                // validate the engine and get the information
                checkEngineValidity(filePath);

                //wait for 100ms to let the validation finish
                Thread.Sleep(100);

                // add the engine
                if (validEngine)
                {
                    User._Engines.Add(tempEngine);
                    Engines.Items.Add(new ListBoxItem() { Content = tempEngine.name });
                }

                // show a error to the user
                else
                {
                    MessageBox.Show("Not a valid engine");
                    return;
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            int index = Engines.SelectedIndex;
            if (index == 0)
            {
                MessageBox.Show("Can not delete the default engine");
                return;
            }

            else if (index == -1)
            {
                MessageBox.Show("No item selected");
                return;
            }

            else if (index == User.Settings.DefaultEngine)
            {
                ChangeDefault(0);
            }

            User.Engines.RemoveAt(index);
            Engines.Items.RemoveAt(index);
        }

        private void ChangeDefault(int index)
        {
            ListBoxItem item = (ListBoxItem)Engines.Items[User.Settings.DefaultEngine];
            item.Background = null;
            item = (ListBoxItem)Engines.Items[index];
            item.Background = Brushes.DarkGreen;
            User.Settings.DefaultEngine = index;
        }

        private void MakeDefault_Click(object sender, RoutedEventArgs e)
        {
            int index = Engines.SelectedIndex;

            if (index == -1)
            {
                MessageBox.Show("No item selected");
                return;
            }

            ChangeDefault(index);
        }

        private void checkEngineValidity(string path)
        {
            validEngine = false;
            connectionEnded = false;

            tempEngine = new EngineInfo();
            tempEngine.path = path;

            process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    //CreateNoWindow = true,
                    RedirectStandardError = true,
                }
            };
            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.WriteLine("uci");
            do
            {
                Task.Delay(new TimeSpan(0, 0, 1)).ContinueWith(o => { connectionEnded = true; });
            } while (!connectionEnded);
            process.Kill();
        }

        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            else if (e.Data.Contains("id"))
            {
                // split the id command
                if (e.Data.Contains("name"))
                {
                    string name = e.Data.Split(' ')[2];
                    tempEngine.name = name;
                }

                else
                {
                    // it is the author
                    string author = e.Data.Split(' ')[2];
                    tempEngine.author = author;
                }
            }

            else if (e.Data.Contains("option"))
            {
                // split and parse the option command
            }

            else if (e.Data.Contains("uciok"))
            {
                process.StandardInput.WriteLine("quit");
                validEngine = true;
                connectionEnded = true;
            }
        }

        private bool checkEngineContains(string path)
        {
            // perform a linear search
            foreach (EngineInfo i in User._Engines)
            {
                if (i.path == path) return true;
            }

            return false;
        }
    }
}


// write a function that will check the engine (run it and input the UCI command to make sure it is a vailid engine)
// return the name of the engine also allow the users to input the parameters they want (edit engine popup)