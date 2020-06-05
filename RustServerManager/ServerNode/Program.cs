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
        internal static string WorkingDirectory { get; private set; }

        /// <summary>
        /// Working Directory for Server Node Game Servers
        /// </summary>
        internal static string GameServersDirectory { get => Path.Combine(WorkingDirectory, "gameservers"); }

        /// <summary>
        /// Working Directory for Server Node Pre-Installed Apps
        /// </summary>
        internal static string AppsDirectory { get => Path.Combine(WorkingDirectory, "apps"); }

        /// <summary>
        /// Working Directory for Server Node Logs
        /// </summary>
        internal static string LogsDirectory { get => Path.Combine(WorkingDirectory, "logs"); }

        /// <summary>
        /// Whether Server Node should be running
        /// </summary>
        internal static bool ShouldRun { get; set; } = true;

        /// <summary>
        /// Task for interupting command input and stealing the input for elsewhere
        /// </summary>
        internal static TaskCompletionSource<string> InteruptedInput { get; set; }

        /// <summary>
        /// Whether the safe exit cleanup procedure has completed
        /// </summary>
        private static bool _safeExitComplete = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private static void Main(string[] args)
        {
            WorkingDirectory = Directory.GetCurrentDirectory();

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
                    bool? getyn()
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
                    { LogType.VERBOSE, (true, true, ConsoleColor.Gray) },
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

            Directory.CreateDirectory(AppsDirectory);
            Directory.CreateDirectory(GameServersDirectory);
            Directory.CreateDirectory(LogsDirectory);

            // create some apps for us to test with
            // create css template - 232330
            PreAPIHelper.CreateApp(
                "Counter Strike: Source",
                "css",
                "srcds.exe",
                "srcds_run",
                232330,
                true,
                new string[] {
                    "-console",
                    "-game cstrike",
                    "+map de_dust2",
                    "+maxplayers 10"
                });

            // create rust tempate - 258550
            PreAPIHelper.CreateApp("Rust",
                "rust",
                "RustDedicated.exe",
                "RustDedicated",
                258550,
                false,
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

            // create csgo template - 740
            PreAPIHelper.CreateApp("Counter Strike: Global Offensive",
                "csgo",
                "srcds.exe",
                "srcds_run",
                740,
                false,
                new string[] {
                    "-console",
                    "-game csgo",
                    "+map de_dust2",
                    "+maxplayers 10"
                });

            // make sure that steamcmd is available
            SteamCMD.EnsureAvailable();

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
                    // pass it along, and continue our loop
                    InteruptedInput.TrySetResult(input);
                    continue;
                }

                ParseCommand(input);
            }
        }

        private static void ParseCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            string[] brokenDown = command.Split(' ');

            if (brokenDown.Count() == 1)
            {
                ExecuteCommand(brokenDown[0]);
            }
            else if (brokenDown[1] == "--help")
            {
                ShowHelp(brokenDown[0]);
            }
            else if (brokenDown.Count() == 2)
            {
                ExecuteCommand(brokenDown[0], brokenDown[1]);
            }
            else if (brokenDown.Count() == 3)
            {
                string targets = brokenDown[2];
                string[] split_targets = targets.Split(',');
                string[] clean_split_targets = split_targets.Select(x => x.Trim()).ToArray();
                ExecuteCommand(brokenDown[0], brokenDown[1], clean_split_targets);
            }
        }

        private static void ExecuteCommand(string command)
        {
            switch (command)
            {
                case "quit":
                    SafeExit();
                    break;

                case "exit":
                    SafeExit();
                    break;

                case "server":
                    Console.WriteLine($"Command <{command}> requires an action. Type '{command} --help' for more info.");
                    break;

                case "apps":
                    Console.WriteLine($"Command <{command}> requires an action. Type '{command} --help' for more info.");
                    break;

                case "help":
                    ShowHelp("help");
                    break;

                default:
                    Console.WriteLine($"Command <{command}> not recognised.");
                    Console.WriteLine($"Commands available: <exit>, <quit>, <logs>, <server>, <apps>, <help>.");
                    break;
            }
        }

        private static void ExecuteCommand(string command, string action, string[] targetid = null)
        {
            switch (command)
            {
                case "server":
                    ExecuteServerAction(action, targetid);
                    break;

                case "apps":
                    ExecuteAppsAction(action, targetid);
                    break;

                case "logs":
                    ExecuteLogsAction(action, targetid);
                    break;

                case "help":
                    ShowHelp("help");
                    break;

                default:
                    Console.WriteLine($"Command <{command}> not recognised.");
                    Console.WriteLine($"Commands available: <quit>, <logs>, <server>, <apps>, <help>.");
                    break;
            }
        }

        private static void ExecuteLogsAction(string action, string[] targets = null)
        {
            void enableLogs(string logType)
            {
                foreach (LogType trueType in Enum.GetValues(typeof(LogType)).Cast<LogType>())
                {
                    if (logType.ToUpper() == trueType.ToString())
                    {
                        Log.Informational($"Enabling Log Type: {trueType}");
                        Log.Options[trueType] = (true, Log.Options[trueType].Item2, Log.Options[trueType].Item3);
                        return;
                    }
                }

                Log.Warning($"Attempted to enable log type <{logType}>, but it wasn't found!");
            }

            void disableLogs(string logType)
            {
                foreach (LogType trueType in Enum.GetValues(typeof(LogType)).Cast<LogType>())
                {
                    if (logType.ToUpper() == trueType.ToString())
                    {
                        Log.Informational($"Disabling Log Type: {trueType}");
                        Log.Options[trueType] = (false, Log.Options[trueType].Item2, Log.Options[trueType].Item3);
                        return;
                    }
                }

                Log.Warning($"Attempted to enable log type <{logType}>, but it wasn't found!");
            }

            switch (action)
            {
                case "enable":
                    foreach (string type in targets)
                    {
                        enableLogs(type);
                    }
                    break;

                case "disable":
                    foreach (string type in targets)
                    {
                        disableLogs(type);
                    }
                    break;

                default:

                    Log.Warning($"The <logs> command only allows actions: <enable>, <disable>");
                    break;
            }
        }

        private static void ExecuteAppsAction(string action, string[] targetid)
        {
            bool findApp(string shortName, out SteamApp steamApp)
            {
                if (PreAPIHelper.Apps.ContainsKey(shortName))
                {
                    steamApp = PreAPIHelper.Apps[shortName];
                    return true;
                }

                steamApp = null;
                return false;
            }

            if (targetid == null)
            {
                switch (action)
                {
                    case "list":
                        PreAPIHelper.Apps.Select(x => x.Value).ToList().ForEach(x => Console.WriteLine($"App: {x.ShortName} : {x.Name}"));
                        break;

                    case "view":
                        Console.WriteLine($"Apps action <{action}> requires target shortname - apps view <appshortname>");
                        break;

                    case "install":
                        Console.WriteLine($"Apps action <{action}> requires target shortname - apps install <appshortname>");
                        break;

                    default:
                        Console.WriteLine($"Server action <{action}> not recognised");
                        break;
                }
            }
            else
            {
                foreach (string id in targetid)
                {
                    switch (action)
                    {
                        case "list":
                            Console.WriteLine($"Apps action <{action}> requires no target - apps list");
                            break;

                        case "view":
                            Console.WriteLine($"TODO");
                            break;

                        case "install":
                            if (findApp(id, out SteamApp steamApp))
                            {
                                Task.Run(async() => { await steamApp.InstallAsync(); });
                            }
                            break;

                        default:
                            Console.WriteLine($"Server action <{action}> not recognised");
                            break;
                    }
                }
            }
        }

        private static void ExecuteServerAction(string action, string[] targets = null)
        {
            static bool parseID(string target, out int id)
            {
                try
                {
                    id = Convert.ToInt32(target);
                    return true;
                }
                catch (Exception)
                {
                    id = -1;
                    Console.WriteLine($"Failed to convert <{target}> into an number for selecting server by ID.");
                    return false;
                }
            }

            static bool findServer(int id, out Server server)
            {
                try
                {
                    if (PreAPIHelper.Servers.Exists(x => x.ID == id))
                    {
                        server = PreAPIHelper.Servers.FirstOrDefault(x => x.ID == id);
                        return true;
                    }
                    else
                    {
                        server = null;
                        Console.WriteLine($"Failed to find server with ID <{id}>.");
                        return false;
                    }
                }
                catch (Exception)
                {
                    server = null;
                    Console.WriteLine($"Failed to find server with ID <{id}>.");
                    return false;
                }
            }

            static void cleanupServers()
            {
                Log.Verbose("Server Clean Up - Initiated...");

                foreach (string dir in Directory.EnumerateDirectories(GameServersDirectory))
                {
                    Log.Verbose($"Server Clean Up - Found Directory: {dir}");
                    // get full directory info of target
                    DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                    // if the folder name only contains digits, it matches our norm..
                    if (directoryInfo.Name.IsDigitsOnly())
                    {
                        // get integer value of id
                        int gsID = Convert.ToInt32(directoryInfo.Name);
                        // do we NOT have a server with this id?
                        if (!PreAPIHelper.Servers.Exists(x => x.ID == gsID))
                        {
                            // delete the folder, we don't manage this
                            Log.Informational($"Server Cleanup: Deleting Directory {directoryInfo.FullName}");
                            DirectoryExtensions.DeleteOrTimeout(directoryInfo.FullName);
                        }
                        else
                        {
                            Log.Verbose($"Server Clean Up - This directory is being managed, ignoring.");
                        }
                    }
                    else
                    {
                        Log.Verbose($"Server Clean Up - Directory doesn't match our system, ignoring.");
                    }
                }
            }

            if (targets == null)
            {
                string[] actionsRequiringTarget = new string[]
                {
                    "create", "start", "stop", "kill", "install", "update", "uninstall", "reinstall"
                };

                switch (action)
                {
                    case "list":
                        PreAPIHelper.Servers.ForEach(x => Console.WriteLine($"Server ({x.ID:00}) : {(x.IsRunning ? "ONLINE " : "OFFLINE")} : {x.App.Name}"));
                        break;

                    case "cleanup":
                        cleanupServers();
                        break;

                    default:
                        if (actionsRequiringTarget.Contains(action))
                        {
                            Console.WriteLine($"Server action <{action}> requires target. Type 'server --help' for more info.");
                        }
                        else
                        {
                            Console.WriteLine($"Server action <{action}> not recognised");
                        }
                        break;
                }
            }
            else
            {
                foreach (string target in targets)
                {
                    if (action == "list")
                    {
                        PreAPIHelper.Servers.Where(x => x.App.ShortName == target).ToList().ForEach(x => Console.WriteLine($"Server ({x.ID:00}) : {(x.IsRunning ? "ONLINE " : "OFFLINE")} : {x.App.Name}"));
                        return;
                    }
                    else if (action == "create" && targets.Length == 1)
                    {
                        if (PreAPIHelper.Apps.ContainsKey(target))
                        {
                            SteamApp app = PreAPIHelper.Apps[target];
                            Server server = PreAPIHelper.CreateServer(app);
                        }
                        else
                        {
                            Console.WriteLine($"Couldn't create server with app <{string.Join("", targets)}> - app not found.");
                        }
                    }
                    else if (parseID(target, out int id) && findServer(id, out Server server))
                    {
                        switch (action)
                        {
                            case "start":
                                Task.Run(async () => { await server.StartAsync(); });
                                break;

                            case "stop":
                                Task.Run(async () => { await server.StopAsync(); });
                                break;

                            case "kill":
                                Task.Run(async () => { await server.KillAndWaitForExitAsync(); });
                                break;

                            case "install":
                                Task.Run(async () => { await server.InstallAsync(); });
                                break;

                            case "update":
                                Task.Run(async () => { await server.UpdateAsync(); });
                                break;

                            case "uninstall":
                                Task.Run(async () => { await server.UninstallAsync(); });
                                break;

                            case "reinstall":
                                Task.Run(async () => { await server.ReinstallAsync(); });
                                break;

                            case "delete":
                                Task.Run(async () => { await server.DeleteAsync(); });
                                break;

                            default:
                                Console.WriteLine($"Server action <{action}> not recognised");
                                break;
                        }
                    }
                }
            }
        }

        private static void ShowHelp(string command)
        {
            switch (command)
            {
                case "help":
                    WrapHelpInfo(
                        "Available Commands",
                        "Help",
                        "Use these commands to control your server node. <command> <action> <id>\n" +
                        "Single Target: <command> <action> <id>\n" +
                        "Multi Target: <command> <action> <id,id,id>\n" +
                        "Target All: <command> <action> *",
                        new Dictionary<string, string> {
                            { "quit | exit", "Safely exit Server Node, cleaning up active servers and data." },
                            { "server <action>", "Control a server, use 'server --help' for more info." },
                            { "apps <action>", "Apps Information, use 'server --help' for more info." },
                            { "<command> --help", "View more in depth help on a specific command." }
                        });
                    break;

                case "exit":
                    WrapHelpInfo(
                        "Exit",
                        "Commands",
                        "The <exit> command will clean up any loose ends before shutting down. (Highly recommended)",
                        new Dictionary<string, string>());
                    break;

                case "quit":
                    WrapHelpInfo(
                        "Quit",
                        "Commands",
                        "The <quit> command will clean up any loose ends before shutting down. (Highly recommended)",
                        new Dictionary<string, string>());
                    break;

                case "server":
                    WrapHelpInfo(
                        "Server Commands",
                        "server",
                        "The <server> command is for controlling servers, such as installations, starting and stopping.",
                        new Dictionary<string, string> {
                            { "list", "lists all servers on this server node" },
                            { "cleanup", "removes any server folders that are un-used" },
                            { "list <appshortname>", "filtered list by app shortname (e.g. server list rust)" },
                            { "create <appshortname>", "create a new server with the app" },
                            { "start <id>", "starts a server by id" },
                            { "stop <id>", "starts a server by id" },
                            { "kill <id>", "kills a server by id" },
                            { "install <id>", "installs a server by id" },
                            { "update <id>", "updates a server by id" },
                            { "uninstall <id>", "uninstalls a server by id" },
                            { "reinstall <id>", "reinstalls a server by id" },
                            { "delete <id>", "deletes a server by id" },
                        });
                    break;

                case "apps":
                    WrapHelpInfo(
                        "App Commands",
                        "apps",
                        "The <apps> command is for viewing data on available apps.",
                        new Dictionary<string, string> {
                            { "view <shortname>", "view app information" },
                            { "list", "lists all apps available by shortname:longname" },
                        });
                    break;

                case "logs":
                    WrapHelpInfo(
                        "Logs Commands",
                        "logs",
                        "The <logs> command is for modifying the visibility of different log types.\n" +
                        "Log types available:\n" +
                        "verbose, informational, success, warnings, errors, debugging",
                        new Dictionary<string, string> {
                            { "enable <logtype>", "enable the specific log type" },
                            { "disable <logtype>", "disable the specific log type" },
                        });
                    break;

                default:
                    break;
            }
        }

        private static void WrapHelpInfo(string title, string command, string description, Dictionary<string, string> actions)
        {
            Console.WriteLine($"// ".PadRight(98, '-') + " //");
            Console.WriteLine($"// {title}".PadRight(98, ' ') + " //");
            Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            foreach (string line in description.Split("\n"))
            {
                Console.WriteLine($"// {line.Replace("\n", "")}".PadRight(98, ' ') + " //");
            }
            Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            Console.WriteLine($"// Command -> {command}".PadRight(98, ' ') + " //");
            Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            foreach (var action in actions)
            {
                Console.WriteLine($"// Action -> {action.Key}".PadRight(98, ' ') + " //");
                Console.WriteLine($"//        -> {action.Value}".PadRight(98, ' ') + " //");
                Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            }
            Console.WriteLine($"// ".PadRight(98, '-') + " //");
        }

        /// <summary>
        /// Perform clean up tasks
        /// </summary>
        private static void SafeExit(bool onexit = false)
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
    }
}
