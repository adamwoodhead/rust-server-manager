using Pty.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RustServerManager.Models.Steam
{
    internal class SteamCMD
    {
        internal enum SteamCMDState
        {
            UNDEFINED,

            //
            STEAMCMD_CHECKING_UPDATES,      // [  0%] Checking for available update...
            STEAMCMD_DOWNLOADING_UPDATES,   // [  0%] Downloading update (2,523 of 39,758 KB)...
            STEAMCMD_EXTRACTING_PACKAGES,   // [----] Extracting package...
            STEAMCMD_INSTALLING_UPDATE,     // [----] Installing update...
            STEAMCMD_VERIFYING,             // [----] Verifying installation...
            STEAMCMD_LOADED,                // Loading Steam API...OK.

            // 
            APP_VALIDATING,                 // Update state (0x5) validating, progress: 0.03 (1401888 / 5364833225)
            APP_PREALLOCATING,              // Update state (0x11) preallocating, progress: 31.23 (1675318076 / 5364833225)
            APP_DOWNLOADING,                // Update state (0x61) downloading, progress: 0.06 (3145728 / 5364833225)
            APP_POST_DOWNLOAD_VALIDATING,   // Update state (0x5) validating, progress: 0.03 (1401888 / 5364833225)
            APP_INSTALLED                   // Success! App '258550' fully installed.
        }

        private SteamCMDState _state = SteamCMDState.UNDEFINED;
        private double _progress = 0;
        private bool _hasFinished = false;

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

        internal event EventHandler Finished;

        internal event EventHandler StateChanged;

        internal event EventHandler ProgressChanged;

        internal static string WorkingDirectory { get => Path.Combine(App.Memory.Configuration.WorkingDirectory, "SteamCMD"); }

        private static string SteamCMD_EXE { get; set; }
        
        private IPtyConnection Connection { get; set; }

        private SteamCMD(string steamcmd_arguments)
        {
            Console.WriteLine("Starting with: " + $"powershell.exe /c \"{SteamCMD_EXE} {steamcmd_arguments}\"");

            Connection = PtyProvider.Spawn($"powershell.exe /c \"{SteamCMD_EXE} {steamcmd_arguments}\"", 300, 1);

            Connection.PtyData += Connection_PtyData;
            Connection.PtyDisconnected += (e) => { HasFinished = true; };
        }

        internal static SteamCMD Run(string steamcmd_arguments)
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                throw new ArgumentNullException(nameof(WorkingDirectory));
            }
            else if (string.IsNullOrEmpty(SteamCMD_EXE))
            {
                SteamCMD_EXE = Path.Combine(WorkingDirectory, "steamcmd.exe");
            }

            if (!File.Exists(SteamCMD_EXE))
            {
                Download();
            }

            return new SteamCMD(steamcmd_arguments);
        }

        private void Connection_PtyData(object sender, string data)
        {
            Console.WriteLine("Original Response: " + data);

            string message = Regex.Replace(data, @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", "");

            message = message.Trim();

            if (!string.IsNullOrEmpty(message))
            {
                if (message.Contains("] Checking for available update")) { State = SteamCMDState.STEAMCMD_CHECKING_UPDATES; Progress = 0; }
                else if (message.Contains("] Downloading update")) { State = SteamCMDState.STEAMCMD_DOWNLOADING_UPDATES; }
                else if (message.Contains("] Extracting package")) { State = SteamCMDState.STEAMCMD_EXTRACTING_PACKAGES; Progress = 0; }
                else if (message.Contains("] Installing update")) { State = SteamCMDState.STEAMCMD_INSTALLING_UPDATE; Progress = 0; }
                else if (message.Contains("] Verifying installation")) { State = SteamCMDState.STEAMCMD_VERIFYING; Progress = 0; }
                else if (message.Contains("Loading Steam API...")) { State = SteamCMDState.STEAMCMD_LOADED; Progress = 0; }

                else if (message.Contains("Update state (0x5) validating")) { State = SteamCMDState.APP_VALIDATING; }
                else if (message.Contains("Update state (0x11) preallocating")) { State = SteamCMDState.APP_PREALLOCATING; }
                else if (message.Contains("Update state (0x61) downloading")) { State = SteamCMDState.APP_DOWNLOADING; }
                else if (message.Contains("Update state (0x5) validating") && State == SteamCMDState.APP_DOWNLOADING) { State = SteamCMDState.APP_POST_DOWNLOAD_VALIDATING; }
                else if (message.Contains("Success! App '258550' fully installed")) { State = SteamCMDState.APP_INSTALLED; Progress = 100; }

                Match match = Regex.Match(message, @"(\d{1,2}\.\d{1,2})");
                Match match2 = Regex.Match(message, @"(\[.{0,2}\d{1,3}\%\])");
                if (match.Success)
                {
                    try
                    {
                        Progress = Convert.ToDouble(match.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw;
                    }
                }
                else if (match2.Success)
                {
                    try
                    {
                        Match digitMatch = Regex.Match(match2.Value, @"(\d{1,3})");
                        if (digitMatch.Success)
                        {
                            Progress = Convert.ToDouble(digitMatch.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw;
                    }
                }
                else if (!match.Success && !match2.Success)
                {
                    Progress = 0;
                }
            }
        }

        private static void Download()
        {
            string steamcmd_zip = Path.Combine(WorkingDirectory, "steamcmd.zip");

            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", steamcmd_zip);
            }

            ZipFile.ExtractToDirectory(steamcmd_zip, WorkingDirectory);

            File.Delete(steamcmd_zip);
        }

        protected virtual void OnStateChanged(SteamCMDState _state)
        {
            StateChangedEventArgs e = new StateChangedEventArgs(_state);
            StateChanged?.Invoke(this, e);
        }

        protected virtual void OnProgressChanged(double _progress)
        {
            ProgressChangedEventArgs e = new ProgressChangedEventArgs(_progress);
            ProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnFinished()
        {
            Finished?.Invoke(this, null);
        }
    }
}
