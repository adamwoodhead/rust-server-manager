using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ServerNode.Native
{
    internal static class Linux
    {
        internal static bool HasScreenAccess()
        {
            string screenName = "ServerNodeTest_" + Guid.NewGuid().ToString();
            string shell = "sh";
            string shellScript = @"-c ""pid=$(screen -S " + screenName + @" -dm sh; screen -ls | awk '/\." + screenName + @"\t/ { print($1) }'); echo $pid;""";

            Process starter = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = Program.WorkingDirectory,
                    FileName = shell,
                    Arguments = shellScript,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            List<string> output = new List<string>();
            List<string> errors = new List<string>();
            starter.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Verbose($"{shell} write: \"{e.Data}\"");
                    output.Add(e.Data);
                }
            };

            starter.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Error($"{shell} error: \"{e.Data}\"");
                    errors.Add(e.Data);
                }
            };

            starter.Start();

            starter.BeginOutputReadLine();
            starter.BeginErrorReadLine();

            starter.WaitForExit();

            foreach (string line in output)
            {
                if (line.Contains(screenName))
                {
                    Log.Verbose("Test Screen Name Captured");

                    string spid = line.Split('.').FirstOrDefault();

                    int pid = Convert.ToInt32(spid);

                    try
                    {
                        Process.GetProcessById(pid).Kill();

                        CleanUpScreens();

                        return true;
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void CleanUpScreens()
        {
            string shell = "sh";
            string shellScript = @"-c ""screen -wipe;""";

            Process starter = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = Program.WorkingDirectory,
                    FileName = shell,
                    Arguments = shellScript,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                },
            };

            starter.Start();

            starter.WaitForExit();
        }
    }
}
