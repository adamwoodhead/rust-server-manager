using ServerNode.Models.Games;
using ServerNode.Models.Steam;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Utility
{
    public static class PreAPIHelper
    {
        #region servers
        private static List<Server> Servers { get; set; } = new List<Server>();

        internal static Server CreateServer(SteamApp app)
        {
            int nextID = Servers.Count;
            Server server = new Server(nextID, app);
            Servers.Add(server);
            return server;
        }
        #endregion

        #region apps
        internal static Dictionary<string, SteamApp> Apps { get; } = new Dictionary<string, SteamApp>();

        internal static SteamApp CreateApp(string name, string shortName, string relativeExecutablePath, int steamID)
        {
            SteamApp app = new SteamApp(name, shortName, relativeExecutablePath, steamID);
            Apps.Add(shortName, app);
            return app;
        }
        #endregion
    }
}
