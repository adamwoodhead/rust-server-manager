﻿using ServerNode.Models.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Interfaces
{
    public interface IPerformanceMonitor
    {
        double UsageCPU { get; set; }

        double UsageMem { get; set; }

        double UsageDiskW { get; set; }

        double UsageDiskR { get; set; }

        void BeginMonitoring(Server server);

        void StopMonitoring(Server server);
    }
}
