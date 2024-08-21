using Engine;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UI.MVVM.Views;

namespace UI.MVVM.Models.Players
{
    internal class Player
    {
        public bool isWhite;
        public bool canMove;
        public string UserName;
        public int Elo;
        public string flag;
        public TimeSpan timeOnClock = TimeSpan.MaxValue;
        public double clockIncrementMS = 0;
        internal Game game;

        public Player(Game game)
        {
            canMove = true;
            this.game = game;
        }

        public Player(Game game, string UserName)
        {
            canMove = true;
            this.game = game;
        }

        public virtual void SendMove()
        {
            // do nothing
        }

        public virtual void SendRequest(string type)
        {
            // handle as always accept
            if (type == "TakeBack1")
            {
                if (game.MovesPlayedCount < 2) return;
                ((CurrentGame)game).takeBack();
                Thread.Sleep(100);
                ((CurrentGame)game).takeBack();
            }

            else if (type == "TakeBack2")
            {
                if (game.MovesPlayedCount < 2) return;
                ((CurrentGame)game).takeBack();
            }
        }
    }

    internal class Bot : Player
    {
        protected string path;
        protected static Thread? CommunicationThread;
        public static bool connectionEnded;
        protected Process process;
        protected bool ignoreResponse = false;
        protected Canvas? boardCanvas;
        protected bool readyok = false;
        private string depth;

        public Bot(Game game, string botLoc, string depthToSearch = "5") : base(game)
        {
            this.game = game;
            this.depth = depthToSearch;
            canMove = false;

            if (game.boardModel != null)
            {
                this.boardCanvas = game.boardModel.boardCanvas;
            }

            path = botLoc;
            connectionEnded = false;
            CommunicationThread = new Thread(new ThreadStart(this.CreateConnection));
            CommunicationThread.Start();
        }

        protected void CreateConnection()
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                }
            };
            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(this.OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(this.OutputHandler);
            //* Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.WriteLine("uci");
            process.StandardInput.WriteLine("ucinewgame");
            process.StandardInput.WriteLine("isready");
            do
            {
            } while (!connectionEnded);
            process.Close();
        }

        virtual protected void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {

            if (outLine.Data == null)
            {
                return;
            }

            if (outLine.Data.Contains("bestmove") && !ignoreResponse)
            {
                // best move in the format: bestmove {move} Eval: {value}
                string move = outLine.Data.Split(' ')[1];
                string start = move.Substring(0, 2);
                string end = move.Substring(2, 2);
                int promotion = -1;

                if (move.Length > 4)
                {
                    switch (move[4])
                    {
                        case 'n':
                            promotion = 4;
                            break;

                        case 'b':
                            promotion = 5;
                            break;

                        case 'r':
                            promotion = 6;
                            break;

                        case 'q':
                            promotion = 7;
                            break;
                    }
                }
                byte startIndex = Tools.SquareToIndex(start);
                byte endIndex = Tools.SquareToIndex(end);
                Move moveMade = new Move();
                try
                {
                    if (promotion != -1) // Create a promotion
                    {
                        moveMade = new Move(startIndex, endIndex, (ushort)promotion);
                    }
                    else
                    {
                        moveMade = game.createMove(startIndex, endIndex);
                    }
                }
                catch
                {
                    // an illegal move was made
                    // end the game on an illegal move by the engine
                    boardCanvas.Dispatcher.Invoke(() =>
                    { // make the move
                        ((CurrentGame)game).endGame(isWhite ? 0 : 1);
                        MessageBox.Show($"Engine made invallid move: {move}");
                    });
                    return;
                }

                boardCanvas.Dispatcher.Invoke(() =>
                { // make the move
                    game.MakeMove(moveMade);
                    game.boardModel.DrawBoard();
                    game.boardModel.DrawPieces(game.board);
                });
            }

            else if (outLine.Data == "readyok")
            {
                readyok = true;
            }

            else if (outLine.Data.Contains("Exception"))
            {
                MessageBox.Show("An error has occured, the connection has been ended");
                Bot.connectionEnded = true;
            }
        }

        public override void SendMove()
        { // send the search command
            if (!connectionEnded)
            {
                while (!readyok)
                {
                    // wait for readyok
                }
                string PosCommand = game.genUCIPositionCommand();
                process.StandardInput.WriteLine(PosCommand); // set the position
                process.StandardInput.WriteLine("go depth " + depth);
            }
        }

        public override void SendRequest(string type)
        {
            // get the value of the position
            // if it is wining then reject the draw / accept resignation
        }
    }

    internal class AnalysisBot : Bot
    {
        bool ownbookoff = false;
        public AnalysisBot(AnalysisGame game, string botLoc) : base(game, botLoc)
        {
            this.game = game;
            canMove = false;
            this.boardCanvas = game.boardModel.boardCanvas;
        }

        void turnOffOwnBook()
        {
            process.StandardInput.WriteLine("setoption name OwnBook value false");
        }

        protected override void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {

            if (outLine.Data == null)
            {
                return;
            }

            if (outLine.Data.StartsWith("info"))
            {
                // pass the data to the UI
                AnalysisScreen.analysisView.Dispatcher.Invoke(() =>
                {
                    AnalysisScreen.analysisView.AddInfo(outLine.Data);
                });

                if (outLine.Data.Contains("pv"))
                {
                    string data = outLine.Data.Replace("multipv", "");
                    string move = data.Split("pv ")[1].Split(' ')[0];
                    if (move == "")
                    {
                        return;
                    }
                    string start = move.Substring(0, 2);
                    string end = move.Substring(2, 2);
                    boardCanvas.Dispatcher.Invoke(() =>
                    {
                        ((AnalysisGame)game).addCommentToCurrentPosition(start, end, "Arrow");
                    });
                }

            }

            else if (outLine.Data.Contains("Exception"))
            {
                MessageBox.Show("An error has occured, the connection has been ended");
                connectionEnded = true;
            }
        }

        public override void SendMove()
        {
            // check if opening bnook is turned off
            if (!ownbookoff)
            {
                turnOffOwnBook();
                ownbookoff = !ownbookoff;
            }

            // send the search command
            // stop the current search
            process.StandardInput.WriteLine("stop");
            Thread.Sleep(10);

            // create the position command and send it to the engine
            string PosCommand = game.genUCIPositionCommand(game.currentMoveNo);
            process.StandardInput.WriteLine(PosCommand);

            // start an infinite search
            process.StandardInput.WriteLine("go");
        }
    }


    internal class OnlinePlayer : Player
    {
        string path;
        static Thread CommunicationThread;
        public static bool connectionEnded;
        Process process;
        Canvas boardCanvas;

        public OnlinePlayer(Game game) : base(game)
        {
            this.canMove = false;

        }

        [DllImport("user32.dll")]
        private static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);
        public void CreateConnection()
        {
            
        }

        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
        }

        public override void SendMove()
        {
            
        }

        public override void SendRequest(string type)
        {
            // send request to online user
            // recive answer
            // handle response
        }
    }
}
