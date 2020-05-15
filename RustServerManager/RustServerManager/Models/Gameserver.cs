using RustServerManager.Interfaces;
using RustServerManager.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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

        public bool IsRunning { get; set; } = false;

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

        private Process GameProcess { get; set; }

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
                Server_Identity = $"server_{ID}";
                Server_MaxPlayers = 100;
                Server_SaveInterval = 600;
                Server_Seed = Generators.GetUniqueKey(10, "1234567890");
                Server_Tickrate = 10;
                Server_WorldSize = 3000;
                IsRunning = false;

                Directory.CreateDirectory(Path.Combine(App.Memory.Configuration.ServerIdentityDirectory, Server_Identity));

                OpenPorts();
            }
        }

        internal async void OpenPorts()
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
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += OpenPortsProcess_OutputDataReceived;
                process.ErrorDataReceived += OpenPortsProcess_ErrorDataReceived;

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (StreamWriter streamWriter = process.StandardInput)
                {
                    streamWriter.AutoFlush = true;

                    streamWriter.WriteLine("@echo off");

                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Game TCP ({Server_Identity})\" dir=in action=allow protocol=TCP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Game UDP ({Server_Identity})\" dir=in action=allow protocol=UDP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Rcon TCP ({Server_Identity})\" dir=in action=allow protocol=TCP localport={RCON_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall add rule name=\"Rust Rcon UDP ({Server_Identity})\" dir=in action=allow protocol=UDP localport={RCON_Port}");
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
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += OpenPortsProcess_OutputDataReceived;
                process.ErrorDataReceived += OpenPortsProcess_ErrorDataReceived;

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (StreamWriter streamWriter = process.StandardInput)
                {
                    streamWriter.AutoFlush = true;

                    streamWriter.WriteLine("@echo off");
                    
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Game TCP ({Server_Identity})\" protocol=TCP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Game UDP ({Server_Identity})\" protocol=UDP localport={Server_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Rcon TCP ({Server_Identity})\" protocol=TCP localport={RCON_Port}");
                    streamWriter.WriteLine($"netsh advfirewall firewall delete rule name=\"Rust Rcon UDP ({Server_Identity})\" protocol=UDP localport={RCON_Port}");
                }

                process.WaitForExit();
            });
        }

        private void OpenPortsProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Error: {e.Data}");
        }

        private void OpenPortsProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Response: {e.Data}");
        }

        public void Delete()
        {
            Kill();
            ClosePorts();
            Directory.Delete(Path.Combine(App.Memory.Configuration.ServerIdentityDirectory, Server_Identity), true);
            App.Memory.Gameservers.Remove(this);
            App.MainWindowInstance.ViewModel.GamserversViewModel.Gameservers = new System.Collections.ObjectModel.ObservableCollection<ViewModels.GameserverViewModel>(App.Memory.Gameservers.Select(x => new ViewModels.GameserverViewModel(x)));
        }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Kill()
        {
            GameProcess?.Kill();
            IsRunning = false;
        }

        public void WipeMap()
        {
            throw new NotImplementedException();
        }

        public void WipeMapAndBP()
        {
            throw new NotImplementedException();
        }
    }
}
