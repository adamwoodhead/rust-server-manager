using ServerNode.Logging;
using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Models.Servers
{
    internal class Server
    {
        private TaskCompletionSource<object?> keepAliveWaiting;

        internal Server(int id, SteamApp app)
        {
            ID = id;
            App = app;
            ExecutablePath = Path.Combine(WorkingDirectory, app.RelativeExecutablePath);
        }

        internal Server() { }

        /// <summary>
        /// The SteamApp of the Server
        /// </summary>
        internal SteamApp App { get; set; }

        /// <summary>
        /// Servers ID
        /// </summary>
        internal int ID { get; set; }

        /// <summary>
        /// Servers Working Directory
        /// </summary>
        internal string WorkingDirectory { get => Path.Combine(Program.GameServersDirectory, ID.ToString()); }

        /// <summary>
        /// Servers Executable File Path
        /// </summary>
        internal string ExecutablePath { get; }

        /// <summary>
        /// Checks only if the apps executable exists
        /// </summary>
        internal bool IsInstalled => File.Exists(ExecutablePath);

        /// <summary>
        /// Servers Commandline
        /// </summary>
        internal string[] CommandLine { get; set; }

        /// <summary>
        /// Whether the servers process is currently active, and not exited
        /// </summary>
        internal bool IsRunning { get => (GameProcess != null) && (bool)!GameProcess?.HasExited; }

        /// <summary>
        /// Whether the server should be running
        /// </summary>
        internal bool ShouldRun { get; set; }

        /// <summary>
        /// Whether to reboot the server, if it's process exits unexpectedly
        /// </summary>
        internal bool KeepAlive { get; set; } = true;

        /// <summary>
        /// Current status of the server
        /// </summary>
        internal string Status { get; set; }

        /// <summary>
        /// Process of the game app
        /// </summary>
        internal Process GameProcess { get; set; }

        /// <summary>
        /// Saved instance of the gameprocess pid
        /// </summary>
        internal int? PID { get; private set; }

        /// <summary>
        /// Pre-Installation Tasks, such as gameserver directory creation
        /// </summary>
        /// <returns></returns>
        private async Task PreInstallAsync()
        {
            await Task.Run(() => {
                Log.Verbose($"Creating Server {ID} Directory");
                // create the gameservers working directory
                Directory.CreateDirectory(WorkingDirectory);
            });
        }

        /// <summary>
        /// Updates the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> UpdateAsync()
        {
            return await InstallAsync(true);
        }

        /// <summary>
        /// Installs the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> InstallAsync(bool updating = false)
        {
            // if we're installing (not updating)
            if (!updating)
            {
                Log.Informational($"Server {ID} Installing");
                // perform the pre install procedures
                await PreInstallAsync();
            }
            else
            {
                Log.Informational($"Server {ID} Updating");
            }

            // instantiate a new steamcmd terminal
            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                // when its finished, inform
                steam.Finished += delegate { Log.Verbose($"Server {ID} Install: Finished {(steam.AppInstallationSuccess ? "Successfully" : "Unexpectedly (check for errors)")}"); };
                
                // when the state changes, inform
                steam.StateChanged += delegate { Log.Verbose($"Server {ID} Install: {steam.State}"); };
                
                // when the progress changes, inform
                steam.ProgressChanged += delegate {
                    // if the state is currently downloading
                    if (steam.State == SteamCMDState.APP_DOWNLOADING)
                    {
                        // and the progress isn't zero, and the time left isnt zero
                        if (steam.Progress != 0 && steam.EstimatedDownloadTimeLeft.TotalSeconds != 0)
                        {
                            // then provide install progress, timeleft, and speed
                            Log.Informational($"Server {ID} Install: " + $"Progress: {steam.Progress:00.00}% | Estimated Time Left: {steam.EstimatedDownloadTimeLeft:hh\\:mm\\:ss} | Download Speed {Utility.ByteMeasurements.BytesToMB(steam.AverageDownloadSpeed):000.00}Mb/s");
                        }
                    }
                };

                // if we login to steamcmd anonymously
                if (await steam.LoginAnonymously())
                {
                    // force the steamcmd working directory
                    await steam.ForceInstallDirectory(WorkingDirectory);

                    // install the game app
                    await steam.AppUpdate(App.SteamID, true);

                    // shutdown the steamcmd terminal
                    await steam.Shutdown();
                }

                // if we successfully installed the app
                if (steam.AppInstallationSuccess)
                {
                    // inform the user of success
                    Log.Success($"Server {ID} Successfully {(updating ? "Updated" : "Installed")}");
                }
                else
                {
                    // inform the user of failure
                    Log.Warning($"Server {ID} Unsuccessfully {(updating ? "Updated" : "Installed")}");
                }
            }

            // if the steamapps folder still exists after using steamcmd
            if (Directory.Exists(Path.Combine(WorkingDirectory, "steamapps")))
            {
                // then delete it
                Directory.Delete(Path.Combine(WorkingDirectory, "steamapps"), true);
            }

            return IsInstalled;
        }

        /// <summary>
        /// Reinstalls the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> ReinstallAsync()
        {
            Log.Informational($"Server {ID} Reinstalling");
            
            // uninstall the app
            await UninstallAsync();

            // if we install the app successfully
            if (await InstallAsync())
            {
                // return true
                Log.Success($"Server {ID} Successfully Reinstalled");
                return true;
            }
            // otherwise
            else
            {
                // return false
                Log.Warning($"Server {ID} Unsuccessfully Reinstalled");
                return false;
            }
        }

        /// <summary>
        /// Uninstalls the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> UninstallAsync()
        {
            Log.Informational($"Server {ID} Uninstalling");

            // instantiate a new steamcmd terminal
            using (SteamCMD steam = (SteamCMD)await Terminal.Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
            {
                steam.Finished += delegate { Log.Verbose($"Server {ID} Uninstall: Finished"); };
                steam.StateChanged += delegate { Log.Verbose($"Server {ID} Uninstall: " + steam.State); };

                // if we login to steamcmd
                if (await steam.LoginAnonymously())
                {
                    // force steamcmd to use servers working directory
                    await steam.ForceInstallDirectory($"{WorkingDirectory}");

                    // safely uninstall the app
                    await steam.AppUninstall(App.SteamID, true);

                    // shutdown steamcmd
                    await steam.Shutdown();
                }
            }

            // Directory.Delete marks a directory for deletion, and doesn't actually delete the directory
            // therefore still exists until the last handle is closed. 
            // Lets wait 2 seconds for that handle to close which realistically should be less than 20ms
            if (DirectoryExtensions.DeleteOrTimeout(WorkingDirectory, 2000))
            {
                Log.Success($"Server {ID} Successfully Uninstalled");
                // We just wanted to ensure that it's entirely empty
                // It's better this way than enumerating each file,
                // as we offload that to the native os
                Directory.CreateDirectory(WorkingDirectory);
                return true;
            }
            else
            {
                Log.Warning($"Server {ID} Unsuccessfully Uninstalled");
                return false;
            }
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> StartAsync()
        {
            return await Task.Run(() => {
                return Start();
            });
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <exception cref="ArgumentException"/>
        /// <returns></returns>
        internal bool Start()
        {
            // If the server is already running, we dont want to start it again, but we have the result we want
            if (IsRunning)
            {
                Log.Warning($"Starting Server {ID} Failed - already running, did you mean to restart?");
                return true;
            }
            // if the server is installed
            else if (IsInstalled)
            {
                Log.Informational($"Starting Server {ID} ({App.Name})");

                // any externals should now know that this server should be running
                ShouldRun = true;

                string shell;
                string shellScript;
                string wrappedCommandline;

                // if os is windows, we want a powershell shell
                if (Utility.OperatingSystemHelper.IsWindows())
                {
                    // run the application externally through shell and output the applications process id
                    wrappedCommandline = string.Join(',', CommandLine.Select(x => $"'{x}'"));
                    shell = "powershell";
                    shellScript = @"/c $server" + ID + @" = Start-Process -FilePath '" + ExecutablePath + @"' -ArgumentList " + wrappedCommandline + @" -WindowStyle Minimized -PassThru; echo $server" + ID + @".ID;";
                }
                // if os is linux, we want an sh shell
                else if (Utility.OperatingSystemHelper.IsLinux())
                {
                    // wipes any killed screens, kills any screens matching our server id, run the application externally through shell and output the applications process id, echo the new screen id
                    wrappedCommandline = string.Join(' ', CommandLine.Select(x => $"{x.Replace("\"", "\\\\\\\"")}"));
                    shell = "sh";
                    shellScript = @"-c ""screen -wipe; for pid in $(screen -ls | awk '/\.Server" + ID + @"\t/ { print strtonum($1)}'); do kill $pid; done; screen -wipe; screen -S Server" + ID + @" -dm sh -c \$\""sh " + ExecutablePath + " " + wrappedCommandline + @"\""; screen -ls | awk '/\.Server" + ID + @"\t/ {print strtonum($1)}'""";
                }
                // cant determine a shell to utilize
                else
                {
                    throw new ApplicationException("Couldn't find suitable shell to start gameserver.");
                }

                Log.Debug(shellScript);

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
                        output.Add(e.Data); Log.Debug($"{shell} Out: \"{e.Data}\"");
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

                // asyncronously read error and output
                starter.BeginOutputReadLine();
                starter.BeginErrorReadLine();

                Log.Verbose($"Waiting for {shell} responses");
                starter.WaitForExit();

                //output.ForEach(x => Log.Verbose(x));

                // the id should be the last output reports from the shell
                string returnedID = output.Last();

                // TODO Start here - CSS server process id is not captured on LINUX

                // if the returned id isn't null, and only contains digits..
                if (!string.IsNullOrEmpty(returnedID) && returnedID.All(c => c >= '0' && c <= '9'))
                {
                    Log.Verbose($"Process Captured Successfully - PID {returnedID}");
                    // capture our game app process
                    GameProcess = Process.GetProcessById(Convert.ToInt32(returnedID));
                    // get the pid
                    PID = GameProcess.Id;
                    // wait for the handle to become valid
                    while (GameProcess.SafeHandle.IsInvalid)
                    {
                        Log.Warning("Invalid Handle!");
                        Thread.Sleep(10);
                    }

                    // We run the keep alive task regardless of the KeepAlive property
                    // This way, keep alive can be turned on and off whilst the server is running
                    // without requiring a restart.
                    // The actual keep alive task will only take effect on a server with keepalive = true;
                    Task.Run(async () => {
                        int id = GameProcess.Id;
                        TaskCompletionSource<object?> localTaskSource = (keepAliveWaiting = new TaskCompletionSource<object?>());

                        // We can safely assume that if the process has died within 10 seconds
                        // it's either been stopped manually, or the server has crashed.
                        // lets not keep that alive...
                        Log.Verbose($"Keep Alive waiting ..");

                        await Task.WhenAny(Task.Delay(10000), localTaskSource.Task);

                        Process proc;

                        try
                        {
                            proc = Process.GetProcessById(id);
                        }
                        catch (ArgumentException)
                        {
                            // This exception throws if the process is not found by id.
                            // Some how only throws on linux so far.
                            proc = null;
                        }

                        // The server has been stopped by command
                        if (localTaskSource.Task.IsCompleted)
                        {
                            Log.Warning($"Keep Alive for Server {ID} Cancelled");
                        }
                        // The server is active after our timeout
                        else if (proc != null && !proc.HasExited)
                        {
                            Log.Verbose($"Started Keep Alive for Server {ID}");
                            BeginKeepAliveAsync(GameProcess.Id);
                        }
                        // The server isn't active anymore
                        else
                        {
                            ShouldRun = false;
                            Log.Error($"Keep Alive for Server {ID} failed - typically due to a server crash. (check your game servers log file(s))");
                        }
                    });
                }
                else
                // something went wrong with the shell script, and is most likely something to do
                // with the provided games commandline formatting
                {
                    Log.Verbose($"Process Not Captured");
                    Log.Debug(output);
                    throw new ArgumentException($"Error Starting New Game Server in {shell} with commandline:{Environment.NewLine}{wrappedCommandline}");
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

        /// <summary>
        /// Stops the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> StopAsync()
        {
            return await Task<bool>.Run(() => {
                return Stop();
            });
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        /// <returns></returns>
        internal bool Stop()
        {
            // Turn off ShouldRun, as we want it to stop!
            ShouldRun = false;

            if (keepAliveWaiting != null)
            {
                // If KeepAlive is in the startup phase, lets cancel it
                keepAliveWaiting.TrySetResult(null);
            }

            // only perform a kill if the server is running
            if (IsRunning)
            {
                Log.Informational($"Shutting Down Server {ID}");
                // kill the process and wait for it to exit
                if (KillAndWaitForExit())
                {
                    Log.Success($"Successfully shutdown server {ID}");
                    return true;
                }
                // couldn't kill, we shouldn't really ever get here
                else
                {
                    Log.Warning($"Could not shutdown server {ID}");
                    return false;
                }
            }
            else
            {
                // the server is already not running, return true as we have the result we're wanting
                Log.Warning($"Tried Shutting Down Server {ID} - It's not running!");
                return true;
            }
        }

        /// <summary>
        /// Restart the server
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> RestartAsync()
        {
            await StopAsync();
            return await StartAsync();
        }

        /// <summary>
        /// Restart the server
        /// </summary>
        /// <returns></returns>
        internal bool Restart()
        {
            Stop();
            return Start();
        }

        /// <summary>
        /// Kills the process, and waits for the process to exit.
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> KillAndWaitForExitAsync()
        {
            return await Task<bool>.Run(() => {
                return KillAndWaitForExit();
            });
        }

        /// <summary>
        /// Kills the process, and waits for the process to exit.
        /// </summary>
        /// <returns></returns>
        internal bool KillAndWaitForExit()
        {
            ShouldRun = false;

            GameProcess?.Kill();
            GameProcess?.WaitForExit();

            return GameProcess.HasExited;
        }

        internal async Task<bool> DeleteAsync()
        {
            return await Task<bool>.Run(() => {
                return Delete();
            });
        }

        internal bool Delete()
        {
            try
            {
                if (IsRunning)
                {
                    Log.Verbose("Stopping Server For Deletion.");
                    Stop();
                }

                if (IsInstalled)
                {
                    Log.Verbose("Uninstalling Server For Deletion.");
                    UninstallAsync().Wait();
                }

                if (Directory.Exists(WorkingDirectory))
                {
                    Log.Verbose("Deleting Server Directory For Deletion");
                    if (!DirectoryExtensions.DeleteOrTimeout(WorkingDirectory, 5000))
                    {
                        Log.Error($"Something went wrong whilst trying to delete server {ID} directory");
                        return false;
                    }
                }

                Log.Verbose("Removing Server from Servers List");
                PreAPIHelper.Servers.Remove(this);

                Log.Success($"Server {ID} Deleted - The ID {ID} is now free.");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Something went wrong whilst trying to delete server {ID}");
                Log.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Begin watching the server process id, if the server process exits and checks are passed, reboot the server
        /// </summary>
        /// <param name="id"></param>
        private async void BeginKeepAliveAsync(int id)
        {
            await Task.Run(async () => {
                // Throws an exception in linux..?
                try
                {
                    while (!Process.GetProcessById(id)?.HasExited ?? false)
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (ArgumentException)
                {
                    // This exception throws if the process is not found by id.
                    // Some how only throws on linux so far.
                    GameProcess = null;
                    PID = null;
                }

                // if keep alive is active, our gameserver should run, and the app should run..
                if (KeepAlive && ShouldRun && Program.ShouldRun)
                {
                    Log.Warning($"Server {ID} Unexpectedly Closed");
                    Log.Warning($"Server {ID} Keep Alive: Rebooting!");
                    GameProcess = null;
                    PID = null;
                    Start();
                }
            });
        }
    }
}
