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
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true,
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

            await Task.Run(async() => {
                while (!steam.HasExited)
                {
                    await steam.StandardOutput.BaseStream.FlushAsync();
                    await Task.Delay(1000);
                }
            });

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

        private static string TestWaitForLines(this Process steam, int timeout = 0, params string[] lines)
        {
            DateTime timeoutTime = DateTime.Now;

            if (timeout > 0)
            {
                timeoutTime = DateTime.Now.AddSeconds(timeout);
            }

            Task<string> wait = new Task<string>(() => {
                while (steam != null && !(steam?.HasExited ?? true))
                {
                    try
                    {
                        string output;
                        if (!string.IsNullOrEmpty(output = steam.StandardOutput.ReadLine()))
                        {
                            Console.WriteLine(output);
                            if (lines.ToList().Exists(x => output.Contains(x)))
                            {
                                Console.WriteLine($"SteamCMD found awaited line => {output}");
                                return output;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

                Console.WriteLine("Wait Loop Exited.");

                return null;
            });

            wait.Start();

            if (timeout > 0)
            {
                while (DateTime.Now >= timeoutTime)
                {
                    if (DateTime.Now <= timeoutTime)
                    {
                        Console.WriteLine($"SteamCMD ReadLine Timeout ({timeout}s) - Maybe we received something unexpected.");
                        return null;
                    }
                }
            }

            wait.Wait();

            return wait.Result;

            throw new Exception("SteamCMD has unexpectedly closed!");
        }

        internal static async Task<bool> TestDownloadRust(Gameserver gameserver, IProgress<double> progress = null)
        {
            //await CheckExists();
            
            Process steam = TestSteamCMDProcess();
            steam.Start();

            StreamWriter steamInput = steam.StandardInput;
            steam.StandardInput.AutoFlush = true;

            steam.TestWaitForLines(0, "Loading Steam API...OK.");

            steamInput.WriteLine($"login anonymous");

            await Task.Delay(2000);

            steamInput.WriteLine($"force_install_dir \"{gameserver.WorkingDirectory}\"");

            await Task.Delay(2000);

            steamInput.WriteLine($@"app_update 258550 validate");

            bool end = false;

            while (!end)
            {
                string line = steam.TestWaitForLines(0, "ERROR", "Success!", "downloading", "validating");

                if (line.Contains("ERROR"))
                {
                    end = true;
                    Console.WriteLine("SteamCMD App Update Error");
                    steam.Kill();
                    return false;
                }
                else if (line.Contains("Success"))
                {
                    end = true;
                    Console.WriteLine("Successful Install");
                    steamInput.WriteLine("quit");
                    return true;
                }
                else if (line.Contains("downloading"))
                {
                    Regex regex = new Regex(@"(\d{1,2}\.\d{1,2}\w)");
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        progress?.Report(Convert.ToDouble(match.Value));
                    }
                }
                else if (line.Contains("validating"))
                {
                    Regex regex = new Regex(@"(\d{1,2}\.\d{1,2}\w)");
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        progress?.Report(Convert.ToDouble(match.Value));
                    }
                }
            }

            Console.WriteLine("While Loop Ended");

            return true;
        }

        private static Process TestSteamCMDProcess()
        {
            Process process = new Process();
            process.StartInfo.FileName = SteamCMDEXecutable;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;

            process.EnableRaisingEvents = true;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;

            Console.WriteLine("Steam CMD initializing");

            return process;
        }
    }
}