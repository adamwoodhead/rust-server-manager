using ServerNode.Models.Servers;
using ServerNode.Models.Steam;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Utility
{
    public static class PreAPIHelper
    {
        #region servers
        internal static List<Server> Servers { get; set; } = new List<Server>();

        internal static Server CreateServer(SteamApp app)
        {
            int nextID = Servers.Count;
            Server server = new Server(nextID, app)
            {
                CommandLine = app.DefaultCommandLine
            };
            Servers.Add(server);
            return server;
        }
        #endregion

        #region apps
        internal static Dictionary<string, SteamApp> Apps { get; } = new Dictionary<string, SteamApp>();

        internal static SteamApp CreateApp(string name, string shortName, string relativeWindowsExecutablePath, string relativeLinuxExecutablePath, int steamID, string[] defaultCommandLine)
        {
            SteamApp app = new SteamApp(name, shortName, relativeWindowsExecutablePath, relativeLinuxExecutablePath, steamID, defaultCommandLine);
            Apps.Add(shortName, app);
            return app;
        }
        #endregion
    }
}
