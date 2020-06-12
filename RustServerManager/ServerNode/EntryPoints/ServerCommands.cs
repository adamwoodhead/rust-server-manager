using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Models.Steam;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.EntryPoints
{
    internal static class ServerCommands
    {
        public static void Consume(string[] arguments)
        {
            string command = "";
            string[] parameters = new string[] { };

            if (arguments.Count() > 0)
            {
                command = arguments[0];

                parameters = arguments.Skip(1).ToArray();
            }

            foreach (string parameter in parameters)
            {
                if (command == "list")
                {
                    PreAPIHelper.Servers.Where(x => x.App.ShortName == parameter).ToList().ForEach(x => System.Console.WriteLine($"Server ({x.ID:00}) : {(x.IsRunning ? "ONLINE " : "OFFLINE")} : {x.App.Name}"));
                    return;
                }
                else if (command == "create")
                {
                    if (PreAPIHelper.Apps.ContainsKey(parameter))
                    {
                        SteamApp app = PreAPIHelper.Apps[parameter];
                        Server server = PreAPIHelper.CreateServer(app);
                    }
                    else
                    {
                        System.Console.WriteLine($"Couldn't create server with app <{parameter}> - app not found.");
                    }
                }
                else if (ParseID(parameter, out int id) && FindServer(id, out Server server))
                {
                    switch (command)
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
                            System.Console.WriteLine($"Server action <{command}> not recognised");
                            break;
                    }
                }
            }
        }

        private static bool ParseID(string target, out int id)
        {
            try
            {
                id = Convert.ToInt32(target);
                return true;
            }
            catch (Exception)
            {
                id = -1;
                System.Console.WriteLine($"Failed to convert <{target}> into an number for selecting server by ID.");
                return false;
            }
        }

        private static bool FindServer(int id, out Server server)
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
                    System.Console.WriteLine($"Failed to find server with ID <{id}>.");
                    return false;
                }
            }
            catch (Exception)
            {
                server = null;
                System.Console.WriteLine($"Failed to find server with ID <{id}>.");
                return false;
            }
        }

        private static void CleanupServers()
        {
            Log.Informational("Server Clean Up - Initiated...");

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
    }
}
