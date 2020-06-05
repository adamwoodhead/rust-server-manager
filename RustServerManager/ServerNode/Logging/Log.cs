using ServerNode.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Logging
{
    // TODO Add File Logging for each type & mixed
    internal static class Log
    {
        /// <summary>
        /// Queued Logs to Print
        /// </summary>
        private static BlockingCollection<LogItem> LogQueue { get; set; } = new BlockingCollection<LogItem>();
        private static int HardLogCount;
        private static bool IsInitialised = false;
        private static string hardLogDirectoryPath;
        private static Dictionary<string, StreamWriter> StreamWriters = new Dictionary<string, StreamWriter>();

        internal static bool QueueEmpty => LogQueue.Count == 0;

        /// <summary>
        /// Log Visibility Options
        /// </summary>
        internal static Dictionary<LogType, (bool, bool, ConsoleColor)> Options { get; set; }

        /// <summary>
        /// Static Constructor for Log, starts thread for log queuing
        /// </summary>
        internal static void Initialise(Dictionary<LogType, (bool, bool, ConsoleColor)> visibilityOptions, int hardLogCount)
        {
            // if log hasn't been initialised before, then proceed
            if (!IsInitialised)
            {
                // don't allow initialisation again
                IsInitialised = true;

                // set visibility options
                Options = visibilityOptions;

                // set hard log count
                HardLogCount = hardLogCount;

                hardLogDirectoryPath = Path.Combine(Program.LogsDirectory, DateTime.Now.ToString("dd_MM_yyyy__hh_mm_ss"));
                Verbose("Creating Log Directory at: " + hardLogDirectoryPath);
                Directory.CreateDirectory(hardLogDirectoryPath);

                string[] logDirectories = Directory.GetDirectories(Program.LogsDirectory);
                if (logDirectories.Count() > HardLogCount)
                {
                    Warning($"Exceeded Hard Log Count - Deleting Oldest");

                    foreach (DirectoryInfo dir in logDirectories.Select(x => new DirectoryInfo(x)).OrderByDescending(x => x.CreationTime).Skip(hardLogCount))
                    {
                        try
                        {

                            if (DirectoryExtensions.DeleteOrTimeout(dir))
                            {
                                Verbose($"Deleted Log Folder {dir.FullName}");
                            }

                            if (Directory.GetDirectories(Program.LogsDirectory).Count() <= 5)
                            {
                                break;
                            }
                        }
                        // System.IO.IOException: Directory not empty
                        catch (System.IO.IOException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{DateTime.Now:G}: Server Node doesn't have access to it's the filesystem!");
                            Console.WriteLine($"{DateTime.Now:G}: Try running with admin privileges -> 'sudo ./ServerNode'.");
                            Console.ResetColor();
                            throw new ApplicationException("Server Node doesn't have access to it's the filesystem!");
                        }
                    }
                }

                // start a new thread
                Thread thread = new Thread(() =>
                {
                    // cast LogType to a list of LogType, cast each to a string, aggregate to longest string, get length
                    // This could probably be hard-coded to 13 (INFORMATIONAL), but if we add any more that are longer
                    // we'll probably forget about hard-logging files taking this value..
                    int longestTypeLength = Enum.GetValues(typeof(LogType)).Cast<LogType>().Select(x => x.ToString()).Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length;

                    // only log when the program should be running
                    while (Program.ShouldRun)
                    {
                        // take the first item (fifo)
                        LogItem logItem = LogQueue.Take();
                        // set the console forground
                        Console.ForegroundColor = logItem.conColor;
                        // if our settings allow this console write
                        if (logItem.conWrite)
                        {
                            // then print message
                            Console.WriteLine($"{logItem.recordedAt:G}: {logItem.message}");
                        }
                        // reset colour
                        Console.ResetColor();
                        // if our settings allow this hard log
                        if (logItem.hardLog)
                        {
                            if (StreamWriters.ContainsKey("ALL"))
                            {
                                StreamWriters["ALL"].WriteLine($"{logItem.recordedAt:G}: [{logItem.type.ToString().PadRight(longestTypeLength, '-')}] {logItem.message}");
                            }
                            else
                            {
                                StreamWriter writer = new StreamWriter(Path.Combine(hardLogDirectoryPath, "_ALL.log"), true);
                                StreamWriters.Add("ALL", writer);
                                StreamWriters["ALL"].WriteLine($"{logItem.recordedAt:G}: [{logItem.type.ToString().PadRight(longestTypeLength, '-')}] {logItem.message}");
                            }

                            if (StreamWriters.ContainsKey(logItem.type.ToString()))
                            {
                                StreamWriters[logItem.type.ToString()].WriteLine($"{logItem.recordedAt:G}: [{logItem.type.ToString().PadRight(longestTypeLength, '-')}] {logItem.message}");
                            }
                            else
                            {
                                StreamWriter writer = new StreamWriter(Path.Combine(hardLogDirectoryPath, $"{logItem.type}.log"), true);
                                StreamWriters.Add(logItem.type.ToString(), writer);
                                StreamWriters[logItem.type.ToString()].WriteLine($"{logItem.recordedAt:G}: [{logItem.type.ToString().PadRight(longestTypeLength, '-')}] {logItem.message}");
                            }
                        }
                    }
                });

                // thread should work in the background
                thread.IsBackground = true;
                // start the thread
                thread.Start();
            }
        }

        /// <summary>
        /// Prints to Console in Gray
        /// </summary>
        /// <param name="value"></param>
        internal static void Verbose(object value)
        {
            if (Options[LogType.VERBOSE].Item1)
            {
                LogItem logItem = new LogItem(value.ToString(), LogType.VERBOSE, Options[LogType.VERBOSE]);
                LogQueue.Add(logItem);
            }
        }

        /// <summary>
        /// Prints to Console in Green
        /// </summary>
        /// <param name="value"></param>
        internal static void Success(object value)
        {
            if (Options[LogType.SUCCESS].Item1)
            {
                LogItem logItem = new LogItem(value.ToString(), LogType.SUCCESS, Options[LogType.SUCCESS]);
                LogQueue.Add(logItem);
            }
        }

        /// <summary>
        /// Prints to Console in White
        /// </summary>
        /// <param name="value"></param>
        internal static void Informational(object value)
        {
            if (Options[LogType.INFORMATIONAL].Item1)
            {
                LogItem logItem = new LogItem(value.ToString(), LogType.INFORMATIONAL, Options[LogType.INFORMATIONAL]);
                LogQueue.Add(logItem);
            }
        }

        /// <summary>
        /// Prints to Console in DarkYellow
        /// </summary>
        /// <param name="value"></param>
        internal static void Warning(object value)
        {
            if (Options[LogType.WARNINGS].Item1)
            {
                LogItem logItem = new LogItem(value.ToString(), LogType.WARNINGS, Options[LogType.WARNINGS]);
                LogQueue.Add(logItem);
            }
        }

        /// <summary>
        /// Prints to Console in DarkRed
        /// </summary>
        /// <param name="value"></param>
        internal static void Error(object value)
        {
            if (Options[LogType.ERRORS].Item1)
            {
                LogItem logItem = new LogItem(value.ToString(), LogType.ERRORS, Options[LogType.ERRORS]);
                LogQueue.Add(logItem);
            }
        }

        /// <summary>
        /// Prints to Console in Magenta, prefixed with "### DEBUG TRACE:"
        /// </summary>
        /// <param name="value"></param>
        internal static void Debug(object value)
        {
            if (Options[LogType.DEBUGGING].Item1)
            {
                LogItem logItem = new LogItem("### DEBUG TRACE: " + value.ToString(), LogType.DEBUGGING, Options[LogType.DEBUGGING]);
                LogQueue.Add(logItem);
            }
        }
    }
}
