using Pty.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Models.Steam
{
    /// <summary>
    /// Disposable SteamCMD Object, preferrably called within a using statement.
    /// </summary>
    internal class SteamCMD
    {
        private CancellationToken CancellationToken = new CancellationTokenSource().Token;
        private UTF8Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private SteamCMDState _state = SteamCMDState.UNDEFINED;
        private double _progress = 0;
        private bool _hasFinished = false;

        /// <summary>
        /// Event Handler raised when SteamCMD finishes it's procedure.
        /// </summary>
        internal event EventHandler Finished;

        /// <summary>
        /// Event Handler raised when the current State is changed.
        /// </summary>
        internal event EventHandler StateChanged;

        /// <summary>
        /// Event Handler raised when the Progress of SteamCMD has changed (if applicable to current state and procedure).
        /// </summary>
        internal event EventHandler ProgressChanged;

        private IPtyConnection Terminal { get; set; }

        private TaskCompletionSource<object?> ReadyForInputTsk;
        private bool appInstallationSuccess = false;

        /// <summary>
        /// Get the download path for steamcmd
        /// </summary>
        private static string DownloadPath
        {
            get
            {
                if (Utility.OperatingSystemHelper.IsWindows()) // We're on Windows
                {
                    return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
                }
                else // We're not on Windows, so this should never be called!
                {
                    throw new ApplicationException("should only be called on windows");
                }
            }
        }

        /// <summary>
        /// The executable path for SteamCMD, variable defined by current operating system
        /// </summary>
        private static string WinExecutablePath
        {
            get
            {
                // get the appdata folder
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                // create a folder for the application
                string steamCmdFolder = Directory.CreateDirectory(Path.Combine(appData, "GameServerManagerUtilities")).FullName;
                // executable file for steamcmd
                string steamCmdExe = Path.Combine(steamCmdFolder, "steamcmd.exe");
                // TODO Steam CMD Executable Path (Windows)
                return steamCmdExe;
            }
        }

        private static string LinExecutablePath
        {
            get
            {
                // pre-determined installation location for linux steamcmd (only checked Ubuntu 16.04)
                return @"./.steam/steamcmd/steamcmd.sh";
            }
        }

        /// <summary>
        /// Check if the SteamCMD Executable exists
        /// </summary>
        /// <returns></returns>
        internal static bool ExecutableExists()
        {
            // Check if the variable file exists, depending in windows/linux
            if (File.Exists(WinExecutablePath))
            {
                // File Found
                return true;
            }
            else
            {
                // File not found
                return false;
            }
        }

        /// <summary>
        /// Boolean representation of whether steamcmd successfully installed the app
        /// </summary>
        internal bool AppInstallationSuccess
        { get => appInstallationSuccess; private set => appInstallationSuccess = value; }

        /// <summary>
        /// Current SteamCMD Procedure State
        /// </summary>
        internal SteamCMDState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged(_state);
                }
            }
        }

        /// <summary>
        /// Current SteamCMD Procedure Progress, applicable depending on current state
        /// </summary>
        internal double Progress
        {
            get => _progress;
            private set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnProgressChanged(_progress);
                }
            }
        }

        /// <summary>
        /// Whether the SteamCMD Procedure Finished
        /// </summary>
        internal bool HasFinished
        {
            get => _hasFinished;
            private set
            {
                if (value == true)
                {
                    _hasFinished = value;
                    OnFinished();
                }
            }
        }

        /// <summary>
        /// Event Trigger for a state change
        /// </summary>
        /// <param name="s"></param>
        protected virtual void OnStateChanged(SteamCMDState s)
        {
            StateChangedEventArgs e = new StateChangedEventArgs(s);
            StateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Event Trigger for a progress change
        /// </summary>
        /// <param name="p"></param>
        protected virtual void OnProgressChanged(double p)
        {
            ProgressChangedEventArgs e = new ProgressChangedEventArgs(p);
            ProgressChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Event Trigger for when the SteamCMD procedure finishes
        /// </summary>
        protected virtual void OnFinished()
        {
            Finished?.Invoke(this, null);
        }

        /// <summary>
        /// Download and extract the steamcmd executable for windows
        /// </summary>
        private void DownloadSteamCMD()
        {
            // this should only be used on windows, as it's a prerequisite for linux users to install manually before running the application
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // get the appdata folder
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                // get the application folder
                string steamCmdFolder = Directory.CreateDirectory(Path.Combine(appData, "GameServerManagerUtilities")).FullName;
                // set the desired filepath for steamcmd.zip
                string steamCmdZip = Path.Combine(steamCmdFolder, "steamcmd.zip");

                // create a disposable webclient for downloading steamcmd
                using (WebClient webClient = new WebClient())
                {
                    // download steamcmd to the application folder
                    webClient.DownloadFile(DownloadPath, steamCmdZip);
                }

                // extract steamcmd.zip, providing steamcmd.exe
                ZipFile.ExtractToDirectory(steamCmdZip, steamCmdFolder);

                // clean up and remove the zip
                File.Delete(steamCmdZip);
            }
            else
            {
                // this has been called on an operating system other than windows, that should never happen
                throw new ApplicationException("should only be called on windows");
            }
        }

        internal void InstallAnonymousApp(string installDirectory, int appID, bool validate)
        {
            // argument string for app validation
            string validateString = (validate ? "validate " : "");
            // create steamcmd arguments
            string arguments = $"+login anonymous +force_install_dir {installDirectory} +app_update {appID} {validateString}+quit";

            // begin the installation procedure
            InstallApp(arguments, installDirectory);
        }

        internal void InstallApp(string username, string password, string installDirectory, int appID, bool validate)
        {
            // argument string for app validation
            string validateString = (validate ? "validate " : "");
            // create steamcmd arguments
            string arguments = $"+login {username} {password} +force_install_dir {installDirectory} +app_update {appID} {validateString}+quit";

            // begin the installation procedure
            InstallApp(arguments, installDirectory);
        }

        /// <summary>
        /// Install a steamdb app, and monitor the stages and progress (awaitable)
        /// </summary>
        /// <param name="installDirectory"></param>
        /// <param name="appID"></param>
        /// <param name="validate"></param>
        private async void InstallApp(string arguments, string workingDir)
        {
            // check if we have the steamcmd executable available
            if (!ExecutableExists())
            {
                // check the current operating system is windows
                if (Utility.OperatingSystemHelper.IsWindows())
                {
                    // download and extract steacmd into the appdata utilities folder
                    DownloadSteamCMD();
                }
                else
                {
                    // die, we shouldn't be here!
                    throw new ApplicationException("SteamCMD Not Installed.");
                }
            }

            if (!Directory.Exists(workingDir))
            {
                Console.WriteLine("Creating gameserver directory");
            }
            else
            {
                Console.WriteLine("Gameserver directory already exists");
            }

            Console.WriteLine(arguments);

            string app = Utility.OperatingSystemHelper.IsWindows() ? Path.Combine(Environment.SystemDirectory, "cmd.exe") : "sh";

            PtyOptions ptyOptions = new PtyOptions()
            {
                Name = "SteamCMD",
                App = app,
                Cols = 300,
                Rows = 1,
                Cwd = Environment.CurrentDirectory
            };

            IPtyConnection terminal = await PtyProvider.SpawnAsync(ptyOptions, CancellationToken);

            TaskCompletionSource<uint> processExitedTcs = new TaskCompletionSource<uint>();
            terminal.ProcessExited += (sender, e) => { processExitedTcs.TrySetResult((uint)terminal.ExitCode); HasFinished = true; };

            string GetTerminalExitCode() => processExitedTcs.Task.IsCompleted ? $". Terminal process has exited with exit code {processExitedTcs.Task.GetAwaiter().GetResult()}." : string.Empty;

            int i = 0;
            using (StreamReader reader = new StreamReader(terminal.ReaderStream))
            {
                string result = await reader.ReadLineAsync();
                Console.WriteLine($"{++i}: {result}");
            }
        }

        internal async Task Test()
        {
            // check if we have the steamcmd executable available
            if (!ExecutableExists())
            {
                // check the current operating system is windows
                if (Utility.OperatingSystemHelper.IsWindows())
                {
                    // download and extract steacmd into the appdata utilities folder
                    DownloadSteamCMD();
                }
                else
                {
                    // die, we shouldn't be here!
                    throw new ApplicationException("SteamCMD Not Installed.");
                }
            }

            try
            {
                await ConnectToTerminal();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not start the terminal", ex);
            }

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                await SendCommand(WinExecutablePath);
            }
            else
            {
                await SendCommand(LinExecutablePath);
            }

            await ReadyForInputTsk.Task;

            //await LoginUserPassword("", "");

            await LoginAnonymously();

            await ForceInstallDirectory($"force_install_dir \"{@"C:\test something\"}\"");

            await AppUpdate((int)SteamApps.COUNTER_STRIKE_SOURCE_SERVER, true);

            await Shutdown();
        }

        internal async Task AppUpdate(int id, bool validate)
        {
            if (validate)
            {
                await SendCommand(@$"app_update {id} validate");
            }
            else
            {
                await SendCommand(@$"app_update {id}");
            }

            await ReadyForInputTsk.Task;
        }

        internal async Task ForceInstallDirectory(string path)
        {
            await SendCommand(@$"{path}");

            await ReadyForInputTsk.Task;
        }

        internal async Task<bool> LoginUserPassword(string username, string password, string steamGuard = null)
        {
            State = SteamCMDState.LOGGING_IN;

            await SendCommand(@$"login {username} {password}");

            await ReadyForInputTsk.Task;

            if (State == SteamCMDState.LOGIN_REQUIRES_STEAMGUARD)
            {
                if (steamGuard != null)
                {
                    await SendCommand(@$"{steamGuard.ToUpper()}");

                    await ReadyForInputTsk.Task;
                }
                else
                {
                    // TODO Ask the user for steamguard, and remove the below line
                    await SendCommand(@$" ");
                }
            }

            return (State == SteamCMDState.LOGGED_IN);
        }

        /// <summary>
        /// Send commands to steamcmd to login anonymously (login anonymous)
        /// </summary>
        /// <returns></returns>
        internal async Task LoginAnonymously()
        {
            await SendCommand(@"login anonymous");

            await ReadyForInputTsk.Task;
        }

        /// <summary>
        /// Sends a command to steamcmd requesting a peaceful quit.
        /// Kills the process and pseudoterminal after x seconds if it does not exit peacefully.
        /// </summary>
        /// <returns></returns>
        private async Task Shutdown(int timeout = 10)
        {
            // steamcmd command to peacefully quit
            await SendCommand(@"quit");

            // Wait one second for steamcmd to shutdown peacefully
            await Task.Delay(1000);

            // Exit the terminal peacefully
            await SendCommand(@"exit");

            System.Timers.Timer aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            int seconds = timeout + 1;
            aTimer.Elapsed += delegate { Console.WriteLine($"Waiting for steam to close.. {--seconds}"); };
            aTimer.AutoReset = true;
            aTimer.Enabled = true;

            // If the terminal hasn't exited, we should wait a short amount of time and then kill it if it's still alive
            if (Terminal.WaitForExit(seconds * 1000))
            {
                // cancel the timer
                aTimer.Enabled = false;
            }
            else
            {
                // cancel the timer
                aTimer.Enabled = false;
                // Kill the terminal process containing steamcmd
                Terminal?.Kill();
            }
        }

        private async Task ConnectToTerminal()
        {
            string app = Utility.OperatingSystemHelper.IsWindows() ? Path.Combine(Environment.SystemDirectory, "cmd.exe") : "sh";
            var options = new PtyOptions
            {
                Name = "SteamCMD Terminal",
                // TODO this should be quite long, and cover anything that steamcmd can spit out in a single line + the current directory length
                Cols = 300,
                // we want it line by line, no more than that
                Rows = 1,
                Cwd = Environment.CurrentDirectory,
                App = app
            };

            Terminal = await PtyProvider.SpawnAsync(options, this.CancellationToken);

            var processExitedTcs = new TaskCompletionSource<uint>();
            Terminal.ProcessExited += (sender, e) =>
            {
                processExitedTcs.TrySetResult((uint)Terminal.ExitCode);
                HasFinished = true;

                Terminal.Resize(40, 10);

                Terminal.Dispose();

                using (this.CancellationToken.Register(() => processExitedTcs.TrySetCanceled(this.CancellationToken)))
                {
                    uint exitCode = (uint)Terminal.ExitCode;
                }
            };

            string GetTerminalExitCode() =>
                processExitedTcs.Task.IsCompleted ? $". Terminal process has exited with exit code {processExitedTcs.Task.GetAwaiter().GetResult()}." : string.Empty;

            TaskCompletionSource<object> firstOutput = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            string output = string.Empty;
            Task<bool> checkTerminalOutputAsync = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                var ansiRegex = new Regex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))");

                while (!this.CancellationToken.IsCancellationRequested && !processExitedTcs.Task.IsCompleted)
                {
                    int count = await Terminal.ReaderStream.ReadAsync(buffer, 0, buffer.Length, this.CancellationToken);
                    if (count == 0)
                    {
                        Console.WriteLine("output has finished");
                        break;
                    }

                    firstOutput.TrySetResult(null);

                    output += Encoding.GetString(buffer, 0, count);
                    output = ansiRegex.Replace(output, string.Empty);
                    if (output.Contains("\n") || output.Contains("\r"))
                    {
                        output = output.Replace("\r", string.Empty).Replace("\n", string.Empty);

                        // Parse the output before setting input ready, we need to set states if applicable
                        SteamCMD_ParseOutput(output);

                        // Inform that steamcmd is awaiting input
                        if (output == "Steam>")
                        {
                            State = SteamCMDState.AWAITING_INPUT;
                            ReadyForInputTsk.SetResult(null);
                        }
                        else if (output == "password:")
                        {
                            State = SteamCMDState.LOGIN_REQUIRES_PASSWORD;
                            ReadyForInputTsk.SetResult(null);
                        }
                        else if (output == "Enter the current code from your Steam Guard Mobile Authenticator appTwo-factor code:"
                        || output == "Please check your email for the message from Steam, and enter the Steam Guard code from that message.You can also enter this code at any time using 'set_steam_guard_code' at the console.Steam Guard code:")
                        {
                            State = SteamCMDState.LOGIN_REQUIRES_STEAMGUARD;
                            ReadyForInputTsk.SetResult(null);
                        }

                        // Reset the output
                        output = string.Empty;
                    }
                }

                firstOutput.TrySetCanceled();
                return false;
            });

            try
            {
                await firstOutput.Task;
            }
            catch (OperationCanceledException exception)
            {
                throw new InvalidOperationException($"Could not get any output from terminal{GetTerminalExitCode()}", exception);
            }
        }

        private async Task SendCommand(string command)
        {
            ReadyForInputTsk = new TaskCompletionSource<object?>();
            byte[] commandBuffer = Encoding.GetBytes(command);
            await Terminal.WriterStream.WriteAsync(commandBuffer, 0, commandBuffer.Length, this.CancellationToken);
            await Terminal.WriterStream.WriteAsync(new byte[] { 0x0D }, 0, 1, this.CancellationToken);
            await Terminal.WriterStream.FlushAsync();
        }

        /// <summary>
        /// Output handler for SteamCMD Process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SteamCMD_ParseOutput(string data)
        {
            // ANSI code regex
            Regex ansiRegex = new Regex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))");
            //Regex ansiRegex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])");

            // remove those annoying ANSI codes
            data = ansiRegex.Replace(data, string.Empty);

            // remove whitespaces
            data = data.Trim();

            //Console.WriteLine($"PTY ({Terminal.Pid}): {data}");

            // if the string is now null or empty after trimming, we don't want to handle it
            if (!string.IsNullOrEmpty(data))
            {
                bool shouldCheckProgress = false;
                // ----- steamcmd self updating section
                // steamcmd is checking for available updates
                if (data.Contains("] Checking for available update")) { State = SteamCMDState.STEAMCMD_CHECKING_UPDATES; Progress = 0; }

                // steamcmd is downloading updates
                else if (data.Contains("] Downloading update")) { State = SteamCMDState.STEAMCMD_DOWNLOADING_UPDATES; shouldCheckProgress = true; }

                // steamcmd is downloading updates
                else if (data.Contains("] Downloading update")) { State = SteamCMDState.STEAMCMD_DOWNLOADING_UPDATES; shouldCheckProgress = true; }

                // steamcmd is extracting the new package
                else if (data.Contains("] Extracting package")) { State = SteamCMDState.STEAMCMD_EXTRACTING_PACKAGES; Progress = 0; }

                // steamcmd is installing
                else if (data.Contains("] Installing update")) { State = SteamCMDState.STEAMCMD_INSTALLING_UPDATE; Progress = 0; }

                // steamcmd is verifying its installation
                else if (data.Contains("] Verifying installation")) { State = SteamCMDState.STEAMCMD_VERIFYING; Progress = 0; }

                // steamcmd is now ready and loading
                else if (data.Contains("Loading Steam API...")) { State = SteamCMDState.STEAMCMD_LOADED; Progress = 0; }

                // ----- steamcmd login section
                // login failed with invalid password
                else if (data.Contains("FAILED login with result code Invalid Password")) { State = SteamCMDState.LOGIN_FAILED_BAD_PASS; Progress = 0; }

                // login failed due to too many invalid attempts
                else if (data.Contains("FAILED login with result code Rate Limit Exceeded")) { State = SteamCMDState.LOGIN_FAILED_RATE_LIMIT; Progress = 0; }

                // login failed, unknown reason thus far
                else if (data.Contains("FAILED login")) { State = SteamCMDState.LOGIN_FAILED_GENERIC; Progress = 0; }

                // login success
                else if (data.Contains("Logged in OK")) { State = SteamCMDState.LOGGED_IN; Progress = 0; }

                // ----- steamcmd app installation section
                // steamcmd is validating the app
                else if (data.Contains("Update state (0x5) validating")) { State = SteamCMDState.APP_VALIDATING; shouldCheckProgress = true; }

                // steamcmd validation failed completely so it's now preallocating the storage space for full installation
                else if (data.Contains("Update state (0x11) preallocating")) { State = SteamCMDState.APP_PREALLOCATING; shouldCheckProgress = true; }

                // steamcmd is now downloading the app
                else if (data.Contains("Update state (0x61) downloading")) { State = SteamCMDState.APP_DOWNLOADING; shouldCheckProgress = true; }

                // steamcmd is now validating the app
                else if (data.Contains("Update state (0x5) validating") && State == SteamCMDState.APP_DOWNLOADING) { State = SteamCMDState.APP_POST_DOWNLOAD_VALIDATING; shouldCheckProgress = true; }

                // steamcmd is now validating the app
                else if (data.Contains("Update state (0x5) verifying")) { State = SteamCMDState.APP_VERIFYING; shouldCheckProgress = true; }

                // steamcmd successfully install the app
                else if (data.Contains("Success! App") && data.Contains("fully installed")) { Progress = 100; State = SteamCMDState.APP_INSTALLED; AppInstallationSuccess = true; }

                // data has been flagged to contain progress data
                if (shouldCheckProgress)
                {
                    // app installation regex for progress
                    Match match = Regex.Match(data, @"(\d{1,2}\.\d{1,2})");
                    // steamcmd installation regex for progress
                    Match match2 = Regex.Match(data, @"(\[.{0,2}\d{1,3}\%\])");

                    // we got a match for app installation
                    if (match.Success)
                    {
                        // convert the matched string value to a double, and set the progress
                        Progress = Convert.ToDouble(match.Value);
                    }
                    // we got a match for steamcmd installation
                    else if (match2.Success)
                    {
                        // pull the digits only from the matched string
                        Match digitMatch = Regex.Match(match2.Value, @"(\d{1,3})");
                        // if we successfully pulled digits..
                        if (digitMatch.Success)
                        {
                            // convert the matched string value to a double, and set the progress
                            Progress = Convert.ToDouble(digitMatch.Value);
                        }
                    }
                    // we got no match at all for progress
                    else if (!match.Success && !match2.Success)
                    {
                        // set the progress to zero, it's irrelevant in the current state
                        if (State != SteamCMDState.STEAMCMD_DOWNLOADING_UPDATES &&
                            State != SteamCMDState.APP_VALIDATING &&
                            State != SteamCMDState.APP_PREALLOCATING &&
                            State != SteamCMDState.APP_DOWNLOADING &&
                            State != SteamCMDState.APP_POST_DOWNLOAD_VALIDATING)
                        {
                            Progress = 0;
                        }
                    }
                }
            }
        }
    }
}
