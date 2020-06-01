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
        /// <summary>
        /// Log Visibility Options
        /// </summary>
        internal static Dictionary<LogType, bool> Options;

        /// <summary>
        /// Queued Logs to Print
        /// </summary>
        private static BlockingCollection<LogItem> LogQueue = new BlockingCollection<LogItem>();

        /// <summary>
        /// Static Constructor for Log, starts thread for log queuing
        /// </summary>
        static Log()
        {
            // start a new thread
            Thread thread = new Thread(() => {
                // only log when the program should be running
                while (Program.ShouldRun)
                {
                    // take the first item (fifo)
                    LogItem logItem = LogQueue.Take();
                    // set the console forground
                    Console.ForegroundColor = logItem.color;
                    // print message
                    Console.WriteLine(logItem.message);
                    // reset colour
                    Console.ResetColor();
                }
            });

            // thread should work in the background
            thread.IsBackground = true;
            // start the thread
            thread.Start();
        }

        /// <summary>
        /// Prints to Console in the Console Default Colour
        /// </summary>
        /// <param name="value"></param>
        internal static void Verbose(object value)
        {
            Task.Run(() => {
                if (Options[LogType.VERBOSE])
                {
                    LogItem logItem = new LogItem(value.ToString());
                    LogQueue.Add(logItem);
                }
            });
        }

        /// <summary>
        /// Prints to Console in Green
        /// </summary>
        /// <param name="value"></param>
        internal static void Success(object value)
        {
            Task.Run(() => {
                if (Options[LogType.SUCCESS])
                {
                    LogItem logItem = new LogItem(value.ToString(), ConsoleColor.Green);
                    LogQueue.Add(logItem);
                }
            });
        }

        /// <summary>
        /// Prints to Console in White
        /// </summary>
        /// <param name="value"></param>
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

        /// <summary>
        /// Prints to Console in DarkYellow
        /// </summary>
        /// <param name="value"></param>
        internal static void Warning(object value)
        {
            Task.Run(() => {
                if (Options[LogType.WARNINGS])
                {
                    LogItem logItem = new LogItem(value.ToString(), ConsoleColor.DarkYellow);
                    LogQueue.Add(logItem);
                }
            });
        }

        /// <summary>
        /// Prints to Console in DarkRed
        /// </summary>
        /// <param name="value"></param>
        internal static void Error(object value)
        {
            Task.Run(() => {
                if (Options[LogType.ERRORS])
                {
                    LogItem logItem = new LogItem(value.ToString(), ConsoleColor.DarkRed);
                    LogQueue.Add(logItem);
                }
            });
        }

        /// <summary>
        /// Prints to Console in Magenta, prefixed with "### DEBUG TRACE:"
        /// </summary>
        /// <param name="value"></param>
        internal static void Debug(object value)
        {
            Task.Run(() => {
                if (Options[LogType.ERRORS])
                {
                    LogItem logItem = new LogItem("### DEBUG TRACE: " + value.ToString(), ConsoleColor.Magenta);
                    LogQueue.Add(logItem);
                }
            });
        }
    }
}
