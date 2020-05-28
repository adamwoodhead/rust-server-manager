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
    internal class SteamCMD : IDisposable
    {
        private CancellationTokenSource CancellationTokenSource;
        private CancellationToken CancellationToken;
        private UTF8Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private SteamCMDState _state = SteamCMDState.UNDEFINED;
        private double _progress = 0;
        private bool _hasFinished = false;
        private int _timeoutOnAwaitingInput;
        private TaskCompletionSource<object?> ReadyForInputTsk;
        private Task ReadyForInputTimeoutTsk;
        private CancellationTokenSource ReadyForInputTimeoutTskCts;
        private bool disposedValue;
        private long _downloadStartedOnByteCount = -1;
        private long _totalDownloadBytes = 0;
        private long _totalDownloadedBytes = 0;
        private DateTime? downloadStartedDateTime;

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

        /// <summary>
        /// OS Native PseudoTerminal Object
        /// </summary>
        private IPtyConnection Terminal { get; set; }

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
        /// The executable path for SteamCMD in Windows
        /// </summary>
        private static string WinExecutablePath
        {
            get
            {
                // create a folder for the application
                string steamCmdFolder = Directory.CreateDirectory(Path.Combine(Program.WorkingDirectory, "SteamCMD")).FullName;
                // executable file for steamcmd
                string steamCmdExe = Path.Combine(steamCmdFolder, "steamcmd.exe");
                // TODO Steam CMD Executable Path (Windows)
                return steamCmdExe;
            }
        }

        /// <summary>
        /// The executable path for SteamCMD in Linux
        /// </summary>
        private static string LinExecutablePath
        {
            get
            {
                // pre-determined installation location for linux steamcmd (only checked Ubuntu 16.04)
                return @$"/usr/lib/games/steam/steamcmd.sh";
            }
        }

        /// <summary>
        /// Check if the SteamCMD Executable exists
        /// </summary>
        /// <returns></returns>
        private static bool ExecutableExists()
        {
            // Check if the variable file exists, depending in windows/linux
            if (Utility.OperatingSystemHelper.IsWindows() && File.Exists(WinExecutablePath))
            {
                // File Found
                return true;
            }
            else if (Utility.OperatingSystemHelper.IsLinux() && File.Exists(LinExecutablePath))
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
        internal bool AppInstallationSuccess { get; private set; } = false;

        /// <summary>
        /// The DateTime capture for when steamcmd began an 'app_update'
        /// </summary>
        private DateTime? DownloadStartedDateTime
        {
            get => downloadStartedDateTime;
            set
            {
                if (downloadStartedDateTime == null)
                {
                    downloadStartedDateTime = value;
                }
            }
        }

        /// <summary>
        /// Boolean representation of whether steamcmd successfully logged in
        /// </summary>
        private bool LoggedIn { get; set; } = false;

        /// <summary>
        /// Current SteamCMD Procedure State
        /// </summary>
        internal SteamCMDState State
        {
            get => _state;
            private set
            {
                if (_state != value || value == SteamCMDState.AWAITING_INPUT)
                {
                    _state = value;
                    OnStateChanged(_state);
                }
            }
        }

        /// <summary>
        /// If SteamCMD fails an installation, this should contain the reason.
        /// </summary>
        internal string InstallError { get; private set; }

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
        /// Provides an estimated download speed based on bytes downloaded compated to total requested.
        /// </summary>
        internal double AverageDownloadSpeed { get; private set; } = 0;

        /// <summary>
        /// Provides an estimated Time Left based on bytes requested, current download, and speed.
        /// </summary>
        internal TimeSpan EstimatedDownloadTimeLeft { get; private set; } = TimeSpan.FromSeconds(0);

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
        /// Create a SteamCMD Object with a default input timeout of 30 seconds
        /// </summary>
        /// <param name="timeoutOnAwaitingInput">seconds to wait for input whilst in awaiting input state</param>
        private SteamCMD(int timeoutOnAwaitingInputMilliseconds)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            _timeoutOnAwaitingInput = timeoutOnAwaitingInputMilliseconds;
        }

        /// <summary>
        /// Instantiate a new terminal containing SteamCMD natively, and return upon input available
        /// </summary>
        /// <param name="timeoutOnAwaitingInputMilliseconds"></param>
        /// <returns></returns>
        internal static async Task<SteamCMD> Instantiate(int timeoutOnAwaitingInputMilliseconds = 30000)
        {
            SteamCMD steamCMD = new SteamCMD(timeoutOnAwaitingInputMilliseconds);

            try
            {
                await steamCMD.ConnectToTerminal();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not start the terminal", ex);
            }

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                await steamCMD.SendCommand(WinExecutablePath);
            }
            else
            {
                await steamCMD.SendCommand(LinExecutablePath);
            }

            await steamCMD.ReadyForInputTsk.Task;

            return steamCMD;
        }

        /// <summary>
        /// Download and extract the steamcmd executable for windows
        /// </summary>
        private static void DownloadSteamCMD()
        {
            // this should only be used on windows, as it's a prerequisite for linux users to install manually before running the application
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // get the application folder
                string steamCmdFolder = Directory.CreateDirectory(Path.Combine(Program.WorkingDirectory, "SteamCMD")).FullName;
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

                Thread.Sleep(1000);
            }
            else
            {
                // this has been called on an operating system other than windows, that should never happen
                throw new ApplicationException("should only be called on windows");
            }
        }

        /// <summary>
        /// Check if steamcmd exists.
        /// If it doesn't exist and we're currently on the windows os, download and unzip, then cleanup
        /// </summary>
        internal static void EnsureAvailable()
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
        }

        /// <summary>
        /// Sends the "app_update" command followed by the steamdb app id.
        /// Followed with "validate" if applicable
        /// </summary>
        /// <param name="id"></param>
        /// <param name="validate"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Sends the "force_install_dir" command, followed by the path parameter to the steamcmd
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal async Task ForceInstallDirectory(string path)
        {
            await SendCommand(@$"{path}");

            await ReadyForInputTsk.Task;
        }

        /// <summary>
        /// Sends commands to steamcmd to login with username and password.
        /// Hangs if a steamguard auth code is required.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="steamGuard"></param>
        /// <returns></returns>
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
                    await SendCommand(Console.ReadLine());
                }
            }

            return (LoggedIn);
        }

        /// <summary>
        /// Send commands to steamcmd to login anonymously (login anonymous)
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> LoginAnonymously()
        {
            State = SteamCMDState.LOGGING_IN;

            await SendCommand(@"login anonymous");

            await ReadyForInputTsk.Task;

            return (LoggedIn);
        }

        /// <summary>
        /// Sends a command to steamcmd requesting a peaceful quit.
        /// Kills the process and pseudoterminal after x seconds if it does not exit peacefully.
        /// </summary>
        /// <returns></returns>
        internal async Task Shutdown(int timeout = 10)
        {
            // steamcmd command to peacefully quit
            await SendCommand(@"quit");

            // Wait one second for steamcmd to shutdown peacefully
            await Task.Delay(1000);

            CancellationTokenSource.Cancel();
            CancelInputTimeout();

            // Exit the terminal peacefully
            //await SendCommand(@"exit");

            System.Timers.Timer aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            int seconds = timeout + 1;
            aTimer.Elapsed += delegate { Console.WriteLine($"Waiting for steam to close peacefully.. {--seconds}"); };
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

        /// <summary>
        /// Cancel the input timeout task if it's not null
        /// </summary>
        private void CancelInputTimeout()
        {
            ReadyForInputTimeoutTskCts?.Cancel();
            ReadyForInputTimeoutTsk = null;
        }

        /// <summary>
        /// Create new input timeout task
        /// </summary>
        private void ApplyInputTimeout()
        {
            if (ReadyForInputTimeoutTsk != null)
            {
                ReadyForInputTimeoutTskCts.Cancel();
            }

            ReadyForInputTimeoutTskCts = new CancellationTokenSource();
            ReadyForInputTimeoutTsk = Task.Run(async () =>
            {
                CancellationTokenSource localToken = ReadyForInputTimeoutTskCts;
                await Task.Delay(_timeoutOnAwaitingInput);
                if (!localToken.IsCancellationRequested && !HasFinished && CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested)
                {
                    Console.WriteLine($"Exceeded Awaiting Input Timeout: {_timeoutOnAwaitingInput}ms");
                    CancellationTokenSource.Cancel();
                }
            }, ReadyForInputTimeoutTskCts.Token);
        }

        /// <summary>
        /// Spawn a new Pseudoterminal, begin asyncronously reading its input and output, apply terminal events
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToTerminal()
        {
            string app = Utility.OperatingSystemHelper.IsWindows() ? Path.Combine(Environment.SystemDirectory, "cmd.exe") : "sh";
            PtyOptions options = new PtyOptions
            {
                Name = "SteamCMD Terminal",
                // TODO this should be quite long, and cover anything that steamcmd can spit out in a single line + the current directory length
                Cols = Environment.CurrentDirectory.Length + app.Length + 200,
                // we want it line by line, no more than that
                Rows = 1,
                Cwd = Environment.CurrentDirectory,
                App = app
            };

            Terminal = await PtyProvider.SpawnAsync(options, this.CancellationToken);

            var processExitedTcs = new TaskCompletionSource<uint>();
            Terminal.ProcessExited += (sender, e) =>
            {
                HasFinished = true;

                processExitedTcs.TrySetResult((uint)Terminal.ExitCode);

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
                            ApplyInputTimeout();
                        }
                        else if (output == "password:")
                        {
                            State = SteamCMDState.LOGIN_REQUIRES_PASSWORD;
                            ReadyForInputTsk.SetResult(null);
                            ApplyInputTimeout();
                        }
                        else if (output == "Enter the current code from your Steam Guard Mobile Authenticator appTwo-factor code:"
                        || output == "Please check your email for the message from Steam, and enter the Steam Guard code from that message.You can also enter this code at any time using 'set_steam_guard_code' at the console.Steam Guard code:"
                        || output == "Two-factor code:")
                        {
                            State = SteamCMDState.LOGIN_REQUIRES_STEAMGUARD;
                            ReadyForInputTsk.SetResult(null);
                            ApplyInputTimeout();
                        }
                        else
                        {
                            CancelInputTimeout();
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

        /// <summary>
        /// Asyncronously writes a buffered string to the terminal input stream followed by an enter (0x0D) byte, then asyncronously flush
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task SendCommand(string command)
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                ReadyForInputTsk = new TaskCompletionSource<object?>();
                byte[] commandBuffer = Encoding.GetBytes(command);
                await Terminal.WriterStream.WriteAsync(commandBuffer, 0, commandBuffer.Length, this.CancellationToken);
                await Terminal.WriterStream.WriteAsync(new byte[] { 0x0D }, 0, 1, this.CancellationToken);
                await Terminal.WriterStream.FlushAsync();
            }
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
                else if (data.Contains("Logged in OK")) { State = SteamCMDState.LOGGED_IN; LoggedIn = true; Progress = 0; }

                // ----- steamcmd app installation section
                // steamcmd is validating the app
                else if (data.Contains("Update state (0x5) validating")) { State = SteamCMDState.APP_VALIDATING; shouldCheckProgress = true; }

                // steamcmd validation failed completely so it's now preallocating the storage space for full installation
                else if (data.Contains("Update state (0x11) preallocating")) { State = SteamCMDState.APP_PREALLOCATING; shouldCheckProgress = true; }

                // steamcmd is now downloading the app
                else if (data.Contains("Update state (0x61) downloading")) { State = SteamCMDState.APP_DOWNLOADING; shouldCheckProgress = true; DownloadStartedDateTime = DateTime.Now; }

                // steamcmd is now validating the app
                else if (data.Contains("Update state (0x5) validating") && State == SteamCMDState.APP_DOWNLOADING) { State = SteamCMDState.APP_POST_DOWNLOAD_VALIDATING; shouldCheckProgress = true; }

                // steamcmd is now validating the app
                else if (data.Contains("Update state (0x5) verifying")) { State = SteamCMDState.APP_VERIFYING; shouldCheckProgress = true; }

                // steamcmd successfully install the app
                else if (data.Contains("Success! App") && data.Contains("fully installed")) { Progress = 100; State = SteamCMDState.APP_INSTALLED; AppInstallationSuccess = true; }

                // steamcmd successfully install the app
                else if (data.Contains("Error! App") && data.Contains("state is 0x202 after update job.")) { Progress = 0; State = SteamCMDState.APP_INSTALL_ERROR; InstallError = "Not Enough Space"; AppInstallationSuccess = false; }

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
                            State != SteamCMDState.APP_POST_DOWNLOAD_VALIDATING &&
                            State != SteamCMDState.APP_VERIFYING)
                        {
                            Progress = 0;
                        }
                    }

                    // regex specifically for app downloading
                    Regex regex = new Regex(@"(Update state \(0x61\) downloading, progress: \d{1,3}\.\d{1,2} \()(\d\w+)( \/ )(\d\w+)(.*)");
                    Match match3 = regex.Match(data);

                    // we have a full correct match with data
                    if (match3.Groups.Count == 6)
                    {
                        try
                        {
                            // _downloadStartedOnByteCount initializes on -1 so that we set it ONCE
                            if (_downloadStartedOnByteCount == -1)
                            {
                                _downloadStartedOnByteCount = Convert.ToInt64(match3.Groups[2].Captures[0].Value);
                            }

                            // update the fields for downloaded and total
                            _totalDownloadedBytes = Convert.ToInt64(match3.Groups[2].Captures[0].Value);
                            _totalDownloadBytes = Convert.ToInt64(match3.Groups[4].Captures[0].Value);

                            // check that we're not on the first progress report
                            if (_totalDownloadedBytes != _downloadStartedOnByteCount)
                            {
                                // estimate the average download speed
                                AverageDownloadSpeed = Utility.DownloadEstimator.EstimateSpeed(_downloadStartedOnByteCount, _totalDownloadedBytes, DownloadStartedDateTime.Value);
                                // estimate the time left for download
                                EstimatedDownloadTimeLeft = Utility.DownloadEstimator.EstimateTimeLeft(_downloadStartedOnByteCount, _totalDownloadedBytes, _totalDownloadBytes, DownloadStartedDateTime.Value);
                            }
                        }
                            
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// An instance of steamcmd should really only be used once for simplification, we all using tags by implementing IDisposable.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        Terminal.Dispose();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("An error occured whilst trying to dispose of the terminal", ex);
                    }
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// IDisposable Implementation
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
