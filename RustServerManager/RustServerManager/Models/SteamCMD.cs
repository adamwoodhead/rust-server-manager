using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RustServerManager.Models
{
    internal static class SteamCMD
    {
        private static Process SteamCMDProcess(string arguments = "")
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = App.Memory.Configuration.SteamCMDExecutable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            return process;
        }

        internal static async Task FirstLaunch()
        {
            string args = $"-condebug +quit";

            Process steam = SteamCMDProcess(args);
            steam.Start();

            await Task.Run(() => { steam.WaitForExit(); });
        }

        internal static async Task DownloadRust()
        {
            string args = $"-condebug +login anonymous +force_install_dir \"{App.Memory.Configuration.ActualServerDirectory}\" +app_update 258550 validate +quit";

            Process steam = SteamCMDProcess(args);
            steam.Start();

            await Task.Run(() => { steam.WaitForExit(); });
        }
    }
}