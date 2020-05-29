using ServerNode.Models.Steam;
using ServerNode.Models.Terminal;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode
{
    class Program
    {
        internal static string WorkingDirectory { get; private set; }

        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private static void Main(string[] args)
        {
            Console.WriteLine("Server Node Booting Up");

            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // Create Directories
                WorkingDirectory = @"C:\ServerNode";
                Directory.CreateDirectory(WorkingDirectory);

                // check if steam cmd is installed
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                // Create Directories
                WorkingDirectory = @"/opt/servernode";
                Directory.CreateDirectory(WorkingDirectory);
            }
            else
            {
                throw new Exception("This operating system is not currently supported...");
            }

            SteamApp.Create("Counter Strike: Source",       "css",      "srcds.exe",            232330);
            SteamApp.Create("Rust",                         "rust",     "RustDedicated.exe",    258550);

            SteamCMD.EnsureAvailable();

            // TESTING ONLY
            Task.Run(async () =>
            {
                try
                {
                    using (SteamCMD steam = (SteamCMD)await Terminal.Instantiate<SteamCMD>(new TerminalStartUpOptions("SteamCMD Terminal", 10000)))
                    {
                        steam.Finished += delegate { Console.WriteLine($"SteamCMD Finished {(steam.AppInstallationSuccess ? "Successfully" : "Unexpectedly (check for errors)")}"); };
                        steam.StateChanged += delegate { Console.WriteLine(steam.State); };
                        steam.ProgressChanged += delegate {
                            if (steam.State == SteamCMDState.APP_DOWNLOADING)
                            {
                                if (steam.Progress != 0)
                                {
                                    Console.WriteLine($"Download Speed {Utility.ByteMeasurements.BytesToMB(steam.AverageDownloadSpeed).ToString("000.00")}Mb/s");
                                    Console.WriteLine($"Estimated Time Left: {(int)steam.EstimatedDownloadTimeLeft.TotalSeconds}s");
                                }
                            }
                        };

                        if (await steam.LoginAnonymously())
                        {

                            if (Utility.OperatingSystemHelper.IsWindows())
                            {
                                await steam.ForceInstallDirectory($"force_install_dir \"{@"C:\ServerNode\gameservers\1"}\"");
                            }
                            else
                            {
                                await steam.ForceInstallDirectory($"force_install_dir \"{@"/home/adam/gameservers/1"}\"");
                            }

                            await steam.AppUpdate(SteamApp.Apps[0].SteamID, true);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
                
            });

            // asyncronously ping for requests
            // TODO get requests

            _quitEvent.WaitOne();

            Console.WriteLine("Server Node Shutting Down");

            // cleanup/shutdown and quit
        }
    }
}
