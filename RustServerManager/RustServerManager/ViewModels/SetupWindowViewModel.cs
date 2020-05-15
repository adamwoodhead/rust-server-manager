using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RustServerManager.ViewModels
{
    public class SetupWindowViewModel : ObservableViewModel
    {
        private readonly string SteamCMDURL = @"https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

        private string _status = "Doing Something...";
        private int _progressValue = 0;

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        public ICommand Loaded { get; set; }

        public bool Success { get; internal set; } = false;

        private Views.SetupWindow _setupWindow;

        public SetupWindowViewModel(Views.SetupWindow setupWindow)
        {
            _setupWindow = setupWindow;
            Loaded = new CommandImplementation(o => HasLoaded());
        }

        private void CancelSetup()
        {
            MessageBox.Show("Setup has been cancelled.");
            _setupWindow.Close();
        }

        public async void HasLoaded()
        {
            await Task.Delay(500);

            // Directories...
            Status = "Creating Directories";
            string _appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string _saveFolder = Path.Combine(_appDataFolder, "RSSM");
            Directory.CreateDirectory(_saveFolder);

            // Select Folder For RustServers & SteamCMD...
            Status = "Selecting Folder...";

            string baseDir = QueryFolder();

            if (string.IsNullOrEmpty(baseDir))
            {
                CancelSetup();
                return;
            }

            App.Memory.Configuration.RustServerDirectory = baseDir;
            string steamCMDDir = Directory.CreateDirectory(Path.Combine(App.Memory.Configuration.RustServerDirectory, "SteamCMD")).FullName;
            string rustServerDir = Directory.CreateDirectory(Path.Combine(App.Memory.Configuration.RustServerDirectory, "RustDedicated")).FullName;
            ProgressValue = 20;

            // Download SteamCMD...
            Status = "Downloading Steam CMD";
            string steamCMDZip = Path.Combine(steamCMDDir, "steamcmd.zip");
            string steamCMDEXE = Path.Combine(steamCMDDir, "steamcmd.exe");

            using (var client = new WebClient())
            {
                client.DownloadFile(SteamCMDURL, steamCMDZip);
            }
            
            // Unzip SteamCMD
            Status = "Unzipping Steam CMD";
            ZipFile.ExtractToDirectory(steamCMDZip, steamCMDDir);
            File.Delete(steamCMDZip);
            ProgressValue = 40;

            // Install SteamCMD (firstrun)
            Status = "Installing SteamCMD";

            Progress<string> progresser = new Progress<string>();

            progresser.ProgressChanged += Progresser_ProgressChanged;

            await Task.Run(() => { Models.SteamCMD.FirstLaunch(progresser); });

            ProgressValue = 60;

            // Download Rust...
            Status = "Downloading Rust";
            ProgressValue = 80;
            //commit problems
        }

        private void Progresser_ProgressChanged(object sender, string e)
        {
            Status = e;
        }

        private string QueryFolder()
        {
            string baseDir = string.Empty;

            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Please select, or create a directory where you would like Rust Server Manager to store the rust server(s) files.";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(dialog.SelectedPath) || !Directory.Exists(dialog.SelectedPath))
                    {
                        return QueryFolder();
                    }
                    else
                    {
                        baseDir = dialog.SelectedPath;
                        return baseDir;
                    }                    
                }
                else
                {
                    Success = false;
                    return null;
                }
            }
        }

        private void SetupDirectories()
        {

        }

        private void DownloadSteamCMD()
        {

        }

        private void DownloadRust()
        {

        }

        private bool RustExists()
        {
            return true;
        }
    }
}
