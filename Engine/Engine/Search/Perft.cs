using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Search
{

    public class PerftTest
    {
        public int depth;
        public ulong nodes;
        public string fen;
    }
    public class Perft
    {
        Bitboard board;
        int targetDepth;
        string[] debugingstring;
        PerftTest[] tests;
        Generation generator = new Generation();
        bool stopSearch = false;

        internal void stopsearch()
        {
            stopSearch = true;
        }

        public void PerftTestBulkTest(Bitboard b, PerftTest[] testarray)
        {
            stopSearch = false;
            this.board = b;
            int n_passed = 0;
            foreach (PerftTest i in testarray)
            {
                if (stopSearch) return;

                Console.WriteLine("FEN: " + i.fen);
                board = Tools.FENtoBitboard(i.fen);
                targetDepth = i.depth;
                Console.WriteLine($"Depth: {i.depth}");
                ulong nodes = RecursivePerftTest(i.depth);
                Console.WriteLine("Nodes: " + i.nodes);
                if (nodes == i.nodes)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("  Passed\n");
                    n_passed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("  Failed   " + "EXPECTED: " + i.nodes);
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\n");
            }

            Console.WriteLine($"Passed: {n_passed}/{testarray.Length}");
        }

        public void PerftTest(Bitboard b, int depth)
        {
            stopSearch = false;
            this.board = b;
            targetDepth = depth;
            Console.WriteLine($"Depth: {depth}");

            ulong nodes;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            nodes = RecursivePerftTest(depth);
            timer.Stop();
            Console.WriteLine($"Nodes: {nodes}");
            Console.WriteLine($"Time Elapsed: {timer.Elapsed}");
            Console.WriteLine($"Million Nodes Per Second: {(double)(nodes / 1000) / (double)(timer.ElapsedMilliseconds)}");
            Console.WriteLine("\n");
        }

        public void PerftTestCaptures(Bitboard b, int depth)
        {
            stopSearch = false;
            this.board = b;
            targetDepth = depth;
            Console.WriteLine($"Depth: {depth}");

            ulong nodes;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            nodes = RecursivePerftTestCapturesOnly(depth);
            timer.Stop();
            Console.WriteLine($"Nodes: {nodes}");
            Console.WriteLine($"Time Elapsed: {timer.Elapsed}");
            Console.WriteLine($"Million Nodes Per Second: {(double)(nodes / 1000) / (double)(timer.ElapsedMilliseconds)}");
            Console.WriteLine("\n");
        }

        public void PerftTestDebug(Bitboard b, int depth, string[] DEBUGING)
        {
            stopSearch = false;
            this.board = b;
            this.debugingstring = DEBUGING;
            targetDepth = depth;
            Console.WriteLine($"Depth: {depth}");
            Console.WriteLine($"Nodes: {RecursiveDebugPerftTest(depth)}");
            Console.WriteLine("\n\n");
        }

        public void PerftTestMoveDebug(Bitboard b, int depth, string[] DEBUGING)
        {
            stopSearch = false;
            this.board = b;
            this.debugingstring = DEBUGING;
            targetDepth = depth;
            Console.WriteLine($"Depth: {depth}");
            Console.WriteLine($"Nodes: {RecursiveMoveDebugPerftTest(depth)}");
            Console.WriteLine("\n\n");
        }

        private ulong RecursiveMoveDebugPerftTest(int depth)
        {
            List<Move> move_list = generator.GenerateMoves(board, false);
            int n_moves;
            n_moves = move_list.Count;
            ulong nodes = 0;

            if (depth == 1)
            {
                return (ulong)n_moves;
            }
            else
            {
                for (int i = 0; i < n_moves; i++)
                {
                    ulong expected = 0;

                    if (stopSearch) return nodes;

                    if (depth == targetDepth)
                    {
                        string move = Tools.MoveToString(move_list[i]);
                        Console.Write(move + ": \n");

                        Console.WriteLine("Before:");
                        Tools.printPieceBoard(board);

                        foreach (string j in debugingstring)
                        {
                            if (j.Contains(move))
                            {
                                expected = ulong.Parse(j.Split(' ')[1]);
                            }
                        }
                    }
                    board.MakeMove(move_list[i]);
                    ulong nodesSearched = RecursivePerftTest(depth - 1);
                    if (depth == targetDepth)
                    {
                        Console.WriteLine("After:");
                        Tools.printPieceBoard(board);

                        Console.Write(nodesSearched.ToString());
                        if (nodesSearched == expected)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("  Passed\n");
                        }

                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write($"  Failed   EXPECTED:{expected}\n");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    nodes += nodesSearched;
                    board.UndoMove(move_list[i]);
                }
                return nodes;
            }
        }

        private ulong RecursiveDebugPerftTest(int depth)
        {
            List<Move> move_list = generator.GenerateMoves(board, false);

            int n_moves;
            n_moves = move_list.Count;
            ulong nodes = 0;

            if (depth == 1)
            {
                if (depth == targetDepth)
                {
                    Tools.printPieceBoard(board);
                    string[] movesinstring = new string[n_moves];

                    for (int i = 0; i < n_moves; i++)
                    {
                        movesinstring[i] = Tools.MoveToString(move_list[i]);
                    }

                    foreach (string j in debugingstring)
                    {
                        string move = j.Split(':')[0];
                        Console.Write(move + ":");
                        if (movesinstring.Contains(move))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("  Passed\n");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("  Failed\n");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                return (ulong)n_moves;
            }
            else
            {
                for (int i = 0; i < n_moves; i++)
                {
                    if (stopSearch) return nodes;
                    ulong expected = 0;
                    if (depth == targetDepth)
                    {
                        string move = Tools.MoveToString(move_list[i]);
                        Console.Write(move + ": ");
                        foreach (string j in debugingstring)
                        {
                            if (j.Contains(move))
                            {
                                expected = ulong.Parse(j.Split(' ')[1]);
                            }
                        }
                    }
                    board.MakeMove(move_list[i]);
                    ulong nodesSearched = RecursivePerftTest(depth - 1);
                    if (depth == targetDepth)
                    {
                        Console.Write(nodesSearched.ToString());
                        if (nodesSearched == expected)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("  Passed\n");
                        }

                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write($"  Failed   EXPECTED:{expected}\n");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    nodes += nodesSearched;
                    board.UndoMove(move_list[i]);
                }
                return nodes;
            }
        }

        private ulong RecursivePerftTest(int depth)
        {
            List<Move> move_list = generator.GenerateMoves(board, false);

            int n_moves;
            n_moves = move_list.Count;
            ulong nodes = 0;

            if (depth == 1)
            {
                return (ulong)n_moves;
            }
            else
            {
                for (int i = 0; i < n_moves; i++)
                {
                    if (stopSearch) return nodes;

                    if (depth == targetDepth)
                    {
                        Console.Write(Tools.MoveToString(move_list[i]) + ": ");
                    }

                    board.MakeMove(move_list[i]);
                    ulong nodesSearched = RecursivePerftTest(depth - 1);
                    if (depth == targetDepth)
                    {
                        Console.Write(nodesSearched.ToString() + "\n");
                    }
                    nodes += nodesSearched;
                    board.UndoMove(move_list[i]);
                }
                return nodes;
            }
        }

        private ulong RecursivePerftTestCapturesOnly(int depth)
        {
            List<Move> move_list = generator.GenerateMoves(board, true);

            int n_moves;
            n_moves = move_list.Count;
            ulong nodes = 0;

            if (depth == 1)
            {
                return (ulong)n_moves;
            }
            else
            {
                for (int i = 0; i < n_moves; i++)
                {
                    if (stopSearch) return nodes;

                    if (depth == targetDepth)
                    {
                        Console.Write(Tools.MoveToString(move_list[i]) + ": ");
                    }

                    board.MakeMove(move_list[i]);
                    ulong nodesSearched = RecursivePerftTestCapturesOnly(depth - 1);
                    if (depth == targetDepth)
                    {
                        Console.Write(nodesSearched.ToString() + "\n");
                    }
                    nodes += nodesSearched;
                    board.UndoMove(move_list[i]);
                }
                return nodes;
            }
        }

    }
}
