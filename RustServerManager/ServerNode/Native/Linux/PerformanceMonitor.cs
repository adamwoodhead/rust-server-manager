using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                while (!this.Token.IsCancellationRequested)
                {

                    await Task.Delay(this.Tickrate);
                }
            });
        }
    }
}
