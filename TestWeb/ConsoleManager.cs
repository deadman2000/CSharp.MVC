using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestWeb
{
    class ConsoleManager
    {
        public static void Run()
        {
            Console.CursorVisible = false;
            Console.WindowHeight = 50;
            Console.WindowWidth = 200;

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.C: Console.Clear(); break;
                    default: break;
                }
            }
        }
    }
}
