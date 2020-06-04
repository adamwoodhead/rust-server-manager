using ServerNode.Logging;
using ServerNode.Models.Servers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ServerNode.Native.Windows
{
    internal class PerformanceMonitor : ServerNode.Native.PerformanceMonitor
    {
        struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        public PerformanceMonitor(int pid, int tick = 5) : base(pid, tick)
        {

        }

        [DllImport(@"kernel32.dll", SetLastError = true)]
        static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS counters);

        /// <summary>
        /// Begin monitoring the system usage of a process, by id
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="tokenSource"></param>
        /// <param name="tick"></param>
        public override void BeginMonitoring(Server server)
        {
            Task.Run(async () => {
                if (!IsInitialised)
                {
                    Log.Warning("Attempted to begin monitoring a process, but it never initialised!");
                    return;
                }

                Log.Verbose($"Began Monitoring Process {this.ProcessId}.");

                string instance = GetProcessInstanceName(ProcessId);

                try
                {
                    PerformanceCounter ramCounter = new PerformanceCounter("Process", "Working Set", instance);
                    PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", instance);

                    ulong lastWriteBytes = 0;
                    ulong lastReadBytes = 0;

                    Stopwatch stopwatch = new Stopwatch();

                    while (!this.Token.IsCancellationRequested)
                    {
                        await Task.Delay(this.Tickrate);

                        this.Token.ThrowIfCancellationRequested();

                        // our process can die at any time, so this could fail!
                        try
                        {
                            UsageCPU = cpuCounter.NextValue();
                            UsageMem = ramCounter.NextValue();
                            GetProcessIoCounters(Process.GetProcessById(ProcessId).Handle, out IO_COUNTERS counters);
                            

                            if (lastWriteBytes != 0 && lastReadBytes != 0)
                            {
                                stopwatch.Stop();
                                UsageDiskW = (counters.WriteTransferCount - lastWriteBytes) / stopwatch.Elapsed.TotalSeconds;
                                UsageDiskR = (counters.ReadTransferCount - lastReadBytes) / stopwatch.Elapsed.TotalSeconds;
                                stopwatch.Reset();
                                stopwatch.Start();
                            }

                            lastWriteBytes = counters.WriteTransferCount;
                            lastReadBytes = counters.ReadTransferCount;

                            Log.Verbose($"Server {server.ID} ({server.App.ShortName,10}) Performance - CPU: {UsageCPU:0.00}%, " +
                                $"Mem: {ServerNode.Utility.ByteMeasurements.BytesToMB(UsageMem):0.00}MB, " +
                                $"Disk Write: {ServerNode.Utility.ByteMeasurements.BytesToMB(UsageDiskW):0.00}MB/s, " +
                                $"Disk Read: {ServerNode.Utility.ByteMeasurements.BytesToMB(UsageDiskR):0.00}MB/s");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw;
                }                
            }, Token);
        }

        private static string GetProcessInstanceName(int pid)
        {
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");

            string[] instances = cat.GetInstanceNames();
            foreach (string instance in instances)
            {

                using (PerformanceCounter cnt = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    int val = (int)cnt.RawValue;
                    if (val == pid)
                    {
                        return instance;
                    }
                }
            }
            throw new Exception("Could not find performance counter instance name for current process. This is truly strange ...");
        }
    }
}
