using ServerNode.Logging;
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
        internal static async Task<bool> WipeMapAsync(Server server)
        {
            return await Task.Run(() => {
                return WipeMap(server);
            });
        }

        /// <summary>
        /// Wipes all the map files in the server identity folder
        /// </summary>
        /// <returns></returns>
        internal static bool WipeMap(Server server)
        {
            // if the server isnt running
            if (!server.IsRunning)
            {
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

                            // break out of loop
                            break;
                        }
                        else
                        {
                            Log.Error("Rust Server parameter for identity is incorrect");
                        }
                    }
                }

                // find the map file(s)
                foreach (string file in Directory.EnumerateFiles(Path.Combine(server.WorkingDirectory, "server", identity), "*.map"))
                {
                    // lets double check that the file path is correct
                    if (File.Exists(file))
                    {
                        // delete the map file
                        File.Delete(file);
                        Log.Success($"Server {server.ID} - (rust) - Map Deleted ({new FileInfo(file).Name})");
                    }
                }
                
                return true;
            }
            // otherwise
            else
            {
                // we cant wipe when the server is running
                Log.Error($"Server {server.ID} - (rust) - Tried to Wipe Map: Failed as the server is running");
                return false;
            }
        }
    }
}
