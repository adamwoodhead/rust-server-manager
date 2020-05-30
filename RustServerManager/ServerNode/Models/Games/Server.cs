using ServerNode.Logging;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServerNode.Models.Games
{
    internal abstract class Server
    {
        internal SteamApp App { get; set; }

        internal int ID { get; set; }

        internal string WorkingDirectory { get => Path.Combine(Program.GameServersDirectory, ID.ToString()); }

        internal async Task PreInstall()
        {
            await Task.Run(() => {
                Log.Verbose("Creating Gameserver Directory");
                Directory.CreateDirectory(WorkingDirectory);
            });
        }

        internal async Task<bool> Install()
        {
            await PreInstall();

            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                steam.Finished += delegate { Log.Verbose($"SteamCMD: Finished {(steam.AppInstallationSuccess ? "Successfully" : "Unexpectedly (check for errors)")}"); };
                steam.StateChanged += delegate { Log.Verbose($"SteamCMD: {steam.State}"); };
                steam.ProgressChanged += delegate {
                    if (steam.State == SteamCMDState.APP_DOWNLOADING)
                    {
                        if (steam.Progress != 0 && steam.EstimatedDownloadTimeLeft.TotalSeconds != 0)
                        {
                            Log.Informational("SteamCMD: " + $"Progress: {steam.Progress:00.00}% | Estimated Time Left: {steam.EstimatedDownloadTimeLeft:hh\\:mm\\:ss} | Download Speed {Utility.ByteMeasurements.BytesToMB(steam.AverageDownloadSpeed):000.00}Mb/s");
                        }
                    }
                };

                if (await steam.LoginAnonymously())
                {
                    await steam.ForceInstallDirectory(WorkingDirectory);

                    await steam.AppUpdate(App.SteamID, true);
                }

                if (steam.AppInstallationSuccess)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal async Task<bool> Reinstall()
        {
            return await Uninstall() && await Install();
        }

        internal async Task<bool> Uninstall()
        {
            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                steam.Finished += delegate { Log.Verbose($"SteamCMD:  Finished"); };
                steam.StateChanged += delegate { Log.Verbose("SteamCMD: " + steam.State); };

                if (await steam.LoginAnonymously())
                {

                    await steam.ForceInstallDirectory($"{WorkingDirectory}");

                    await steam.AppUninstall(App.SteamID, true);

                    await steam.Shutdown();

                    if (Directory.Exists(WorkingDirectory))
                    {
                        Directory.Delete(WorkingDirectory, true);
                    }
                }
            }

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                return !Directory.Exists(WorkingDirectory);
            }
            else
            {
                return !Directory.Exists(WorkingDirectory);
            }
        }
    }
}
