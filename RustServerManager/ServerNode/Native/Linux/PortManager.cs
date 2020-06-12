using ServerNode.Models.Servers;
using System;
using System.Linq;

namespace ServerNode.Native.Linux
{
    internal class PortManager
    {
        // TODO Linux Add Firewall Rule
        internal static bool AddFirewallRule(Server server)
        {
            string[] output1 = SH.Shell(Program.WorkingDirectory, $"-c \"iptables -A INPUT -p udp --dport {server.Port} -j ACCEPT\"");

            if (output1.Count() > 0 && output1.FirstOrDefault().Contains("Permission denied."))
            {
                return false;
            }

            string[] output2 = SH.Shell(Program.WorkingDirectory, $"-c \"iptables -A INPUT -p tcp --dport {server.Port} -j ACCEPT\"");

            if (output2.Count() > 0 && output2.FirstOrDefault().Contains("Permission denied."))
            {
                return false;
            }

            return true;
        }

        // TODO Linux Remove Firewall Rule
        internal static bool RemoveFirewallRule(Server server)
        {
            string[] output1 = SH.Shell(Program.WorkingDirectory, $"-c \"iptables -D INPUT -p udp --dport {server.Port} -j ACCEPT\"");

            if (output1.Count() > 0 && output1.FirstOrDefault().Contains("Permission denied."))
            {
                return false;
            }

            string[] output2 = SH.Shell(Program.WorkingDirectory, $"-c \"iptables -D INPUT -p tcp --dport {server.Port} -j ACCEPT\"");

            if (output2.Count() > 0 && output2.FirstOrDefault().Contains("Permission denied."))
            {
                return false;
            }

            return true;
        }
    }
}