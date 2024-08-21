using Engine.Evaluation;
using Engine.PolyGlot;
using Engine.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Engine
{
    public class CommandHandler
    {

        Bitboard b = new Bitboard(); // the current position
        Searcher searcher = new Searcher(); // 
        static Generation generator = new Generation();
        Perft perft = new Perft();
        string loc = Directory.GetParent(Directory.GetCurrentDirectory()).FullName.Split(new string[] { "UI", "Engine" }, StringSplitOptions.None)[0];
        bool UseBook = true; // bool to indicate whether or not the engine can use the opening book
        Thread actionThread; // the current proccess which is being executed
        public static bool Debug = false; // if debug information should be shown, used throughout the program

        /// <summary>
        /// Handles the UCI commands
        /// </summary>
        public CommandHandler() { }

        /// <summary>
        /// Gets the input from the console and passes it to the corresponding parser
        /// </summary>
        /// <exception cref="Exception">Throws an error if the command is invalid</exception>
        public void ProcessCommand(string command)
        {
            command = command.Trim();
            try
            {
                if (command == "uci")
                {
                    Console.WriteLine("id name NEA Chess Engine");
                    Console.WriteLine("id author Harry Foster");
                    Console.WriteLine("option name OwnBook type check default true");
                    Console.WriteLine("option name BookName type string");
                    Console.WriteLine("uciok");
                }


                else if (command == "ucinewgame")
                {
                    b = new Bitboard();
                    searcher = new Searcher();
                    generator = new Generation();
                    perft = new Perft();
                }

                else if (command == "isready")
                {
                    Console.WriteLine("readyok");
                }

                else if (command == "stop")
                {
                    stopThreadProcess();
                }

                else if (command.Contains("position"))
                {
                    HandlePositionCommand(command);
                }

                else if (command.Contains("setoption"))
                {
                    HandleSetOptionCommand(command);
                }

                else if (command.Contains("go"))
                {
                    HandleGoCommand(command);
                }

                else if (command == "d")
                {
                    Tools.printPieceBoard(b);
                }

                else if (command == "help")
                {
                    ListCommands();
                }

                else if (command == "poly")
                {
                    HandlePolyCommand(command);
                }

                else if (command == "quit")
                {
                    Environment.Exit(0);
                }

                else if (command == "static")
                {
                    Evaluator evaluator = new Evaluator();
                    Console.WriteLine(evaluator.Evaluate(b));
                }

                else if (command == "")
                {
                }

                else { throw new Exception("type help for a list of availible commands"); }
            }

            catch (Exception e)
            {
                Console.WriteLine("Could not recognise command: " + command);
                Console.WriteLine(e.Message);


                if (Debug)
                {
                    Console.WriteLine(e.InnerException);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine(e.Source);
                }
            }
        }

        /// <summary>
        /// Parse the position command and will execute the specified instruction
        /// </summary>
        /// <exception cref="Exception">Throws an error if the command is invalid</exception>
        void HandlePositionCommand(string command)
        {
            Bitboard oldpos = this.b;

            // check if the command has enough arguments 
            if (command.Split(' ').Length >= 2)
            {
                // if the second argument is starpos then create a new bitboard
                if (command.Contains("startpos"))
                {
                    b = new Bitboard();
                }

                else if (command.Contains("fen"))
                {
                    // extracts everything after "fen"
                    string fenstring = command.Split(new string[] { "fen " }, StringSplitOptions.None)[1];

                    string[] fenArray = fenstring.Split(' ');
                    fenstring = "";
                    // create the fen string 
                    for (int i = 0; i < 6; i++) // 6 as there are 6 parts in a fen string: {board} {w/b} {Castling} {enpassant} {plys} {moves}
                    {
                        fenstring += fenArray[i] + ' ';
                    }

                    b = Tools.FENtoBitboard(fenstring);
                }

                // check if the command makes any moves
                if (command.Contains("moves"))
                {
                    // extract everything after "moves "
                    string MovesString = command.Split(new string[] { "moves " }, StringSplitOptions.None)[1];
                    // converts it into an array of moves
                    string[] moves = MovesString.Split(' ');
                    foreach (string move in moves)
                    {
                        // try and convert the move from a string
                        if (stringToMove(move, b, out Move target))
                        {
                            b.MakeMove(target);
                        }

                        // the conversion failed and a move was not found therefore it must be invalid
                        else
                        {
                            this.b = oldpos;
                            throw new Exception($"Invalid Move: {move}");
                        }
                    }
                }

            }

            else
            {
                throw new Exception("Incorrect position command expected starpos or fen");
            }
        }

        /// <summary>
        /// Parse the setOption command and will execute the specified instruction
        /// </summary>
        /// <exception cref="Exception">Throws an error if the command is invalid</exception>
        void HandleSetOptionCommand(string command)
        {
            string[] param = command.Split(' ');

            if (param[2] == "BookName")
            {
                // creates a new book using the new book name
                Polyglot.BookPath = param[4];
            }

            else if (param[2] == "OwnBook")
            {
                // converts the input to a bool
                UseBook = bool.Parse(param[4]);
            }
        }

        /// <summary>
        /// Displays the books moves for the current position
        /// </summary>
        void HandlePolyCommand(string command)
        {
            // show the book moves for the current position
            PolyGlot.Polyglot.showBookMoves(b.zobristKey.Key);
        }

        /// <summary>
        /// Parse the go command and will execute the specified instruction
        /// </summary>
        /// <exception cref="Exception">Throws an error if the go command is invalid</exception>
        void HandleGoCommand(string command)
        {
            int depth;
            // check if it is a performance test command
            if (command.Contains("perft"))
            {
                //  the command should contain at least 3 arguments
                if (command.Split(' ').Length < 3)
                {
                    throw new Exception("Invalid perft command expected format: go <perft> <int>");
                }

                // do a capture performance test
                if (command.Contains("captures"))
                {
                    // the depth of search should be the third argument
                    depth = int.Parse(command.Split(' ')[2]);
                    actionThread = new Thread(() => perft.PerftTestCaptures(b, depth));
                    actionThread.Start();
                    return;
                }

                // do a bulk performance test
                else if (command.Contains("bulk"))
                {
                    // load the json data from the file
                    string bulkFileLoc = Path.Combine(loc, "PerftTestPositions.txt");
                    string json = File.ReadAllText(bulkFileLoc);
                    // serialise the json into an arry of PerftTest classes
                    PerftTest[] perftTests = JsonConvert.DeserializeObject<PerftTest[]>(json);
                    actionThread = new Thread(() => perft.PerftTestBulkTest(b, perftTests));
                    actionThread.Start();
                    return;
                }

                // does a performance test and compares the output with the text file
                else if (command.Contains("results"))
                {
                    depth = int.Parse(command.Split(' ')[2]);
                    // loads the expected outputs
                    string resultsFileLoc = Path.Combine(loc, "PerftResults.txt");

                    string[] results = File.ReadAllLines(resultsFileLoc);
                    // performs the test
                    actionThread = new Thread(() => perft.PerftTestDebug(b, depth, results));
                    actionThread.Start();
                    return;
                }

                // regular perft test
                depth = int.Parse(command.Split(' ')[2]);
                actionThread = new Thread(() => perft.PerftTest(b, depth));
                actionThread.Start();
            }
            else
            {

                if (command.Split(' ').Length < 2 || command.Contains("infinite")) // it is only "go" so an infinite search will start
                {
                    // probe the opening book
                    if (tryFindBook(out Move foundBookMove))
                    {
                        Console.WriteLine($"bestmove {Tools.MoveToString(foundBookMove)}");
                        return;
                    }

                    // start a infinite search
                    actionThread = new Thread(() => searcher.Search(b));
                    actionThread.Start();
                }

                if (command.Contains("time"))
                { // the command contains wtime <int> and btime <int> 
                    try
                    {
                        if (tryFindBook(out Move foundBookMove))
                        {
                            Console.WriteLine($"bestmove {Tools.MoveToString(foundBookMove)}");
                            return;
                        }

                        int whiteInc = 0;
                        int blackInc = 0;

                        int whiteTime = int.Parse(command.Split(new string[] { "wtime " }, StringSplitOptions.None)[1].Split(' ')[0]);
                        int blackTime = int.Parse(command.Split(new string[] { "btime " }, StringSplitOptions.None)[1].Split(' ')[0]);

                        if (command.Contains("inc"))
                        {
                            whiteInc = int.Parse(command.Split(new string[] { "winc " }, StringSplitOptions.None)[1].Split(' ')[0]);
                            blackInc = int.Parse(command.Split(new string[] { "binc " }, StringSplitOptions.None)[1].Split(' ')[0]);
                        }

                        runTimedSearch(whiteTime, blackTime, whiteInc, blackInc);
                    }
                    catch
                    {
                        throw new Exception("Invalid go command expected format: go <wtime> <int> <winc> <int> <btime> <int> <binc> <inc>");
                    }
                }

                else if (command.Contains("depth"))
                {
                    if (tryFindBook(out Move foundBookMove))
                    {
                        Console.WriteLine($"bestmove {Tools.MoveToString(foundBookMove)}");
                        return;
                    }
                    try
                    {
                        depth = int.Parse(command.Split(' ')[2]);
                        actionThread = new Thread(() => searcher.Search(b, depth));
                        actionThread.Start();
                    }
                    catch
                    {
                        throw new Exception("Invalid go command expected format: go <depth> <int>");
                    }
                }

            }
        }

        /// <summary>
        /// Lists all of the vaild commands a user can enter
        /// </summary>
        void ListCommands()
        {
            Console.WriteLine("uci - switch to uci mode and show the possible engine settings");
            Console.WriteLine("isready - check if the engine is ready to recive commands");
            Console.WriteLine("ucinewgame - create a new game");
            Console.WriteLine("setoption name <OptionName> value <value> - change a option that was listed in uci");
            Console.WriteLine("help - show this screen");
            Console.WriteLine("d - display the current board");
            Console.WriteLine("position [fen <fenstring> | startpos ] moves <move1> .... <movei> - sets up the specified position");
            Console.WriteLine("go - starts an infinite search");
            Console.WriteLine("go depth <int> - starts a search for a limited depth");
            Console.WriteLine("go perft <int> - runs a performance test for a limited depth");
            Console.WriteLine("go perft <int> <bulk/capture/results> - runs a performance test and displays info for debugging for a limited depth");
            Console.WriteLine("poly - shows all of the moves found in the opening book for the current position");
            Console.WriteLine("quit - exits the engine");
            Console.WriteLine("stop - stops the current process as soon as possible");
            Console.WriteLine("For more information please refer to the UCI documentaion online");

        }

        /// <summary>
        /// Method <c>tryFindBook</c> 
        /// will run a polyglot book search if the option is enabled
        /// </summary>
        /// <param name="bookmove">The move that was returned by the opening book prober</param>
        /// <returns>A bool that indicates if the search was successful</returns>
        bool tryFindBook(out Move bookmove)
        {
            try
            {
                bookmove = new Move(); // create empty move
                if (UseBook)
                {
                    // get the current king index
                    int kingloc = BitBoardTools.BitboardToIndex(Bitboard.getKings(b.Board, (b.WhiteToPlay ? 0 : 1)));
                    // probe the opening book
                    Move result = Polyglot.queryBook(kingloc, b.zobristKey.Key);
                    bookmove = result;

                    if (result.isEmpty)
                    {
                        return false;
                    }

                    return true;
                }

                // useBook was false
                else return false;
            }
            catch
            {
                bookmove = new Move();
                return false;
            }
        }

        /// <summary>
        /// Method <c>stopThreadProcess</c> 
        /// Will call the stop search function in the searcher and the perft which will terminate the thread process as fast as it can 
        /// </summary>
        void stopThreadProcess()
        {
            if (actionThread != null && actionThread.IsAlive)
            {
                searcher.stopSearch();
                perft.stopsearch();
            }
        }

        /// <summary>
        /// Method <c>stringToMove</c> 
        /// Will convert the given move from a string into a Move struct
        /// </summary>
        /// <param name="move">The move that will be converted</param>
        /// <param name="move1">The converted move in the Move struct format</param>
        /// <returns>A bool that indicates if the conversion was successful</returns>
        public static bool stringToMove(string move, Bitboard board, out Move move1)
        {
            string start = move.Substring(0, 2);
            string end = move.Substring(2, 2);
            int promotion = -1;

            // check if it is a promotion
            if (move.Length > 4)
            {
                switch (move[4])
                {
                    case 'n':
                        promotion = 0;
                        break;

                    case 'b':
                        promotion = 1;
                        break;

                    case 'r':
                        promotion = 2;
                        break;

                    case 'q':
                        promotion = 3;
                        break;
                }
            }

            byte startIndex = Tools.SquareToIndex(start);
            byte endIndex = Tools.SquareToIndex(end);

            List<Move> movelist = generator.GenerateMoves(board);

            bool found = false;
            Move target = new Move();
            move1 = new Move();

            // loop over the legal moves and find the target move
            foreach (Move i in movelist)
            {
                // check if the start index and destination index are the same
                if (i.startIndex == startIndex && i.destinationIndex == endIndex)
                {
                    if (promotion != -1)
                    {
                        // get the correct promotion
                        if (i.isPromotion && i.pieceIndex == promotion)
                        {
                            target = i;
                            found = true;
                            break;
                        }
                    }

                    // it is not a promotion
                    else
                    {
                        target = i;
                        found = true;
                        break;
                    }
                }
            }
            // the target move was found
            if (found)
            {
                move1 = target;
                return true;
            }

            // the target move was not found
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method <c>runTimedSearch</c> 
        /// Will calculate the amount of time the program will search for.
        /// If the program does not finish in the specified time then it will cancel the search
        /// and return the best evaluation found
        /// </summary>
        /// <param name="whiteTime">the amount of time in ms that white has left on the clock</param>
        /// <param name="blackTime">the amount of time in ms that black has left on the clock</param>
        /// <param name="whiteInc"> the amount of time in ms that white will add every move played</param>
        /// <param name="blackInc"> the amount of time in ms that black will add every move played</param>
        void runTimedSearch(int whiteTime, int blackTime, int whiteInc, int blackInc)
        {
            int Searchtime = Searcher.CalculateSearchTime(whiteTime, blackTime, whiteInc, blackInc, b.WhiteToPlay);
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(Searchtime);
                stopThreadProcess();
            });
        }
    }
}
