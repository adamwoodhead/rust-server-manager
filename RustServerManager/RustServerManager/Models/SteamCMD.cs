using System;
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
        private static IProgress<string> _progress;

        private static void SteamCMDProcess(string arguments = "")
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = App.Memory.Configuration.SteamCMDExecutable,
                Arguments = arguments,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            Process process = Process.Start(processStartInfo);
            ConsoleAutomator automator = new ConsoleAutomator(process.StandardOutput);

            // AutomatorStandardInputRead is your event handler
            automator.StandardInputRead += Automator_StandardInputRead;
            automator.StartAutomate();

            // do whatever you want while that process is running
            process.WaitForExit();
            automator.StandardInputRead -= Automator_StandardInputRead;
            process.Close();
        }

        private static void Automator_StandardInputRead(object sender, ConsoleInputReadEventArgs e)
        {
            _progress.Report(e.Input);
            Console.WriteLine(e.Input);
        }

        internal static void DownloadRust(IProgress<string> progress = null)
        {
            _progress = progress;

            string args = $"+login anonymous +force_install_dir \"{App.Memory.Configuration.RustServerDirectory}\\RustDedicated\\\" +app_update 258550 validate +quit";

            SteamCMDProcess(args);
        }

        internal static void FirstLaunch(IProgress<string> progress = null)
        {
            _progress = progress;

            string args = $"+quit";

            SteamCMDProcess(args);
        }
    }

    public class ConsoleInputReadEventArgs : EventArgs
    {
        public ConsoleInputReadEventArgs(string input)
        {
            this.Input = input;
        }

        public string Input { get; private set; }
    }

    public interface IConsoleAutomator
    {
        StreamWriter StandardInput { get; }

        event EventHandler<ConsoleInputReadEventArgs> StandardInputRead;
    }

    public abstract class ConsoleAutomatorBase : IConsoleAutomator
    {
        protected readonly StringBuilder inputAccumulator = new StringBuilder();

        protected readonly byte[] buffer = new byte[256];

        protected volatile bool stopAutomation;

        public StreamWriter StandardInput { get; protected set; }

        protected StreamReader StandardOutput { get; set; }

        protected StreamReader StandardError { get; set; }

        public event EventHandler<ConsoleInputReadEventArgs> StandardInputRead;

        protected void BeginReadAsync()
        {
            if (!this.stopAutomation)
            {
                this.StandardOutput.BaseStream.BeginRead(this.buffer, 0, this.buffer.Length, this.ReadHappened, null);
            }
        }

        protected virtual void OnAutomationStopped()
        {
            this.stopAutomation = true;
            this.StandardOutput.DiscardBufferedData();
        }

        private void ReadHappened(IAsyncResult asyncResult)
        {
            var bytesRead = this.StandardOutput.BaseStream.EndRead(asyncResult);
            if (bytesRead == 0)
            {
                this.OnAutomationStopped();
                return;
            }

            var input = this.StandardOutput.CurrentEncoding.GetString(this.buffer, 0, bytesRead);
            this.inputAccumulator.Append(input);

            if (bytesRead < this.buffer.Length)
            {
                this.OnInputRead(this.inputAccumulator.ToString());
            }

            this.BeginReadAsync();
        }

        private void OnInputRead(string input)
        {
            var handler = this.StandardInputRead;
            if (handler == null)
            {
                return;
            }

            handler(this, new ConsoleInputReadEventArgs(input));
            this.inputAccumulator.Clear();
        }
    }

    public class ConsoleAutomator : ConsoleAutomatorBase, IConsoleAutomator
    {
        public ConsoleAutomator(StreamReader standardOutput)
        {
            this.StandardOutput = standardOutput;
        }

        public void StartAutomate()
        {
            this.stopAutomation = false;
            this.BeginReadAsync();
        }

        public void StopAutomation()
        {
            this.OnAutomationStopped();
        }
    }
}
