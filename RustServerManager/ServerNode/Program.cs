using ServerNode.Logging;
using ServerNode.Models.Games;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode
{
    class Program
    {
        internal static string WorkingDirectory { get; private set; }

        internal static string GameServersDirectory { get => Path.Combine(WorkingDirectory, "gameservers"); }

        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        internal static bool ShouldRun { get; set; } = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private static void Main(string[] args)
        {
            Log.Options = new Dictionary<LogType, bool>()
            {
                { LogType.VERBOSE, true },
                { LogType.INFORMATIONAL, true },
                { LogType.SUCCESS, true },
                { LogType.WARNINGS, true },
                { LogType.ERRORS, true },
            };

            Log.Informational("Server Node Booting Up");

            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
                ShouldRun = false;
            };

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // Create Directories
                WorkingDirectory = @"C:\ServerNode";
                Directory.CreateDirectory(WorkingDirectory);
                Directory.CreateDirectory(GameServersDirectory);

                // check if steam cmd is installed
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                // Create Directories
                WorkingDirectory = @"/opt/servernode";
                Directory.CreateDirectory(WorkingDirectory);
                Directory.CreateDirectory(GameServersDirectory);
            }
            else
            {
                throw new Exception("This operating system is not currently supported...");
            }

            PreAPIHelper.CreateApp("Counter Strike: Source",       "css",      "srcds.exe",            232330);
            PreAPIHelper.CreateApp("Rust",                         "rust",     "RustDedicated.exe",    258550);

            SteamCMD.EnsureAvailable();

            // TESTING ONLY
            Task.Run(async () =>
            {
                try
                {
                    Server server = PreAPIHelper.CreateServer(PreAPIHelper.Apps["rust"]);

                    server.CommandLine = "-batchmode";

                    if (await server.Start())
                    {
                        Log.Success("Wahoooo!");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw;
                }
                
            });

            // asyncronously ping for requests
            // TODO get requests

            _quitEvent.WaitOne();

            Log.Informational("Server Node Shutting Down");

            // cleanup/shutdown and quit
        }
    }
}
