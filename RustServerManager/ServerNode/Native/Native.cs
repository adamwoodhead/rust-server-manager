using ServerNode.Logging;
using ServerNode.Models.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Native
{
    internal static class Native
    {
        /// <summary>
        /// Perform a native shell execution in the stated working directory, returns shell script output.
        /// Script will have to be predefined for the native system.
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static string[] Shell(string workingDir, string script)
        {
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                return Windows.Powershell.Shell(workingDir, script);
            }
            else
            {
                return Linux.SH.Shell(workingDir, script);
            }
        }

        public static bool AddFirewallRule(Server server)
        {
            Log.Verbose($"Attempting to add new firewall rule for server {server.ID}");

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                return Windows.PortManager.AddFirewallRule(server);
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                return Linux.PortManager.AddFirewallRule(server);
            }
            else
            {
                throw new ApplicationException("Port Manager not valid on operating system.");
            }
        }

        public static bool RemoveFirewallRule(Server server)
        {
            Log.Verbose($"Attempting to remove firewall rule for server {server.ID}");

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                return Windows.PortManager.RemoveFirewallRule(server);
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                return Linux.PortManager.RemoveFirewallRule(server);
            }
            else
            {
                throw new ApplicationException("Port Manager not valid on operating system.");
            }
        }

        public static IPerformanceMonitor GetPerformanceMonitor(int processId)
        {
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                return new Windows.PerformanceMonitor(processId);
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                return new Linux.PerformanceMonitor(processId);
            }
            else
            {
                throw new ApplicationException("Performance Counter not valid on operating system.");
            }
        }
    }
}
