using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ServerNode.Native.Windows
{
    internal static class Powershell
    {
        public static string[] Shell(string workingDir, string script)
        {
            Process starter = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = workingDir,
                    FileName = "powershell",
                    Arguments = script,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            List<string> output = new List<string>();
            starter.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Add(e.Data); Log.Debug($"Powershell Out: \"{e.Data}\"");
                }
            };

            starter.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Add(e.Data); Log.Verbose($"Powershell Error: \"{e.Data}\"");
                }
            };

            starter.Start();

            // asyncronously read error and output
            starter.BeginOutputReadLine();
            starter.BeginErrorReadLine();

            Log.Verbose($"Waiting for Powershell responses");

            starter.WaitForExit();

            return output.ToArray();
        }
    }
}
