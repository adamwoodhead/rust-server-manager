using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Steam
{
    internal class SteamApp
    {
        /// <summary>
        /// App Name
        /// </summary>
        internal string Name { get; }
        
        /// <summary>
        /// App Short Name
        /// </summary>
        internal string ShortName { get; }

        /// <summary>
        /// Windows Relative Executable Path
        /// </summary>
        private string WindowsRelativeExecutablePath { get; }

        /// <summary>
        /// Linux Relative Executable Path
        /// </summary>
        private string LinuxRelativeExecutablePath { get; }

        /// <summary>
        /// Native Executable Path
        /// </summary>
        internal string RelativeExecutablePath { get => Utility.OperatingSystemHelper.IsWindows() ? WindowsRelativeExecutablePath : LinuxRelativeExecutablePath; }
        
        /// <summary>
        /// Apps Steam DB ID
        /// </summary>
        internal int SteamID { get; }

        /// <summary>
        /// Default Commandline for Server
        /// </summary>
        public string[] DefaultCommandLine { get; }

        internal SteamApp(string name, string shortName, string relativeWindowsExecutablePath, string relativeLinuxExecutablePath, int steamID, string[] defaultCommandLine)
        {
            Name = name;
            ShortName = shortName;
            WindowsRelativeExecutablePath = relativeWindowsExecutablePath;
            LinuxRelativeExecutablePath = relativeLinuxExecutablePath;
            SteamID = steamID;
            DefaultCommandLine = defaultCommandLine;
        }
    }
}
