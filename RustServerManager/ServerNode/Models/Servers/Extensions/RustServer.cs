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
            Variable variable = server.Variables.FirstOrDefault(x => x.Name == "Identity");

            if (variable != null)
            {
                string identity = variable.Value;

                return Path.Combine(server.WorkingDirectory, "server", identity);
            }

            return null;
        }
    }
}
