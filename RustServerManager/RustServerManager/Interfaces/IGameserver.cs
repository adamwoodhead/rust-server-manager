using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Interfaces
{
    internal interface IGameserver
    {
        int ID { get; set; }
        string Name { get; set; }
        string RCON_IP { get; set; }
        string RCON_Password { get; set; }
        int RCON_Port { get; set; }
        bool RCON_Web { get; set; }
        string Server_Hostname { get; set; }
        string Server_Identity { get; set; }
        string Server_IP { get; set; }
        int Server_MaxPlayers { get; set; }
        int Server_Port { get; set; }
        int Server_SaveInterval { get; set; }
        string Server_Seed { get; set; }
        int Server_Tickrate { get; set; }
        int Server_WorldSize { get; set; }
        bool IsInstalled { get; set; }
        bool IsRunning { get; set; }
        void Start();
        void Restart();
        void Stop();
        void Kill();
        void Install();
        void Reinstall();
        void Uninstall();
        void Delete();
        void WipeMap();
        void WipeMapAndBP();
    }
}
