using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Steam
{
    internal class SteamApp
    {
        internal string Name { get; }
        internal string ShortName { get; }
        private string WindowsRelativeExecutablePath { get; }
        private string LinuxRelativeExecutablePath { get; }
        internal string RelativeExecutablePath { get => Utility.OperatingSystemHelper.IsWindows() ? WindowsRelativeExecutablePath : LinuxRelativeExecutablePath; }
        internal int SteamID { get; }
        public string[] DefaultCommandLine { get; }
        public string[] DeterminesInput { get; }

        internal SteamApp(string name, string shortName, string relativeWindowsExecutablePath, string relativeLinuxExecutablePath, int steamID, string[] defaultCommandLine, string[] defaultInput)
        {
            Name = name;
            ShortName = shortName;
            WindowsRelativeExecutablePath = relativeWindowsExecutablePath;
            LinuxRelativeExecutablePath = relativeLinuxExecutablePath;
            SteamID = steamID;
            DefaultCommandLine = defaultCommandLine;
            DeterminesInput = defaultInput;
        }
    }
}
