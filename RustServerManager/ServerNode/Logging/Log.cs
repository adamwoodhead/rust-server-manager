using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Logging
{
    internal static class Log
    {
        internal static Dictionary<LogType, bool> Options;

        private static BlockingCollection<LogItem> LogQueue = new BlockingCollection<LogItem>();

        static Log()
        {
            Thread thread = new Thread(() => {
                while (Program.ShouldRun)
                {
                    LogItem logItem = LogQueue.Take();
                    Console.ForegroundColor = logItem.color;
                    Console.WriteLine(logItem.message);
                    Console.ResetColor();
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        internal static void Verbose(object value)
        {
            Task.Run(() => {
                if (Options[LogType.VERBOSE])
                {
                    LogItem logItem = new LogItem("Verbose: " + value.ToString());
                    LogQueue.Add(logItem);
                }
            });
        }

        internal static void Success(object value)
        {
            Task.Run(() => {
                if (Options[LogType.SUCCESS])
                {
                    LogItem logItem = new LogItem("Success: " + value.ToString(), ConsoleColor.Green);
                    LogQueue.Add(logItem);
                }
            });
        }

        internal static void Informational(object value)
        {
            Task.Run(() => {
                if (Options[LogType.INFORMATIONAL])
                {
                    LogItem logItem = new LogItem(value.ToString(), ConsoleColor.White);
                    LogQueue.Add(logItem);
                }
            });
        }

        internal static void Warning(object value)
        {
            Task.Run(() => {
                if (Options[LogType.WARNINGS])
                {
                    LogItem logItem = new LogItem("Warning: " + value.ToString(), ConsoleColor.DarkYellow);
                    LogQueue.Add(logItem);
                }
            });
        }

        internal static void Error(object value)
        {
            Task.Run(() => {
                if (Options[LogType.ERRORS])
                {
                    LogItem logItem = new LogItem("Error: " + value.ToString(), ConsoleColor.DarkRed);
                    LogQueue.Add(logItem);
                }
            });
        }
    }
}
