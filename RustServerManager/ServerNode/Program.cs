using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Models.Servers.Extensions;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode
{
    class Program
    {
        /// <summary>
        /// Working Directory for Server Node
        /// </summary>
        public static string WorkingDirectory { get; private set; }

        /// <summary>
        /// Working Directory for Server Node Game Servers
        /// </summary>
        public static string GameServersDirectory { get => Path.Combine(WorkingDirectory, "gameservers"); }

        /// <summary>
        /// Working Directory for Server Node Pre-Installed Apps
        /// </summary>
        public static string AppsDirectory { get => Path.Combine(WorkingDirectory, "apps"); }

        /// <summary>
        /// Working Directory for Server Node Logs
        /// </summary>
        public static string LogsDirectory { get => Path.Combine(WorkingDirectory, "logs"); }

        /// <summary>
        /// Whether Server Node should be running
        /// </summary>
        public static bool ShouldRun { get; set; } = true;

        /// <summary>
        /// Task for interupting command input and stealing the input for elsewhere
        /// </summary>
        public static TaskCompletionSource<string> InteruptedInput { get; set; }

        /// <summary>
        /// Whether the safe exit cleanup procedure has completed
        /// </summary>
        private static bool _safeExitComplete = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private static void Main(string[] args)
        {
            WorkingDirectory = Directory.GetCurrentDirectory();

            ParseArguments(args);

            // only in windows debug mode
            if (Debugger.IsAttached && Utility.OperatingSystemHelper.IsWindows())
            {
                WorkingDirectory = @"C:\servernode";
                // no harm in it..
                Directory.CreateDirectory(WorkingDirectory);
            }

            Log.Initialise(
                new Dictionary<LogType, (bool, bool, ConsoleColor)>() // Visibility Options
                {
                    { LogType.VERBOSE, (true, false, ConsoleColor.Gray) },
                    { LogType.INFORMATIONAL, (true, true, ConsoleColor.White) },
                    { LogType.SUCCESS, (true, true, ConsoleColor.Green) },
                    { LogType.WARNINGS, (true, true, ConsoleColor.DarkYellow) },
                    { LogType.ERRORS, (true, true, ConsoleColor.Red) },
                    { LogType.DEBUGGING, (true, false, ConsoleColor.Magenta) },
                },
                5 // Hard Logs Count
            );

            Log.Informational("Server Node Booting Up");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => { SafeExit(true); };

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    e.Cancel = true; // tell the CLR to keep running
                }
                else if (e.SpecialKey == ConsoleSpecialKey.ControlBreak)
                {
                    //e.Cancel = true; // "Applications are not allowed to cancel the ....
                }

                SafeExit();
            };

            CheckLinuxRequirements();

            Directory.CreateDirectory(AppsDirectory);
            Directory.CreateDirectory(GameServersDirectory);
            Directory.CreateDirectory(LogsDirectory);

            // make sure that steamcmd is available
            SteamCMD.EnsureAvailable();

            // run our debugging creations
            FOR__TESTING__ONLY();

            Log.Success("Server Node Booted");

            Log.Informational("Type 'help' to view available commands.");

            while (ShouldRun)
            {
                string input = Console.ReadLine()?.Trim() ?? null;

                if (input != "" && input == null)
                {
                    break;
                }

                // if something is trying to steal input...
                if (InteruptedInput != null && !InteruptedInput.Task.IsCompleted)
                {
                    // pass it along, and continue iterating our loop exiting at this point
                    InteruptedInput.TrySetResult(input);
                    continue;
                }

                EntryPoints.Console.ParseCommand(input).ConfigureAwait(false);
            }
        }

        private static void ParseArguments(string[] args)
        {

            // Lets check that we've executed servernode from it's directory..
            if (!args.Contains("-skip-directory-check"))
            {
                string executableName = Utility.OperatingSystemHelper.IsWindows() ? "ServerNode.exe" : "ServerNode";
                bool insideExecutablesFolder = false;

                foreach (string filestr in Directory.EnumerateFiles(WorkingDirectory))
                {
                    FileInfo file = new FileInfo(filestr);
                    if (file.Name == executableName)
                    {
                        insideExecutablesFolder = true;
                        break;
                    }
                }

                if (!insideExecutablesFolder)
                {
                    static bool? getyn()
                    {
                        string responded = Console.ReadLine();

                        switch (responded)
                        {
                            case "y":
                                return true;
                            case "n":
                                return false;
                            default:
                                return null;
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"WARNING: YOU HAVE STARTED SERVERNODE IN ANOTHER DIRECTORY THAN IT'S OWN");
                    Console.WriteLine($"WARNING: '{WorkingDirectory}' WILL BE THE WORKING DIRECTORY!");
                    Console.WriteLine($"Do you want to continue?");

                    bool? answer = getyn();
                    while (answer == null)
                    {
                        answer = getyn();
                    }

                    if (!answer.Value)
                    {
                        Console.WriteLine("Please launch in the correct directory.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Continuing launch in current directory, don't say we didn't warn you!.");
                    }
                }
            }
        }

        /// <summary>
        /// Perform clean up tasks
        /// </summary>
        public static void SafeExit(bool onexit = false)
        {
            // the process has exit, we have a *very* limited amount of time before the memory is cleared
            // so we need to be extremely quick in our actions, and use zero tasks (background workers)
            if (onexit)
            {
                // loop servers that have a process id active
                foreach (Server server in PreAPIHelper.Servers.Where(x => x.PID != null))
                {
                    // try to get the process id and kill it instantly
                    // if we dont get a process from the pid, this trycatch helps us out
                    // but the exception is not adhered to
                    try 
                    { 
                        Process.GetProcessById(server.PID.Value)?.Kill(); 
                    }
                    catch (Exception)
                    { }
                }
            }
            // the kind user typed quit or exit
            // we can now shutdown in a normal manner and inform them of our actions
            else
            {
                ShouldRun = false;

                // if this hasn't be been ran before 
                if (!_safeExitComplete)
                {
                    // now this has been ran
                    _safeExitComplete = true;

                    Log.Informational("Server Node Shutting Down");

                    // then each server
                    foreach (Server server in PreAPIHelper.Servers)
                    {
                        // should stop
                        server.Stop();
                    }

                    Log.Informational("Server Node Shutdown Complete");

                    Environment.Exit(0);
                }
            }
        }

        private static void CheckLinuxRequirements()
        {
            if (Utility.OperatingSystemHelper.IsLinux())
            {
                Log.Informational("Testing for sysstat");
                if (!Native.Linux.Pidstat.IsPidstatAvailable())
                {
                    Log.Error("SysStat is not installed!");
                    Log.Informational("You can install SysStat with the following command:");
                    Log.Informational("sudo apt-get install sysstat");
                    throw new ApplicationException("sysstat must be installed for ServerNode to function fully.");
                }
                else
                {
                    Log.Success("Successfully Collected SysStat Info!");
                }

                Log.Informational("Performing Screen Test");
                if (!Native.Linux.Screens.HasScreenAccess())
                {
                    Log.Error("Server Node does not have access to the screen command.");
                    Log.Informational("If you do not have screen installed, please use the following:");
                    Log.Informational("sudo apt-get update");
                    Log.Informational("sudo apt-get install screen");
                    Log.Informational("If you do have screen installed, please try running sudo ./ServerNode");
                    throw new ApplicationException("ServerNode does not have access to screen command.");
                }
                else
                {
                    Log.Success("Successfully Launched and killed a screen!");
                }
            }
        }

        private static void FOR__TESTING__ONLY()
        {
            // create some apps for us to test with
            // create css template - 232330
            PreAPIHelper.CreateApp(
                "Counter Strike: Source",
                "css",
                27015,
                10,
                "srcds.exe",
                "srcds_run",
                232330,
                false,
                new string[] {
                    "-console",
                    "-game cstrike",
                    "-ip !{IPAddress}",
                    "-port !{Port}",
                    "-maxplayers_override !{Slots}",
                    "+hostname \"!{Hostname}\"",
                    "+map !{Map}",
                },
                new Variable[]
                {
                    new Variable("Map", "de_dust2", true),
                });

            // create rust tempate - 258550
            PreAPIHelper.CreateApp(
                "Rust",
                "rust",
                28015,
                100,
                "RustDedicated.exe",
                "RustDedicated",
                258550,
                false,
                new string[] {
                    "-batchmode",
                    "+server.ip !{IPAddress}",
                    "+server.port !{Port}",
                    "+server.tickrate !{Tickrate}",
                    "+server.hostname \"!{Hostname}\"",
                    "+server.identity !{Identity}",
                    "+server.seed !{Seed}",
                    "+server.maxplayers !{Slots}",
                    "+server.worldsize !{WorldSize}",
                    "+server.saveinterval !{SaveInterval}",
                    "+rcon.ip !{IPAddress}",
                    "+rcon.port !{RconPort}",
                    "+rcon.password \"!{RconPassword}\"",
                    "+rcon.web 1"
                },
                new Variable[]
                {
                    new Variable("Tickrate", "10", true),
                    new Variable("Identity", "server", true),
                    new Variable("Seed", "12345", true),
                    new Variable("WorldSize", "3500", true),
                    new Variable("SaveInterval", "300", true),
                });

            // create csgo template - 740
            PreAPIHelper.CreateApp(
                "Counter Strike: Global Offensive",
                "csgo",
                27015,
                10,
                "srcds.exe",
                "srcds_run",
                740,
                false,
                new string[] {
                    "-console",
                    "-game csgo",
                    "-ip !{IPAddress}",
                    "-port !{Port}",
                    "-maxplayers_override !{Slots}",
                    "+map !{Map}",
                    "+hostname \"!{Hostname}\"",
                },
                new Variable[]
                {
                    new Variable("Map", "de_dust2", true),
                });

            // search gameservers folder for currently existing gameservers...
            foreach (string dir in Directory.EnumerateDirectories(GameServersDirectory))
            {
                // get full directory info of target
                DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                // if the folder name only contains digits, it matches our norm..
                if (directoryInfo.Name.IsDigitsOnly())
                {
                    // get integer value of id
                    int gsID = Convert.ToInt32(directoryInfo.Name);
                    // loop through files in the directory
                    foreach (FileInfo file in directoryInfo.EnumerateFiles())
                    {
                        // find relevant app to this file
                        SteamApp app = PreAPIHelper.Apps.Values.ToList().FirstOrDefault(x => x.RelativeExecutablePath == file.Name) ?? null;
                        // we found a relevant executable
                        if (app != null)
                        {
                            // this is a native app executable!
                            Log.Informational($"Found a native executable related to available app in: {dir}");
                            PreAPIHelper.CreateServer(app, gsID);
                        }
                    }
                }
            }
        }
    }
}
