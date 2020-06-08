using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Models.Terminal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Models.Steam
{
    public class SteamApp
    {
        /// <summary>
        /// App Name
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// App Short Name
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Default port for app
        /// </summary>
        public int Port { get; }

        public int DefaultSlots { get; }

        public Variable[] CustomVariables { get; }

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
        public string RelativeExecutablePath { get => Utility.OperatingSystemHelper.IsWindows() ? WindowsRelativeExecutablePath : LinuxRelativeExecutablePath; }
        
        /// <summary>
        /// Apps Steam DB ID
        /// </summary>
        public int SteamID { get; }

        public bool RequiresPurchase { get; }

        public string WorkingDirectory { get => Path.Combine(Program.AppsDirectory, ShortName); }

        public bool IsInstalled { get => File.Exists(Path.Combine(WorkingDirectory, RelativeExecutablePath)); }

        /// <summary>
        /// Default Commandline for Server
        /// </summary>
        public string[] DefaultCommandLine { get; }

        public SteamApp(string name, string shortName, int port, int defaultSlots, string relativeWindowsExecutablePath, string relativeLinuxExecutablePath, int steamID, bool requirePurchase, string[] defaultCommandLine, Variable[] customVariables)
        {
            Name = name;
            ShortName = shortName;
            Port = port;
            DefaultSlots = defaultSlots;
            WindowsRelativeExecutablePath = relativeWindowsExecutablePath;
            LinuxRelativeExecutablePath = relativeLinuxExecutablePath;
            SteamID = steamID;
            RequiresPurchase = requirePurchase;
            DefaultCommandLine = defaultCommandLine;
            CustomVariables = customVariables;
        }

        public async Task<bool> InstallAsync(bool updating = false)
        {
            Log.Informational($"Pre Installing {Name}");

            try
            {
                // instantiate a new steamcmd terminal
                using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
                {
                    // when its finished, inform
                    steam.Finished += delegate { Log.Verbose($"App {Name} Pre Install: Finished {(steam.AppInstallationSuccess ? "Successfully" : "Unexpectedly (check for errors)")}"); };

                    // when the state changes, inform
                    steam.StateChanged += delegate
                    {
                        Log.Verbose($"App {Name} Pre Install: {steam.State}");
                        if (steam.State == SteamCMDState.APP_INSTALL_ERROR
                        || steam.State == SteamCMDState.APP_INSTALL_ERROR_NO_DISK)
                        {
                            Log.Error($"App {Name} Pre Install Error: {steam.State}");
                        }
                    };

                    // when the progress changes, inform
                    steam.ProgressChanged += delegate {
                        // if the state is currently downloading
                        if (steam.State == SteamCMDState.APP_DOWNLOADING)
                        {
                            // and the progress isn't zero, and the time left isnt zero
                            if (steam.Progress != 0 && steam.EstimatedDownloadTimeLeft.TotalSeconds != 0)
                            {
                                // then provide install progress, timeleft, and speed
                                Log.Informational($"App {Name} Pre Install: " + $"Progress: {steam.Progress:00.00}% | Estimated Time Left: {steam.EstimatedDownloadTimeLeft:hh\\:mm\\:ss} | Download Speed {Utility.ByteMeasurements.BytesToMB(steam.AverageDownloadSpeed):000.00}Mb/s");
                            }
                        }
                        else if (steam.State == SteamCMDState.APP_PREALLOCATING)
                        {
                            // and the progress isn't zero, and the time left isnt zero
                            if (steam.Progress != 0)
                            {
                                // then provide install progress, timeleft, and speed
                                Log.Informational($"App {Name} Pre Install: " + $"Pre-Allocating Progress: {steam.Progress:00.00}%");
                            }
                        }
                        else if (steam.State == SteamCMDState.APP_VERIFYING)
                        {
                            // and the progress isn't zero, and the time left isnt zero
                            if (steam.Progress != 0)
                            {
                                // then provide install progress, timeleft, and speed
                                Log.Informational($"App {Name} Pre Install: " + $"Validating Progress: {steam.Progress:00.00}%");
                            }
                        }
                    };

                    // if we login to steamcmd anonymously
                    if (await steam.Login(this))
                    {
                        // force the steamcmd working directory
                        await steam.ForceInstallDirectory(WorkingDirectory);

                        // install the game app
                        await steam.AppUpdate(this.SteamID, true);

                        // shutdown the steamcmd terminal
                        await steam.Shutdown();
                    }

                    // if we successfully installed the app
                    if (steam.AppInstallationSuccess)
                    {
                        // inform the user of success
                        Log.Success($"App {Name} Successfully {(updating ? "Updated" : "Installed")}");
                    }
                    else
                    {
                        // inform the user of failure
                        Log.Warning($"App {Name} Unsuccessfully {(updating ? "Updated" : "Installed")}");
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw new ApplicationException("SteamCMD Failed to instantiate!", ex);
            }

            // if the steamapps folder still exists after using steamcmd
            if (Directory.Exists(Path.Combine(WorkingDirectory, "steamapps")))
            {
                // then delete it
                Directory.Delete(Path.Combine(WorkingDirectory, "steamapps"), true);
            }

            return IsInstalled;
        }
    }
}
