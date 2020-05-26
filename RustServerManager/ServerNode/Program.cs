using ServerNode.Models.Steam;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ServerNode
{
    class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private static void Main(string[] args)
        {
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // Create Directories
                
                // check if steam cmd is installed
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                // Create Directories

                // A requirement for steamcmd on ubuntu/debian is lib32gcc1 (sudo apt-get install lib32gcc1)
                // This should be provided as an installation instruction

                // check if steam cmd is installed
                if (!File.Exists("/usr/games/steamcmd"))
                {
                    Console.WriteLine("SteamCMD is not installed! Please follow the installation instructions!");
                }
            }
            else
            {
                throw new Exception("This operating system is not currently supported...");
            }


            //Lets test some functionality...

            //SteamDB ID for Rust: 258550

            SteamCMD steamCMD = new SteamCMD();

            steamCMD.StateChanged += (s, e) => { Console.WriteLine($"SteamCMD State Change: {(e as StateChangedEventArgs).State}"); };
            steamCMD.ProgressChanged += (s, e) => { Console.WriteLine($"SteamCMD Progress Change: {(e as ProgressChangedEventArgs).Progress}"); };
            steamCMD.Finished += delegate { Console.WriteLine("SteamCMD Finished!"); };

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                steamCMD.InstallAnonymousApp(@"C:\rsm\1", 258550, true);
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                steamCMD.InstallAnonymousApp(@"/home/adam/1", 258550, true);
            }

            Console.Read();
        }
    }
}
