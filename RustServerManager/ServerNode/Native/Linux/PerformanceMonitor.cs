using ServerNode.Logging;
using ServerNode.Models.Servers;
using ServerNode.Utility;
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
        public override void BeginMonitoring(Server server)
        {
            Task.Run(async() => {
                if (!IsInitialised)
                {
                    Log.Warning("Attempted to begin monitoring a process, but it never initialised!");
                    return;
                }

                Log.Informational($"Began Monitoring Process {this.ProcessId}.");

                try
                {
                    // We need to get the real pid, we currently have the screen pids...
                    IEnumerable<int> pids = Pstree.ProcessTree(ProcessId).SkipLast(1);

                    string[] pidstat_headers = null;

                    while (!this.Token.IsCancellationRequested)
                    {
                        await Task.Delay(this.Tickrate);

                        Token.ThrowIfCancellationRequested();

                        try
                        {
                            // refresh the pids we're watcing, child process can pop up and close at any time
                            pids = Pstree.ProcessTree(ProcessId).SkipLast(1);

                            // get cpu stats, memory stats & disk stats
                            string[] pidstat_values = SH.Shell(Program.WorkingDirectory, @"-c ""pidstat -urdh -p " + string.Join(',', pids) + @"""", null, null).Skip(1).ToArray();

                            if (pidstat_values.Length <= 2)
                            {
                                // we have headers... but no longer have any data to parse.

                                // Linux 4.19.104 - microsoft - standard(LAPTOP - ADAM)         06 / 04 / 2020      _x86_64_(8 CPU)
                                // #      Time   UID       PID    %usr %system  %guest    %CPU   CPU  minflt/s  majflt/s     VSZ     RSS   %MEM   kB_rd/s   kB_wr/s kB_ccwr/s iodelay  Command
                                return;
                            }

                            // headers should only need to be handled once...
                            // #      Time   UID       PID    %usr %system  %guest    %CPU   CPU  minflt/s  majflt/s     VSZ     RSS   %MEM   kB_rd/s   kB_wr/s kB_ccwr/s iodelay  Command

                            if (pidstat_headers == null)
                            {
                                string header_row = pidstat_values[0];
                                // remove stupid #
                                header_row = header_row.Replace("#", "");

                                pidstat_headers = header_row.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                            }

                            double UsageCPU = 0;
                            double UsageMem = 0;
                            double UsageDiskW = 0;
                            double UsageDiskR = 0;

                            // data rows.. skipping the headers..
                            foreach (string row in pidstat_values.Skip(1))
                            {
                                string[] cols = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                                UsageCPU += Convert.ToDouble(cols[Array.IndexOf(pidstat_headers, "%CPU")]);
                                UsageMem += Convert.ToDouble(cols[Array.IndexOf(pidstat_headers, "RSS")]);
                                UsageDiskW += Convert.ToDouble(cols[Array.IndexOf(pidstat_headers, "kB_wr/s")]);
                                UsageDiskR += Convert.ToDouble(cols[Array.IndexOf(pidstat_headers, "kB_rd/s")]);
                            }

                            int padding = PreAPIHelper.Apps.Values.Max(x => x.ShortName.Length);

                            Log.Verbose($"Server {server.ID:00} ({server.App.ShortName.PadLeft(padding)}) Performance - CPU: {UsageCPU:000.00}%" +
                                        $"     Mem: {ServerNode.Utility.ByteMeasurements.KiloBytesToMB(UsageMem):00000.00}MB" +
                                        $"     Disk Write: {ServerNode.Utility.ByteMeasurements.KiloBytesToMB(UsageDiskW):000.00}MB/s" +
                                        $"     Disk Read: {ServerNode.Utility.ByteMeasurements.KiloBytesToMB(UsageDiskR):000.00}MB/s");
                        }
                        catch (NullReferenceException)
                        {
                            Log.Warning("Looks like our server stopped, the performance monitor just broke out.");
                            if (Token.CanBeCanceled)
                            {
                                TokenSource.Cancel();
                            }
                            return;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw;
                }
            });
        }
    }
}
