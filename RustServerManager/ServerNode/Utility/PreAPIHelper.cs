using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Models.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerNode.Utility
{
    internal static class PreAPIHelper
    {
        #region servers
        public static List<Server> Servers { get; set; } = new List<Server>();

        public static Server CreateServer(SteamApp app, int? forcedID = null)
        {
            int nextID = 0;

            if (forcedID == null)
            {
                while (Servers.Exists(x => x.ID == nextID))
                {
                    nextID++;
                }
            }

            Server server = new Server(forcedID ?? nextID, app)
            {
                CommandLine = app.DefaultCommandLine
            };
            Servers.Add(server);
            Directory.CreateDirectory(server.WorkingDirectory);
            Log.Success($"Server Created - ID: {server.ID} | App: {app.Name}");
            return server;
        }
        #endregion

        #region apps
        public static Dictionary<string, SteamApp> Apps { get; } = new Dictionary<string, SteamApp>();

        public static SteamApp CreateApp(string name, string shortName, int port, int slots, string relativeWindowsExecutablePath, string relativeLinuxExecutablePath, int steamID, bool requirePurchase, string[] defaultCommandLine, Variable[] customVariables = null)
        {
            SteamApp app = new SteamApp(name, shortName, port, slots, relativeWindowsExecutablePath, relativeLinuxExecutablePath, steamID, requirePurchase, defaultCommandLine, customVariables);
            Apps.Add(shortName, app);
            return app;
        }
        #endregion
    }
}
