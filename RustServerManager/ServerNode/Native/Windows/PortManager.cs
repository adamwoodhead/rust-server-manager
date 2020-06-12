using ServerNode.Logging;
using ServerNode.Models.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Native.Windows
{
    internal static class PortManager
    {
        public static bool AddFirewallRule(Server server)
        {
            string[] output1 = Powershell.Shell(Program.WorkingDirectory, $"/c New-NetFirewallRule -DisplayName \\\"Server {server.ID} TCP\\\" -Direction Inbound -LocalPort {server.Port} -Protocol TCP -Action Allow", true);

            if (output1.Count() > 0 && output1.FirstOrDefault().Contains("Access is denied."))
            {
                return false;
            }

            string[] output2 = Powershell.Shell(Program.WorkingDirectory, $"/c New-NetFirewallRule -DisplayName \\\"Server {server.ID} UDP\\\" -Direction Inbound -LocalPort {server.Port} -Protocol UDP -Action Allow", true);

            if (output2.Count() > 0 && output2.FirstOrDefault().Contains("Access is denied."))
            {
                return false;
            }

            return true;
        }

        public static bool RemoveFirewallRule(Server server)
        {
            string[] output1 = Powershell.Shell(Program.WorkingDirectory, $"/c Remove-NetFirewallRule -DisplayName \\\"Server {server.ID} TCP\\\"", true);

            if (output1.Count() > 0 && output1.FirstOrDefault().Contains("Access is denied."))
            {
                return false;
            }

            string[] output2 = Powershell.Shell(Program.WorkingDirectory, $"/c Remove-NetFirewallRule -DisplayName \\\"Server {server.ID} UDP\\\"", true);

            if (output2.Count() > 0 && output2.FirstOrDefault().Contains("Access is denied."))
            {
                return false;
            }

            return true;
        }
    }
}
