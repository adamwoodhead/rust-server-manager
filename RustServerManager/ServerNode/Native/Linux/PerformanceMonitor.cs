using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Native.Linux
{
    internal class PerformanceMonitor : ServerNode.Native.PerformanceMonitor
    {
        public PerformanceMonitor(int pid, int tick = 5) : base(pid, tick)
        {
        }

        /// <summary>
        /// Begin monitoring the system usage of a process, by id
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="tokenSource"></param>
        /// <param name="tick"></param>
        public override void BeginMonitoring(int serverID)
        {
            Task.Run(async() => {
                if (!IsInitialised)
                {
                    Log.Warning("Attempted to begin monitoring a process, but it never initialised!");
                    return;
                }

                Log.Warning($"Began Monitoring Process {this.ProcessId}.");

                // We need to get the real pid, we currently have the screen pids...
                string[] pstree = SH.Shell(Program.WorkingDirectory, @"-c ""pstree " + ProcessId + @" -p""");
                string firstLine = pstree.FirstOrDefault();
                List<int> pids = new List<int>();
                string tmpPID = "";
                foreach (char ch in firstLine)
                {
                    if (Utility.StringExtension.IsRealDigitOnly(ch))
                    {
                        tmpPID += ch;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(tmpPID) && tmpPID.Length != 1)
                        {
                            pids.Add(Convert.ToInt32(tmpPID));
                        }

                        tmpPID = "";
                    }
                }

                while (!this.Token.IsCancellationRequested)
                {
                    await Task.Delay(this.Tickrate);

                    string[] pidstat = SH.Shell(Program.WorkingDirectory, @"-c ""pidstat -p " + string.Join(',', pids) + @"""");
                    string[] pidstat_d = SH.Shell(Program.WorkingDirectory, @"-c ""pidstat -d -p " + string.Join(',', pids) + @"""");

                    // TODO Combine all the data into singular values, and provide that as the general performance statistic for this process

                    Log.Verbose($"Server {serverID} Performance - CPU: {UsageCPU:0.00}%, " +
                                $"Mem: {ServerNode.Utility.ByteMeasurements.BytesToMB(UsageMem):0.00}MB, " +
                                $"Disk Write: {ServerNode.Utility.ByteMeasurements.BytesToMB(UsageDiskW):0.00}MB/s, " +
                                $"Disk Read: {ServerNode.Utility.ByteMeasurements.BytesToMB(UsageDiskR):0.00}MB/s");
                }
            });
        }
    }
}
