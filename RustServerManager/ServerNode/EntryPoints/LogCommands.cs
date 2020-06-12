using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.EntryPoints
{
    internal static class LogCommands
    {
        public static async Task Consume(string[] arguments)
        {
            string command = "";
            string[] parameters = new string[] { };

            if (arguments.Count() > 0)
            {
                command = arguments[0];

                parameters = arguments.Skip(1).ToArray();
            }

            switch (command)
            {
                case "enable":
                    foreach (string parameter in parameters)
                    {
                        await EnableLogs(parameter);
                    }
                    break;

                case "disable":
                    foreach (string parameter in parameters)
                    {
                        await DisableLogs(parameter);
                    }
                    break;

                default:
                    System.Console.WriteLine($"App Command <{command}> not recognised.");
                    break;
            }
        }

        private static Task EnableLogs(string logType)
        {
            return Task.Run(() => {
                foreach (LogType trueType in Enum.GetValues(typeof(LogType)).Cast<LogType>())
                {
                    if (logType.ToUpper() == trueType.ToString())
                    {
                        System.Console.WriteLine($"Enabling Log Type: {trueType}");
                        Log.Options[trueType] = (true, Log.Options[trueType].Item2, Log.Options[trueType].Item3);
                        return;
                    }
                }

                Log.Warning($"Attempted to enable log type <{logType}>, but it wasn't found!");
            });
        }

        private static Task DisableLogs(string logType)
        {
            return Task.Run(() => {
                foreach (LogType trueType in Enum.GetValues(typeof(LogType)).Cast<LogType>())
                {
                    if (logType.ToUpper() == trueType.ToString())
                    {
                        System.Console.WriteLine($"Disabling Log Type: {trueType}");
                        Log.Options[trueType] = (false, Log.Options[trueType].Item2, Log.Options[trueType].Item3);
                        return;
                    }
                }

                Log.Warning($"Attempted to enable log type <{logType}>, but it wasn't found!");
            });
        }
    }
}
