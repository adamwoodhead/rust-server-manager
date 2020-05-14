using RustServerManager.Interfaces;
using RustServerManager.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            }
        }

        public void Delete()
        {
            throw new NotImplementedException();
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
