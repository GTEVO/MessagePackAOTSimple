using CommonLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class ServerConsoleDebugger : IDebugger
    {
        public void LogErrorFormat(string format, object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
        }

        public void LogFormat(string format, object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(format, args);
        }

        public void LogWarningFormat(string format, object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(format, args);
        }
    }
}
