using System;

namespace Engine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string command = "";
            CommandHandler handler = new CommandHandler();

            do
            {
                command = Console.ReadLine();
                handler.ProcessCommand(command);

            } while (command != "exit");
        }
    }
}
