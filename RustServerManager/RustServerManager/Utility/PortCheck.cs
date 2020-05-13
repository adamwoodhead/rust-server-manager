using RustServerManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Utility
{
    internal static class PortCheck
    {
        internal static bool IsGamePortOpen(Gameserver gameserver)
        {
            using (System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient())
            {
                try
                {
                    tcpClient.Connect(gameserver.Server_IP, gameserver.Server_Port);
                    tcpClient.Close();

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        internal static bool IsRCONPortOpen(Gameserver gameserver)
        {
            using (System.Net.Sockets.UdpClient udpClient = new System.Net.Sockets.UdpClient())
            {
                try
                {
                    udpClient.Connect(gameserver.RCON_IP, gameserver.RCON_Port);
                    udpClient.Close();

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
