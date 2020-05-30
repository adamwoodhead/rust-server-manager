using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Steam
{
    internal class SteamApp
    {
        internal string Name { get; }
        internal string ShortName { get; }
        internal string RelativeExecutablePath { get; }
        internal int SteamID { get; }

        internal SteamApp(string name, string shortName, string relativeExecutablePath, int steamID)
        {
            Name = name;
            ShortName = shortName;
            RelativeExecutablePath = relativeExecutablePath;
            SteamID = steamID;
        }
    }
}
