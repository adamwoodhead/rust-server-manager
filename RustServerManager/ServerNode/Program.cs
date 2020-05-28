using ServerNode.Models.Steam;
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
                WorkingDirectory = @"/ServerNode";
                Directory.CreateDirectory(WorkingDirectory);

                // A requirement for steamcmd on ubuntu/debian is lib32gcc1 (sudo apt-get install lib32gcc1)
                // This should be provided as an installation instruction
            }
            else
            {
                throw new Exception("This operating system is not currently supported...");
            }

            SteamApp.Create("Counter Strike: Source",       "css",      "srcds.exe",            232330);
            SteamApp.Create("Rust",                         "rust",     "RustDedicated.exe",    258550);

            //Lets test some functionality...

            //SteamDB ID for Rust: 258550

            //SteamCMD steamCMD = new SteamCMD();

            //steamCMD.StateChanged += (s, e) => { Console.WriteLine($"SteamCMD State Change: {(e as StateChangedEventArgs).State}"); };
            //steamCMD.ProgressChanged += (s, e) => { Console.WriteLine($"SteamCMD Progress Change: {(e as ProgressChangedEventArgs).Progress}"); };
            //steamCMD.Finished += delegate { Console.WriteLine("SteamCMD Finished!"); };

            //if (Utility.OperatingSystemHelper.IsWindows())
            //{
            //    steamCMD.InstallAnonymousApp(@"C:\rsm\1", 258550, true);
            //}
            //else if (Utility.OperatingSystemHelper.IsLinux())
            //{
            //    steamCMD.InstallAnonymousApp(@"/home/adam/1", 258550, true);
            //}
            SteamCMD.EnsureAvailable();

            // TESTING ONLY
            Task.Run(async () =>
            {
                using (SteamCMD steamCMD = await SteamCMD.Instantiate(10000))
                {
                    steamCMD.StateChanged += delegate { Console.WriteLine(steamCMD.State); };
                    steamCMD.ProgressChanged += delegate {
                        if (steamCMD.State == SteamCMDState.APP_DOWNLOADING)
                        {
                            if (steamCMD.Progress != 0)
                            {
                                Console.WriteLine($"Download Speed {Utility.ByteMeasurements.BytesToMB(steamCMD.AverageDownloadSpeed).ToString("000.00")}Mb/s");
                                Console.WriteLine($"Estimated Time Left: {(int)steamCMD.EstimatedDownloadTimeLeft.TotalSeconds}s");
                            }
                        }
                    };

                    if (await steamCMD.LoginAnonymously())
                    {

                        if (Utility.OperatingSystemHelper.IsWindows())
                        {
                            await steamCMD.ForceInstallDirectory($"force_install_dir \"{@"C:\gameservers\1"}\"");
                        }
                        else
                        {
                            await steamCMD.ForceInstallDirectory($"force_install_dir \"{@"/home/adam/gameservers/1"}\"");
                        }

                        await steamCMD.AppUpdate(SteamApp.Apps[0].SteamID, true);
                    }

                    steamCMD.Finished += delegate { Console.WriteLine($"SteamCMD Finished {(steamCMD.AppInstallationSuccess ? "Successfully" : "With Errors")}"); };

                }
            });

            // asyncronously ping for requests
            // TODO get requests

            _quitEvent.WaitOne();

            // cleanup/shutdown and quit
        }
    }
}
