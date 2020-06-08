using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Models.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Utility
{
    internal static class ConsoleCommands
    {
        public static void ParseCommand(string command)
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
                    Program.SafeExit();
                    break;

                case "exit":
                    Program.SafeExit();
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
            static void enableLogs(string logType)
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

            static void disableLogs(string logType)
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
            static bool findApp(string shortName, out SteamApp steamApp)
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
                                Task.Run(async () => { await steamApp.InstallAsync(); });
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

                foreach (string dir in Directory.EnumerateDirectories(Program.GameServersDirectory))
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
                        PreAPIHelper.Servers.ForEach(x => Console.WriteLine($"Server ({x.ID:00}) : {(x.IsRunning ? "ONLINE " : "OFFLINE")} : {x.App.Name} : {x.IP} : {x.Port} : {x.Hostname}"));
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
            else if ((action == "getvar" || action == "setvar") && parseID(targets[0], out int foundid) && findServer(foundid, out Server foundServer))
            {
                if (action == "getvar")
                {
                    string strvar = targets[1];

                    Console.WriteLine($"Server ({foundServer.ID:00}) : Variable {strvar}={foundServer.Variables.FirstOrDefault(x => x.Name == strvar)?.Value}");

                    return;
                }
                else
                {
                    string strvar = targets[1];
                    string strval = targets[2];

                    if (foundServer.Variables.FirstOrDefault(x => x.Name == strvar) != null)
                    {
                        foundServer.Variables.FirstOrDefault(x => x.Name == strvar).Value = strval;
                        Console.WriteLine($"Server ({foundServer.ID:00}) : Setting Variable {strvar}={strval}");
                    }
                    else
                    {
                        Console.WriteLine($"Server ({foundServer.ID:00}) : Couldn't Find Variable {strvar}");
                    }
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
    }
}
