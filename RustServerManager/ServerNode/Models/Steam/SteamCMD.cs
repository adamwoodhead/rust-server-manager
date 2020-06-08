using Pty.Net;
using ServerNode.Logging;
using ServerNode.Models.Terminal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Models.Steam
{
    /// <summary>
    /// Disposable SteamCMD Object, preferrably called within a using statement.
    /// </summary>
    internal class SteamCMD : Terminal.Terminal, ITerminal, IDisposable
    {
        private SteamCMDState _state = SteamCMDState.UNDEFINED;
        private double _progress = 0;
        private long _downloadStartedOnBytes = -1;
        private long _totalBytesToDownload = 0;
        private long _totalDownloadedBytes = 0;
        private DateTime? downloadStartedDateTime;

        /// <summary>
        /// Event Handler raised when the current State is changed.
        /// </summary>
        public event EventHandler StateChanged;

        /// <summary>
        /// Event Handler raised when the Progress of SteamCMD has changed (if applicable to current state and procedure).
        /// </summary>
        public event EventHandler ProgressChanged;

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
        public override string ExecutablePath { get; set; }

        /// <summary>
        /// Boolean representation of whether steamcmd successfully installed the app
        /// </summary>
        public bool AppInstallationSuccess { get; private set; } = false;

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
        public SteamCMDState State
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
        public string InstallError { get; private set; }

        /// <summary>
        /// Current SteamCMD Procedure Progress, applicable depending on current state
        /// </summary>
        public double Progress
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
        public double AverageDownloadSpeed { get; private set; } = 0;

        /// <summary>
        /// Provides an estimated Time Left based on bytes requested, current download, and speed.
        /// </summary>
        public TimeSpan EstimatedDownloadTimeLeft { get; private set; } = TimeSpan.FromSeconds(0);

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
        /// Create a SteamCMD Object with a default input timeout of 30 seconds
        /// </summary>
        /// <param name="timeoutOnAwaitingInput">seconds to wait for input whilst in awaiting input state</param>
        public SteamCMD(int timeoutOnAwaitingInputMilliseconds) : base(timeoutOnAwaitingInputMilliseconds)
        {
            DeterminesInput = new string[] {
                "Steam>",
                "password:",
                "Enter the current code from your Steam Guard Mobile Authenticator appTwo-factor code:",
                "Steam Guard code:",
                "Two-factor code:"
            };

            ExecutablePath = GetNativeExectutablePath();
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
        public static void EnsureAvailable()
        {
            Log.Informational("Checking if SteamCMD is available.");

            // check if we have the steamcmd executable available
            if (!File.Exists(GetNativeExectutablePath()))
            {
                // check the current operating system is windows
                if (Utility.OperatingSystemHelper.IsWindows())
                {
                    // download and extract steacmd into the appdata utilities folder
                    DownloadSteamCMD();
                    Log.Success("SteamCMD successfully downloaded & now available");
                }
                else
                {
                    // this should only occur on linux, when the user has not installed prerequisites
                    Log.Error("SteamCMD not installed.");

                    Log.Informational("You can use the following commands to install steamcmd.");
                    Log.Informational("sudo add-apt-repository multiverse");
                    Log.Informational("sudo dpkg --add - architecture i386");
                    Log.Informational("sudo apt update");
                    Log.Informational("sudo apt install lib32gcc1");
                    Log.Informational("sudo apt install steamcmd");

                    throw new ApplicationException("SteamCMD not installed.");
                }
            }
            else
            {
                Log.Success("SteamCMD is Available.");
            }
        }

        /// <summary>
        /// Sends the "app_update" command followed by the steamdb app id.
        /// Followed with "validate" if applicable
        /// </summary>
        /// <param name="id"></param>
        /// <param name="validate"></param>
        /// <returns></returns>
        public async Task AppUpdate(int id, bool validate)
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
        /// Sends the "app_uninstall" command followed by the steamdb app id, and whether it should be a complete uninstall or only game files.
        /// Followed with "validate" if applicable
        /// </summary>
        /// <param name="id"></param>
        /// <param name="validate"></param>
        /// <returns></returns>
        public async Task AppUninstall(int id, bool complete)
        {
            if (complete)
            {
                await SendCommand(@$"app_uninstall {id} -complete");
            }
            else
            {
                await SendCommand(@$"app_uninstall {id}");
            }

            await ReadyForInputTsk.Task;
        }

        /// <summary>
        /// Sends the "force_install_dir" command, followed by the path parameter to the steamcmd
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task ForceInstallDirectory(string path)
        {
            await SendCommand($"force_install_dir \"{path}\"");

            await ReadyForInputTsk.Task;
        }

        public async Task<bool> Login(SteamApp steamApp)
        {
            if (steamApp.RequiresPurchase)
            {
                return await LoginUserPassword();
            }
            else
            {
                return await LoginAnonymously();
            }
        }

        /// <summary>
        /// Sends commands to steamcmd to login with username and password.
        /// Hangs if a steamguard auth code is required.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="steamGuard"></param>
        /// <returns></returns>
        private async Task<bool> LoginUserPassword()
        {
            State = SteamCMDState.LOGGING_IN;

            // ask the user for username
            // TODO add an api request for steam username
            Log.Informational("This app requires ownership on a steam account, you must login.");
            Log.Informational("Please provide your username and password.");

            // Lets give them more time, 10 seconds to type your
            // username, password and steamguard is a bit steep...
            await Task.Run(async() => {
                // we parse before we wait so cancelling would be overridden, wait one
                await Task.Delay(1000);
                CancelInputTimeout();
            });

            Log.Informational("Enter Your Username:");
            string username = await Utility.ConsoleHelper.InteruptReadForInput(true);

            Log.Informational("Enter Your Password:");
            string password = await Utility.ConsoleHelper.InteruptReadForInput(true);

            await SendCommand(@$"login {username} {password}");

            username = null;
            password = null;

            await ReadyForInputTsk.Task;

            if (State == SteamCMDState.LOGIN_REQUIRES_STEAMGUARD)
            {
                // Lets give them more time
                await Task.Run(async () => {
                    // we parse before we wait so cancelling would be overridden, wait one
                    await Task.Delay(1000);
                    CancelInputTimeout();
                });

                Log.Error("Your account requires a steam guard identification code, please supply this now!");
                Log.Informational("Enter Your Steam Guard Code:");

                string steamGuard = await Utility.ConsoleHelper.InteruptReadForInput(true);

                await SendCommand(steamGuard.ToUpper());

                await ReadyForInputTsk.Task;
            }

            return (LoggedIn);
        }

        /// <summary>
        /// Send commands to steamcmd to login anonymously (login anonymous)
        /// </summary>
        /// <returns></returns>
        private async Task<bool> LoginAnonymously()
        {
            State = SteamCMDState.LOGGING_IN;

            await SendCommand(@"login anonymous");

            await Task.WhenAny(ReadyForInputTsk.Task, Task.Delay(_timeoutOnAwaitingInput));

            if (!ReadyForInputTsk.Task.IsCompleted && State == SteamCMDState.LOGGED_IN)
            {
                Log.Verbose("SteamCMD login flag stalled, we're still ok");
                ReadyForInputTsk.TrySetResult(null);
            }

            return (LoggedIn);
        }

        /// <summary>
        /// Sends a command to steamcmd requesting a peaceful quit.
        /// Kills the process and pseudoterminal after x seconds if it does not exit peacefully.
        /// </summary>
        /// <returns></returns>
        public async Task Shutdown()
        {
            // steamcmd command to peacefully quit
            await SendCommand(@"quit");
        }

        /// <summary>
        /// Output handler for SteamCMD Process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void Terminal_ParseOutput(string data)
        {
            // remove whitespaces
            data = data.Trim();

            Log.Verbose($"SteamCMD> {data}");

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

                // login requires steam guard token
                else if (data.Contains("Steam Guard") || data.Contains("Two-factor")) { State = SteamCMDState.LOGIN_REQUIRES_STEAMGUARD; Progress = 0; }

                // invalid steamguard code
                else if (data.Contains("Two-factor code mismatch")) { State = SteamCMDState.LOGIN_FAILED_GUARD_MISMATCH; Progress = 0; }

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

                // steamcmd unsuccessfully install the app, there's an error!
                else if (data.Contains("Error! App"))
                {
                    if (data.Contains("0x202"))
                    {
                        State = SteamCMDState.APP_INSTALL_ERROR_NO_DISK;
                        InstallError = "Not Enough Space";
                    }
                    else
                    {
                        State = SteamCMDState.APP_INSTALL_ERROR;
                        InstallError = "Unhandled SteamCMD Error!";
                    }

                    Progress = 0;
                    AppInstallationSuccess = false;
                }

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
                            if (_downloadStartedOnBytes == -1)
                            {
                                _downloadStartedOnBytes = Convert.ToInt64(match3.Groups[2].Captures[0].Value);
                            }

                            // update the fields for downloaded and total
                            _totalDownloadedBytes = Convert.ToInt64(match3.Groups[2].Captures[0].Value);
                            _totalBytesToDownload = Convert.ToInt64(match3.Groups[4].Captures[0].Value);

                            // check that we're not on the first progress report
                            if (_totalDownloadedBytes != _downloadStartedOnBytes)
                            {
                                // estimate the average download speed
                                AverageDownloadSpeed = Utility.DownloadEstimator.EstimateSpeed(_downloadStartedOnBytes, _totalDownloadedBytes, DownloadStartedDateTime.Value);
                                // estimate the time left for download
                                EstimatedDownloadTimeLeft = Utility.DownloadEstimator.EstimateTimeLeft(_downloadStartedOnBytes, _totalDownloadedBytes, _totalBytesToDownload, DownloadStartedDateTime.Value);
                            }
                        }
                            
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the os native executable file for steamcmd
        /// </summary>
        /// <exception cref="ApplicationException"/>
        /// <returns></returns>
        public static string GetNativeExectutablePath()
        {
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // create a folder for the application
                string steamCmdFolder = Directory.CreateDirectory(Path.Combine(Program.WorkingDirectory, "SteamCMD")).FullName;
                // executable file for steamcmd
                string steamCmdExe = Path.Combine(steamCmdFolder, "steamcmd.exe");
                // TODO Steam CMD Executable Path (Windows)
                return steamCmdExe;
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                // pre-determined installation location for linux steamcmd (only checked Ubuntu 16.04)
                return @$"/usr/games/steamcmd";
            }
            else
            {
                throw new ApplicationException("Could not determine operating system for steamcmd use...");
            }
        }
    }
}
