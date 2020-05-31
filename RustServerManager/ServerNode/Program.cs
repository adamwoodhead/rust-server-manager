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

            PreAPIHelper.CreateApp(
                "Counter Strike: Source",
                "css",
                "srcds.exe",
                "srcds",
                232330,
                new string[] { "-game css", "+map de_dust2", "+maxplayers 10" },
                new string[] { });

            PreAPIHelper.CreateApp("Rust",
                "rust",
                "RustDedicated.exe",
                "RustDedicated",
                258550,
                new string[] {  $"-batchmode",
                                $"+server.ip 0.0.0.0",
                                $"+server.port 28015",
                                $"+server.tickrate 10",
                                $"+server.hostname \"A New Rust Server\"",
                                $"+server.identity server",
                                $"+server.seed 12345",
                                $"+server.maxplayers 100",
                                $"+server.worldsize 3500",
                                $"+server.saveinterval 300",
                                $"+rcon.ip 0.0.0.0",
                                $"+rcon.port 28016",
                                $"+rcon.password \"apassword\"",
                                $"+rcon.web 1"
                },
                new string[] { "Checking for new Steam Item Definitions.." });

            SteamCMD.EnsureAvailable();

            // TESTING ONLY
            Task.Run(async () =>
            {
                try
                {
                    Server server = PreAPIHelper.CreateServer(PreAPIHelper.Apps["rust"]);

                    if (await server.Install() && await server.Start())
                    {
                        Log.Success("Wahoooo!");
                    }

                    //await server.ReadyForInputTsk.Task;

                    await server.SendCommand("playerlist");
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
