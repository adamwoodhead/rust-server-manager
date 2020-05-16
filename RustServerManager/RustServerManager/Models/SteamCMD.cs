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
        private static string SteamCMDURL { get => @"https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"; }

        private static string SteamCMDFolder { get => Path.Combine(App.Memory.Configuration.WorkingDirectory, "SteamCMD"); }

        private static string SteamCMDEXecutable { get => Path.Combine(SteamCMDFolder, "steamcmd.exe"); }

        private static Process SteamCMDProcess(string arguments = "")
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = SteamCMDEXecutable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_OutputDataReceived;

            return process;
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static async Task FirstLaunch()
        {
            string args = $"+quit";
            Process steam = SteamCMDProcess(args);
            steam.Start();
            steam.BeginOutputReadLine();
            steam.BeginErrorReadLine();

            await Task.Run(() => { steam.WaitForExit(); });
        }

        internal static async Task DownloadRust(string directory)
        {
            await Install();

            string args = $"+login anonymous +force_install_dir \"{directory}\" +app_update 258550 validate +quit";

            Process steam = SteamCMDProcess(args);
            steam.Start();
            steam.BeginOutputReadLine();
            steam.BeginErrorReadLine();

            await Task.Run(() => { steam.WaitForExit(); });
        }

        internal static bool Exists()
        {
            if (Directory.Exists(SteamCMDFolder) && File.Exists(SteamCMDEXecutable))
            {
                return true;
            }

            return false;
        }

        internal static async Task Install()
        {
            if (!Exists())
            {
                string steamCMDZip = Path.Combine(SteamCMDFolder, "steamcmd.zip");

                using (var client = new WebClient())
                {
                    client.DownloadFile(SteamCMDURL, steamCMDZip);
                }

                ZipFile.ExtractToDirectory(steamCMDZip, SteamCMDFolder);

                File.Delete(steamCMDZip);

                await FirstLaunch();
            }
        }
    }
}