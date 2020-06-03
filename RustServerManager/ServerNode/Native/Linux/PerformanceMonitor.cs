﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Native.Linux
{
    internal class PerformanceMonitor : ServerNode.Native.PerformanceMonitor
    {
        public PerformanceMonitor(Process p, CancellationToken token, int tick = 5) : base(p, token, tick)
        {
        }

        internal override void BeginMonitoring()
        {
            throw new NotImplementedException();
        }
    }
}
