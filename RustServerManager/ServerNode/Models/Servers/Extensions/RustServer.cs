using ServerNode.Logging;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Models.Servers.Extensions
{
    /// <summary>
    /// Server Extensions for Rust
    /// </summary>
    internal static class RustServer
    {
        /// <summary>
        /// Wipes all the map files in the server identity folder
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> WipeMapAsync(Server server)
        {
            return await Task.Run(() => {
                return WipeMap(server);
            });
        }

        /// <summary>
        /// Wipes all the map files in the server identity folder
        /// </summary>
        /// <returns></returns>
        public static bool WipeMap(Server server)
        {
            if (DeleteServerIdentityFiles(server, "*.map", "*.sav"))
            {
                Log.Success($"Server {server.ID} (rust) Map Wiped Successfully");
                return true;
            }
            else
            {
                Log.Error($"Server {server.ID} (rust) Map Wiped Unsuccessfully");
                return false;
            }
        }

        /// <summary>
        /// Wipes all Player Data in the server identity folder
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static async Task<bool> WipePlayerDataAsync(Server server)
        {
            return await Task.Run(() => {
                return WipePlayerData(server);
            });
        }

        /// <summary>
        /// Wipes all Player Data in the server identity folder
        /// </summary>
        /// <returns></returns>
        public static bool WipePlayerData(Server server)
        {
            if (DeleteServerIdentityFiles(server, "*.db"))
            {
                Log.Success($"Server {server.ID} (rust) Player Data Wiped Successfully");
                return true;
            }
            else
            {
                Log.Error($"Server {server.ID} (rust) Player Data Wiped Unsuccessfully");
                return false;
            }
        }

        /// <summary>
        /// Wipes all Map & Player Data in the server identity folder
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static async Task<bool> FullWipeAsync(Server server)
        {
            return await Task.Run(() => {
                return FullWipe(server);
            });
        }

        /// <summary>
        /// Wipes all Map & Player Data in the server identity folder
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static bool FullWipe(Server server)
        {
            if (DeleteServerIdentityFiles(server, "*.map", "*.sav", "*.db"))
            {
                Log.Success($"Server {server.ID} (rust) Full Wipe Successful");
                return true;
            }
            else
            {
                Log.Error($"Server {server.ID} (rust) Full Wipe Unsuccessful");
                return false;
            }
        }

        /// <summary>
        /// Attempt to delete files inside the identity folder based on the pattern provided, returns true if all files matchs are deleted successfully.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        private static bool DeleteServerIdentityFiles(Server server, params string[] filters)
        {
            // if the server isnt running
            if (!server.IsRunning)
            {
                if (FileExtensions.DeleteOrTimeoutFilteredFilesInDirectory(GetIdendityDirectory(server), filters))
                {
                    Log.Verbose($"Server {server.ID} - (rust) - File Deletion Complete");
                    return true;
                }
                else
                {
                    Log.Warning($"Server {server.ID} - (rust) - File Deletion Incomplete");
                    return false;
                }
            }
            // otherwise
            else
            {
                // we cant wipe when the server is running
                Log.Error($"Server {server.ID} - (rust) - Can't delete server files whilst it's running!");
                return false;
            }
        }

        /// <summary>
        /// Return the directory of the server identity
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        private static string GetIdendityDirectory(Server server)
        {
            // declare identity
            string identity = "";

            // cycle each commandline parameter
            foreach (string parameter in server.CommandLine)
            {
                // if it contains identity
                if (parameter.Contains("+server.identity"))
                {
                    // then split it up by spaces
                    string[] twovars = parameter.Split(' ');

                    // check we only have two strings from our split (+server.identity & the *identity)
                    if (twovars.Count() == 2)
                    {
                        // take the identity part
                        identity = twovars[1];

                        // if the server identity is empty, it's incorrect
                        if (string.IsNullOrEmpty(identity))
                        {
                            Log.Error("Rust Server parameter for identity is incorrect");
                            // as the identity is incorrect, we should cancel here
                            // before we accidentally iterate all identity folders
                            return null;
                        }

                        // break out of loop
                        break;
                    }
                    // if we have more than two strings, our identity string is incorrect
                    else
                    {
                        Log.Error("Rust Server parameter for identity is incorrect");
                        return null;
                    }
                }
            }

            if (!string.IsNullOrEmpty(identity))
            {
                return Path.Combine(server.WorkingDirectory, "server", identity);
            }
            else
            {
                return null;
            }
        }
    }
}
