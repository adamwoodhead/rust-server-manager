using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Steam
{
    internal class SteamApp
    {
        internal static List<SteamApp> Apps { get; } = new List<SteamApp>();

        internal string Name { get; }
        internal string ShortName { get; }
        internal string RelativeExecutablePath { get; }
        internal int SteamID { get; }

        private SteamApp(string name, string shortName, string relativeExecutablePath, int steamID)
        {
            Name = name;
            ShortName = shortName;
            RelativeExecutablePath = relativeExecutablePath;
            SteamID = steamID;
        }

        internal static void Create(string name, string shortName, string relativeExecutablePath, int steamID)
        {
            Apps.Add(new SteamApp(name, shortName, relativeExecutablePath, steamID));
        }
    }
}
