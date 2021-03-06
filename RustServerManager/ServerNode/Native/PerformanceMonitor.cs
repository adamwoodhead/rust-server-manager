﻿using ServerNode.Interfaces;
using ServerNode.Logging;
using ServerNode.Models.Servers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Native
{
    public abstract class PerformanceMonitor : IPerformanceMonitor
    {
        protected CancellationTokenSource TokenSource { get; private set; }

        protected CancellationToken Token { get; private set; }

        /// <summary>
        /// Millisecond delay per performance count
        /// </summary>
        protected int Tickrate { get; set; }

        /// <summary>
        /// Process ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Process successfully caputured - ready for monitoring
        /// </summary>
        public bool IsInitialised { get; private set; } = false;

        /// <summary>
        /// Average CPU usage
        /// </summary>
        public double UsageCPU { get; set; }

        /// <summary>
        /// Working memory set
        /// </summary>
        public double UsageMem { get; set; }

        /// <summary>
        /// Disk writes per second (bytes)
        /// </summary>
        public double UsageDiskW { get; set; }

        /// <summary>
        /// Disk reads per second (bytes)
        /// </summary>
        public double UsageDiskR { get; set; }

        /// <summary>
        /// Begin monitoring the system usage of a process, by id
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="tokenSource"></param>
        /// <param name="tick"></param>
        public PerformanceMonitor(int pid, int tick = 5)
        {
            ProcessId = pid;
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
            Tickrate = tick * 1000;

            try
            {
                Process.GetProcessById(pid).Exited += delegate { TokenSource.Cancel(); };
                IsInitialised = true;
            }
            catch (Exception)
            {
                IsInitialised = false;
            }
        }

        /// <summary>
        /// Begin the monitoring loop
        /// </summary>
        /// <param name="serverID"></param>
        public abstract void BeginMonitoring(Server server);

        /// <summary>
        /// Signal to stop monitoring
        /// </summary>
        public void StopMonitoring(Server server)
        {
            if (Token.CanBeCanceled)
            {
                TokenSource.Cancel();
                Log.Verbose($"Server {server.ID:00} Performance Monitor cancelled quietly.");
            }
        }
    }
}
