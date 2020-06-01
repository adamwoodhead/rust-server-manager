﻿using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private static readonly ManualResetEvent _quitEvent = new ManualResetEvent(false);

        internal static bool ShouldRun { get; set; } = true;

        private static bool _safeExitComplete = false;

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
                { LogType.DEBUGGING, true },
            };

            Log.Informational("Server Node Booting Up");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => { SafeExit(); };

            Console.CancelKeyPress += (sender, eArgs) => {
                SafeExit();

                _quitEvent.Set();

                eArgs.Cancel = true;
                ShouldRun = false;
            };

            WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                WorkingDirectory = @"C:\ServerNode";
            }
            
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // Create Directories
                Directory.CreateDirectory(GameServersDirectory);

                // check if steam cmd is installed
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                // Create Directories
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
                "srcds_run",
                232330,
                new string[] {  
                    "-console",
                    "-game cstrike",
                    "+map de_dust2",
                    "+maxplayers 10"
                });

            PreAPIHelper.CreateApp("Rust",
                "rust",
                "RustDedicated.exe",
                "RustDedicated",
                258550,
                new string[] {
                    $"-batchmode",
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
                });

            SteamCMD.EnsureAvailable();

            // TESTING ONLY
            Task.Run(async () =>
            {
                try
                {
                    Server server1 = PreAPIHelper.CreateServer(PreAPIHelper.Apps["css"]);
                    Server server2 = PreAPIHelper.CreateServer(PreAPIHelper.Apps["rust"]);

                    //await server1.InstallAsync();

                    //await server2.InstallAsync();

                    Log.Debug("Starting Server 0");
                    await server1.StartAsync();

                    Log.Debug("Starting Server 0");
                    await server1.StartAsync();

                    Log.Debug("Restarting Server 0");
                    await server1.RestartAsync();

                    Log.Debug("Delaying 10 seconds");
                    await Task.Delay(10000);

                    Log.Debug("Server 0 Keep Alive = false");
                    server1.KeepAlive = false;

                    Log.Debug("Killed Server 0 Process Externally (emulated)");
                    server1.GameProcess.Kill();

                    Log.Debug("Delaying 10 seconds");
                    await Task.Delay(10000);

                    Log.Debug("Starting Server 0");
                    await server1.StartAsync();

                    Log.Debug("Delaying 3 seconds");
                    await Task.Delay(3000);

                    Log.Debug("Server 0 Keep Alive = true");
                    server1.KeepAlive = true;

                    Log.Debug("Killed Server 0 Process Externally");
                    server1.GameProcess.Kill();

                    Log.Debug("Delaying 0.5 seconds");
                    await Task.Delay(500);

                    Log.Debug("Stopping Server 0");
                    server1.Stop();

                    //await server2.StopAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    throw;
                }
                
            });

            // asyncronously ping for requests
            // TODO get requests

            _quitEvent.WaitOne();
        }

        /// <summary>
        /// Perform clean up tasks
        /// </summary>
        private static void SafeExit()
        {
            if (!_safeExitComplete)
            {
                Log.Informational("Server Node Shutting Down");

                foreach (Server server in PreAPIHelper.Servers)
                {
                    server.Stop();
                }

                Log.Informational("Server Node Shutdown Complete");
            }

            _safeExitComplete = true;
        }
    }
}
