using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ServerNode.Native.Linux
{
    internal static class SH
    {
        public static string[] Shell(string workingDir, string script, DataReceivedEventHandler dataReceivedEvent = null, DataReceivedEventHandler errorReceivedEvent = null, bool disableDebugLogs = false)
        {
            Process starter = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = workingDir,
                    FileName = "sh",
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

            if (dataReceivedEvent != null)
            {
                starter.OutputDataReceived += dataReceivedEvent;
            }
            else
            {
                starter.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.Add(e.Data);

                        if (!disableDebugLogs)
                        {
                            Log.Debug($"sh Out: \"{e.Data}\"");
                        }
                    }
                };
            }

            if (errorReceivedEvent != null)
            {
                starter.ErrorDataReceived += errorReceivedEvent;
            }
            else
            {
                starter.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.Add(e.Data);

                        if (!disableDebugLogs)
                        {
                            Log.Debug($"sh Err: \"{e.Data}\"");
                        }
                    }
                };
            }

            starter.Start();

            // asyncronously read error and output
            starter.BeginOutputReadLine();
            starter.BeginErrorReadLine();

            if (!disableDebugLogs)
            {
                Log.Verbose($"Waiting for sh responses");
            }

            starter.WaitForExit();

            return output.ToArray();
        }
    }
}
