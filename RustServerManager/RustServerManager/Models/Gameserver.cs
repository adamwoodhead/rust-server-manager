using RustServerManager.Interfaces;
using RustServerManager.Models.Steam;
using RustServerManager.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace RustServerManager.Models
{
    [DataContract]
    public class Gameserver : IGameserver
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string RCON_IP { get; set; }

        [DataMember]
        public string RCON_Password { get; set; }

        [DataMember]
        public int RCON_Port { get; set; }

        [DataMember]
        public bool RCON_Web { get; set; }

        [DataMember]
        public string Server_Hostname { get; set; }

        [DataMember]
        public string Server_Identity { get; set; }

        [DataMember]
        public string Server_IP { get; set; }

        [DataMember]
        public int Server_MaxPlayers { get; set; }

        [DataMember]
        public int Server_Port { get; set; }

        [DataMember]
        public int Server_SaveInterval { get; set; }

        [DataMember]
        public string Server_Seed { get; set; }

        [DataMember]
        public int Server_Tickrate { get; set; }

        [DataMember]
        public int Server_WorldSize { get; set; }

        [DataMember]
        public string WorkingDirectory { get; set; }

        [DataMember]
        public bool IsInstalled { get; set; }

        [DataMember]
        public bool UmodInstalled { get; set; } = false;

        public bool IsRunning { get => (GameProcess != null) ? (bool)!GameProcess?.HasExited : false; }

        public bool ShouldRun { get; set; }

        public string Status { get; set; }

        //private string ServerDataDirectory
        //{
        //    get => App.Memory.Configuration;
        //}

        private string CommandLine
        {
            get => 
                $"-batchmode " +
                $"+server.ip {Server_IP} " +
                $"+server.port {Server_Port} " +
                $"+server.tickrate {Server_Tickrate} " +
                $"+server.hostname \"{Server_Hostname}\" " +
                $"+server.identity \"{Server_Identity}\" " +
                $"+server.seed {Server_Seed} " +
                $"+server.maxplayers {Server_MaxPlayers} " +
                $"+server.worldsize {Server_WorldSize} " +
                $"+server.saveinterval {Server_SaveInterval} " +
                $"+rcon.ip {RCON_IP} " +
                $"+rcon.port {RCON_Port} " +
                $"+rcon.password \"{RCON_Password}\" " +
                $"+rcon.web {(RCON_Web ? "1" : "0")} " +
                $"-logfile \"{DateTime.Now.ToString("dd-MM hh-mm")}{Server_Identity}.log\"";
        }

        internal Process GameProcess { get; set; }

        public Gameserver(bool creating = false)
        {
            if (creating)
            {
                ID = 0;
                Server_Port = 28015;
                RCON_Port = 28016;

                if (App.Memory.Gameservers.Count > 0)
                {
                    ID = App.Memory.Gameservers.Max(x => x.ID) + 1;
                    Server_Port = App.Memory.Gameservers.Max(x => x.Server_Port) + 10;
                    RCON_Port = Server_Port + 1;
                }
                
                Name = $"My New Rust Server";
                Server_IP = "0.0.0.0";
                RCON_IP = "0.0.0.0";
                RCON_Password = Generators.GetUniqueKey(8);
                RCON_Web = true;
                Server_Hostname = "My New Rust Server | Created Using RustSimpleServerManager";
                Server_Identity = $"server";
                Server_MaxPlayers = 100;
                Server_SaveInterval = 600;
                Server_Seed = Generators.GetUniqueKey(10, "1234567890");
                Server_Tickrate = 10;
                Server_WorldSize = 3000;
                IsInstalled = false;

                WorkingDirectory = Directory.CreateDirectory(Path.Combine(App.ServersDirectory, ID.ToString())).FullName;

                OpenPorts();

                App.Memory.Save();
            }
        }

        internal async Task InstallUmod()
        {
            await Task.Run(() => {

            });
        }

        internal async Task Install(IProgress<(SteamCMD.SteamCMDState, double)> progress = null)
        {
            await Task.Run(async() => {
                Console.WriteLine($"+login anonymous +force_install_dir \"{WorkingDirectory}\" +app_update 258550 validate +quit");
                SteamCMD steamCMD = SteamCMD.Run($"+login anonymous +force_install_dir \"{WorkingDirectory}\" +app_update 258550 validate +quit");

                steamCMD.StateChanged += (s, e) => { progress.Report((steamCMD.State, steamCMD.Progress)); };
                steamCMD.ProgressChanged += (s, e) => { progress.Report((steamCMD.State, steamCMD.Progress)); };
                steamCMD.Finished += (s, e) => { Console.WriteLine("steamcmd finished!?"); };
                
                while (!steamCMD.HasFinished)
                {
                    await Task.Delay(20);
                }

                IsInstalled = true;
                App.Memory.Save();
            });
        }

        internal async Task Uninstall()
        {
            await Task.Run(() => {
                Kill();

                if (Directory.Exists(WorkingDirectory))
                {
                    Directory.Delete(WorkingDirectory, true);
                }

                IsInstalled = false;
                App.Memory.Save();
            });
        }

        internal async void Reinstall()
        {
            await Install();

            await Uninstall();
        }

        private async void OpenPorts()
        {
            await Task.Run(() => {

                Process process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardInput = true
                    },
                    EnableRaisingEvents = true
                };

                process.Start();

                using (StreamWriter streamWriter = process.StandardInput)
                {
                    streamWriter.AutoFlush = true;

                    streamWriter.WriteLine("@echo off");

                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Game TCP ({ID})\" dir=in action=allow protocol=TCP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Game UDP ({ID})\" dir=in action=allow protocol=UDP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Rcon TCP ({ID})\" dir=in action=allow protocol=TCP localport={RCON_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Rcon UDP ({ID})\" dir=in action=allow protocol=UDP localport={RCON_Port}");
                }

                process.WaitForExit();
            });
        }

        internal async void ClosePorts()
        {
            await Task.Run(() => {

                Process process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardInput = true
                    },
                    EnableRaisingEvents = true
                };

                process.Start();

                using (StreamWriter streamWriter = process.StandardInput)
                {
                    streamWriter.AutoFlush = true;

                    streamWriter.WriteLine("@echo off");
                    
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Game TCP ({ID})\" protocol=TCP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Game UDP ({ID})\" protocol=UDP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Rcon TCP ({ID})\" protocol=TCP localport={RCON_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Rcon UDP ({ID})\" protocol=UDP localport={RCON_Port}");
                }

                process.WaitForExit();
            });
        }

        public async void Delete()
        {
            Kill();

            ClosePorts();

            await Uninstall();

            App.Memory.Gameservers.Remove(this);

            App.MainWindowInstance.Dispatcher.Invoke(() => {
                App.MainWindowInstance.ViewModel.GamserversViewModel.Gameservers.Remove(App.MainWindowInstance.ViewModel.GamserversViewModel.Gameservers.FirstOrDefault(x => x.ID == ID));
            }, System.Windows.Threading.DispatcherPriority.Normal);

            App.Memory.Save();
        }

        public void Start()
        {
            if (IsInstalled)
            {
                ShouldRun = true;

                GameProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = WorkingDirectory,
                        FileName = Path.Combine(WorkingDirectory, "RustDedicated.exe"),
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

                KeepAlive(GameProcess);
            }
        }

        public void Stop()
        {
            ShouldRun = false;

            try
            {
                WebRcon.RconService rcon = new WebRcon.RconService();
                rcon.Connect($"{RCON_IP}:{RCON_Port}", RCON_Password);
                if (rcon.IsConnected)
                {
                    rcon.Request("quit");
                }

                GameProcess?.WaitForExit(5000);

                if (GameProcess.HasExited)
                {
                    GameProcess = null;
                    return;
                }
                else
                {
                    Kill();
                }
            }
            catch (Exception)
            {
                Kill();
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Kill()
        {
            ShouldRun = false;

            GameProcess?.Kill();
            GameProcess?.WaitForExit();
        }

        public void WipeMap()
        {
            throw new NotImplementedException();
        }

        public void WipeMapAndBP()
        {
            throw new NotImplementedException();
        }

        public void KeepAlive(Process process)
        {
            Task.Run(async() => {
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
