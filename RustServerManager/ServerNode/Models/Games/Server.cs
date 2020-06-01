using ServerNode.Logging;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

        // TODO Remove true, for testing purposes only.
        internal bool IsInstalled { get; private set; } = true;

        internal string[] CommandLine { get; set; }

        internal bool IsRunning { get => (GameProcess != null) ? (bool)!GameProcess?.HasExited : false; }

        internal bool ShouldRun { get; set; }

        internal string Status { get; set; }

        internal Process GameProcess { get; set; }

        private async Task PreInstallAsync()
        {
            await Task.Run(() => {
                Log.Verbose($"Creating Server {ID} Directory");
                Directory.CreateDirectory(WorkingDirectory);
            });
        }

        internal async Task<bool> UpdateAsync()
        {
            return await InstallAsync(true);
        }

        internal async Task<bool> InstallAsync(bool updating = false)
        {
            if (!updating)
            {
                Log.Informational($"Server {ID} Installing");
                await PreInstallAsync();
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

                    await steam.Shutdown();
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

        internal async Task<bool> ReinstallAsync()
        {
            Log.Informational($"Server {ID} Reinstalling");
            if (await UninstallAsync() && await InstallAsync())
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

        internal async Task<bool> UninstallAsync()
        {
            Log.Informational($"Server {ID} Uninstalling");

            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                steam.Finished += delegate { Log.Verbose($"Server {ID} Uninstall: Finished"); };
                steam.StateChanged += delegate { Log.Verbose($"Server {ID} Uninstall: " + steam.State); };

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

        internal async Task<bool> StartAsync()
        {
            return await Task.Run(() => {
                return Start();
            });
        }

        internal bool Start()
        {
            if (IsInstalled)
            {
                Log.Informational($"Starting Server {ID} ({App.Name})");

                ShouldRun = true;

                string shell;
                string shellScript;

                if (Utility.OperatingSystemHelper.IsWindows())
                {
                    string wrappedCommandline = string.Join(',', CommandLine.Select(x => $"'{x}'"));
                    shell = "powershell";
                    shellScript = @"/c $server" + ID + @" = Start-Process -FilePath '" + ExecutablePath + @"' -ArgumentList " + wrappedCommandline + @" -PassThru; echo $server" + ID + @".ID;";
                }
                else if (Utility.OperatingSystemHelper.IsLinux())
                {
                    string wrappedCommandline = string.Join(' ', CommandLine.Select(x => $"{x.Replace("\"", "\\\\\\\"")}"));
                    shell = "sh";
                    shellScript = @"-c ""screen -wipe; for pid in $(screen -ls | awk '/\.Server" + ID + @"\t/ { print strtonum($1)}'); do kill $pid; done; screen -wipe; screen -S Server" + ID + @" -dm sh -c \$\""sh " + ExecutablePath + " " + wrappedCommandline + @"\""; screen -ls | awk '/\.Server" + ID + @"\t/ {print strtonum($1)}'""";
                }
                else
                {
                    throw new ApplicationException("Couldn't find suitable shell to start gameserver.");
                }

                //Log.Verbose(shellScript);

                Process starter = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = WorkingDirectory,
                        FileName = shell,
                        Arguments = shellScript,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };

                List<string> output = new List<string>();
                List<string> errors = new List<string>();
                starter.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.Add(e.Data); //Log.Verbose($"{shell} Out: \"{e.Data}\"");
                    }
                };

                starter.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errors.Add(e.Data); Log.Verbose($"{shell} Error: \"{e.Data}\"");
                    }
                };

                starter.Start();

                starter.BeginOutputReadLine();
                starter.BeginErrorReadLine();

                Log.Verbose($"Waiting for {shell} responses");
                starter.WaitForExit();

                //output.ForEach(x => Log.Verbose(x));

                string returnedID = output.Last();

                if (!string.IsNullOrEmpty(returnedID) && returnedID.All(c => c >= '0' && c <= '9'))
                {
                    Log.Verbose($"Process Captured Successfully - PID {returnedID}");
                    GameProcess = Process.GetProcessById(Convert.ToInt32(returnedID));
                    while (GameProcess.SafeHandle.IsInvalid)
                    {
                        Log.Verbose("Invalid Handle!");
                        Thread.Sleep(10);
                    }

                    // Keep Alive
                    GameProcess.Exited += GameProcess_Exited;
                }
                else
                {
                    Log.Verbose($"Process Not Captured");
                    throw new ApplicationException($"Error Starting New Game Server in {shell}:{Environment.NewLine}{returnedID}");
                }

                Log.Success($"Server {ID} Started ({App.Name})");
                return true;
            }
            else
            {
                Log.Verbose($"Failed To Launch Server {ID} ({App.Name}) - Not Installed");
                return false;
            }
        }

        private void GameProcess_Exited(object sender, EventArgs e)
        {
            Log.Warning($"Server {ID} has closed unexpectedly");

            if (ShouldRun)
            {
                Log.Warning($"Server {ID} Keep Alive Activated!");
                Start();
            }
        }

        internal async Task<bool> StopAsync()
        {
            return await Task<bool>.Run(() => {
                return Stop();
            });
        }

        internal bool Stop()
        {
            GameProcess.Exited -= GameProcess_Exited;
            ShouldRun = false;

            if (!GameProcess?.HasExited ?? false)
            {
                Log.Informational($"Shutting Down Server {ID}");
                if (Kill())
                {
                    Log.Success($"Successfully shutdown server {ID}");
                    return true;
                }
                else
                {
                    Log.Warning($"Could not shutdown server {ID}");
                    return false;
                }
            }
            else
            {
                Log.Warning($"Tried Shutting Down Server {ID} - It's not running!");
                return true;
            }
        }

        internal async Task<bool> RestartAsync()
        {
            return await Task<bool>.Run(() => {
                return Restart();
            });
        }

        internal bool Restart()
        {
            Stop();
            return Start();
        }

        internal async Task<bool> KillAsync()
        {
            return await Task<bool>.Run(() => {
                return Kill();
            });
        }

        internal bool Kill()
        {
            ShouldRun = false;

            GameProcess?.Kill();
            GameProcess?.WaitForExit();

            return GameProcess.HasExited;
        }
    }
}
