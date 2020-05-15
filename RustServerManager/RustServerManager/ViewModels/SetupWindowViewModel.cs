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
        private bool _firstTime = false;

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

        public SetupWindowViewModel(Views.SetupWindow setupWindow, bool firstTime)
        {
            _firstTime = firstTime;
            _setupWindow = setupWindow;
            Loaded = new CommandImplementation(o => HasLoaded());
        }

        private void CancelSetup()
        {
            MessageBox.Show("Setup has been cancelled.");
            Close();
        }

        private void Close()
        {
            _setupWindow.Close();
        }

        public async void HasLoaded()
        {
            await Task.Delay(20);

            if (_firstTime)
            {
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
                

                Directory.CreateDirectory(steamCMDDir);
                Directory.CreateDirectory(rustServerDir);
            }
            

            ProgressValue = 20;
            string steamCMDZip = Path.Combine(App.Memory.Configuration.SteamCMDFolder, "steamcmd.zip");
            string steamCMDEXE = App.Memory.Configuration.SteamCMDExecutable;

            if (!File.Exists(steamCMDEXE))
            {
                // Download SteamCMD...
                Status = "Downloading Steam CMD";

                using (var client = new WebClient())
                {
                    client.DownloadFile(SteamCMDURL, steamCMDZip);
                }

                // Unzip SteamCMD
                Status = "Unzipping Steam CMD";
                ZipFile.ExtractToDirectory(steamCMDZip, App.Memory.Configuration.SteamCMDFolder);
                File.Delete(steamCMDZip);
                ProgressValue = 40;

                // Install SteamCMD (firstrun)
                Status = "Installing SteamCMD";

                await Models.SteamCMD.FirstLaunch();
            }
            else
            {
                ProgressValue = 40;
                // Install SteamCMD (firstrun)
                Status = "Updating & Validating SteamCMD";

                await Models.SteamCMD.FirstLaunch();
            }

            ProgressValue = 60;

            // Download Rust...
            if (_firstTime)
            {
                Status = "Downloading Rust Dedicated Server";
                ProgressValue = 80;
            }
            else
            {
                Status = "Updating & Validating Rust Dedicated Server";
                ProgressValue = 80;
            }
            
            await Models.SteamCMD.DownloadRust();
            ProgressValue = 100;

            Success = true;

            Close();
        }

        private string QueryFolder()
        {
            string baseDir = string.Empty;

            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Please select, or create a directory where you would like Rust Server Manager to store the rust server(s) files.";
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
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
