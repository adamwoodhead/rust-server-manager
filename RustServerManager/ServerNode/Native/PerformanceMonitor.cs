using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Native
{
    internal abstract class PerformanceMonitor
    {
        protected CancellationToken Token;

        protected int Tickrate { get; set; }

        internal Process Process { get; set; }

        internal double UsageCPU { get; set; }

        internal double UsageMem { get; set; }

        internal double UsageDisk { get; set; }

        internal PerformanceMonitor(Process p, CancellationToken token, int tick = 5)
        {
            Process = p;
            Token = token;
            Tickrate = tick * 1000;
        }

        internal abstract void BeginMonitoring();
    }
}
