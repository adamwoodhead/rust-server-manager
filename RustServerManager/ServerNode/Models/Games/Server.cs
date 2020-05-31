using ServerNode.Logging;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ServerNode.Models.Games
{
    internal class Server
    {
        internal Server(int id, SteamApp app)
        {
            ID = id;
            App = app;
            ExecutablePath = Path.Combine(WorkingDirectory, app.RelativeExecutablePath);
        }

        internal Server() { }

        internal SteamApp App { get; set; }

        internal int ID { get; set; }

        internal string WorkingDirectory { get => Path.Combine(Program.GameServersDirectory, ID.ToString()); }

        internal string ExecutablePath { get; }

        internal bool IsInstalled { get; private set; } = true;

        internal string CommandLine { get; set; }

        internal bool IsRunning { get => (GameProcess != null) ? (bool)!GameProcess?.HasExited : false; }

        internal bool ShouldRun { get; set; }

        internal string Status { get; set; }

        internal Process GameProcess { get; set; }

        internal async Task PreInstall()
        {
            await Task.Run(() => {
                Log.Verbose("Creating Gameserver Directory");
                Directory.CreateDirectory(WorkingDirectory);
            });
        }

        internal async Task<bool> Update()
        {
            return await Install(true);
        }

        internal async Task<bool> Install(bool updating = false)
        {
            if (!updating)
            {
                Log.Informational($"Server {ID} Installing");
                await PreInstall();
            }
            else
            {
                Log.Informational($"Server {ID} Updating");
            }

            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                steam.Finished += delegate { Log.Verbose($"Server {ID} Install: Finished {(steam.AppInstallationSuccess ? "Successfully" : "Unexpectedly (check for errors)")}"); };
                steam.StateChanged += delegate { Log.Verbose($"Server {ID} Install: {steam.State}"); };
                steam.ProgressChanged += delegate {
                    if (steam.State == SteamCMDState.APP_DOWNLOADING)
                    {
                        if (steam.Progress != 0 && steam.EstimatedDownloadTimeLeft.TotalSeconds != 0)
                        {
                            Log.Informational($"Server {ID} Install: " + $"Progress: {steam.Progress:00.00}% | Estimated Time Left: {steam.EstimatedDownloadTimeLeft:hh\\:mm\\:ss} | Download Speed {Utility.ByteMeasurements.BytesToMB(steam.AverageDownloadSpeed):000.00}Mb/s");
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
                    IsInstalled = true;
                    Log.Success($"Server {ID} Successfully {(updating ? "Updated" : "Installed")}");
                }
                else
                {
                    IsInstalled = false;
                    Log.Warning($"Server {ID} Unsuccessfully {(updating ? "Updated" : "Installed")}");
                }
            }

            if (Directory.Exists(Path.Combine(WorkingDirectory, "steamapps")))
            {
                Directory.Delete(Path.Combine(WorkingDirectory, "steamapps"), true);
            }

            return IsInstalled;
        }

        internal async Task<bool> Reinstall()
        {
            Log.Informational($"Server {ID} Reinstalling");
            if (await Uninstall() && await Install())
            {
                Log.Success($"Server {ID} Successfully Reinstalled");
                return true;
            }
            else
            {
                Log.Warning($"Server {ID} Unsuccessfully Reinstalled");
                return false;
            }
        }

        internal async Task<bool> Uninstall()
        {
            Log.Informational($"Server {ID} Uninstalling");

            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                steam.Finished += delegate { Log.Verbose($"Server {ID} Uninstall: Finished"); };
                steam.StateChanged += delegate { Log.Verbose("Server {ID} Uninstall: " + steam.State); };

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

            Task waitTask = Task.Run(async() => {
                while (Directory.Exists(WorkingDirectory))
                {
                    await Task.Delay(2);
                }
            });

            // Directory.Delete marks a directory for deletion, and doesn't actually delete the directory
            // therefore still exists until the last handle is closed. 
            // Lets wait 2 seconds for that handle to close which realistically should be less than 20ms
            if (await Task.WhenAny(waitTask, Task.Delay(2000)) == waitTask)
            {
                Log.Success($"Server {ID} Successfully Uninstalled");
                IsInstalled = false;
                return true;
            }
            else
            {
                Log.Warning($"Server {ID} Unsuccessfully Uninstalled");
                IsInstalled = true;
                return false;
            }
        }

        internal async Task<bool> Start()
        {
            return await Task<bool>.Run(() =>
            {
                if (IsInstalled)
                {
                    ShouldRun = true;

                    GameProcess = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            WorkingDirectory = WorkingDirectory,
                            FileName = ExecutablePath,
                            Arguments = CommandLine,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        }
                    };

                    GameProcess.Start();

                    GameProcess.Exited += delegate
                    {
                        if (!ShouldRun)
                        {
                            GameProcess = null;
                        }
                    };

                    KeepAliveAsync(GameProcess);

                    Log.Verbose($"Launched Server {ID}");
                    return true;
                }

                Log.Verbose($"Failed To Launch Server {ID}");
                return false;
            });
        }

        internal virtual void Stop()
        {
            ShouldRun = false;

            Kill();
        }

        internal void Restart()
        {
            Stop();
            Start();
        }

        internal void Kill()
        {
            ShouldRun = false;

            GameProcess?.Kill();
            GameProcess?.WaitForExit();
        }

        public void KeepAliveAsync(Process process)
        {
            Task.Run(async () => {
                while (process != null)
                {
                    try
                    {
                        if (process.HasExited)
                        {
                            Kill();
                            Start();
                            process = null;
                            break;
                        }

                        await Task.Delay(5000);
                    }
                    catch (Exception) { }
                }
            });
        }
    }
}
