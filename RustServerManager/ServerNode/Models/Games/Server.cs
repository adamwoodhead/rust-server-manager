using Pty.Net;
using ServerNode.Models.Steam;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Games
{
    internal abstract class Server
    {
        int ID { get; set; }

        string IPAddress { get; set; }

        string Port { get; set; }

        string SafeStopCommand { get; set; }

        string WorkingDirectory { get; set; }

        int? SteamDBID { get; set; }

        IPtyConnection PseudoTerminal { get; set; }

        IPtyConnection Commandline { get; }

        bool Install()
        {
            throw new NotImplementedException();
        }

        void Kill()
        {
            this.PseudoTerminal.Kill();
        }

        void Reinstall()
        {
            throw new NotImplementedException();
        }

        bool Restart()
        {
            Stop();
            Start();
        }

        bool Start()
        {

        }

        bool Stop()
        {
            throw new NotImplementedException();
        }

        bool Uninstall()
        {
            throw new NotImplementedException();
        }
    }
}
