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

namespace ServerNode.EntryPoints.Console
{
    internal static class ServerCommands
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


            if (parameters.Length == 0)
            {
                if (command == "list")
                {
                    PreAPIHelper.Servers.ForEach(x => Log.Informational($"Server ({x.ID:00}) : {(x.IsRunning ? "ONLINE " : "OFFLINE")} : {x.App.Name}"));
                    return;
                }
            }

            foreach (string parameter in parameters)
            {
                if (command == "list")
                {
                    PreAPIHelper.Servers.Where(x => x.App.ShortName == parameter).ToList().ForEach(x => Log.Informational($"Server ({x.ID:00}) : {(x.IsRunning ? "ONLINE " : "OFFLINE")} : {x.App.Name}"));
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
                            await server.StartAsync();
                            break;

                        case "restart":
                            await server.RestartAsync();
                            break;

                        case "stop":
                            await server.StopAsync();
                            break;

                        case "kill":
                            await server.KillAndWaitForExitAsync();
                            break;

                        case "install":
                            await server.InstallAsync();
                            break;

                        case "update":
                            await server.UpdateAsync();
                            break;

                        case "uninstall":
                            await server.UninstallAsync();
                            break;

                        case "reinstall":
                            await server.ReinstallAsync();
                            break;

                        case "delete":
                            await server.DeleteAsync();
                            break;

                        case "cleanup":
                            await CleanupServers();
                            break;

                        default:
                            Log.Error($"Server Command <{command}> not recognised");
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
                Log.Error($"Failed to convert <{target}> into an number for selecting server by ID.");
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
                    Log.Error($"Failed to find server with ID <{id}>.");
                    return false;
                }
            }
            catch (Exception)
            {
                server = null;
                Log.Error($"Failed to find server with ID <{id}>.");
                return false;
            }
        }

        private static async Task CleanupServers()
        {
            await Task.Run(() => {
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
            });
        }
    }
}
