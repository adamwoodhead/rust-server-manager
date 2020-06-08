using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ServerNode.Native.Linux
{
    internal static class Screens
    {
        public static bool HasScreenAccess()
        {
            string screenName = "ServerNodeTest_" + Guid.NewGuid().ToString();
            string shellScript = @"-c ""pid=$(screen -S " + screenName + @" -dm sh; screen -ls | awk '/\." + screenName + @"\t/ { print($1) }'); echo $pid;""";

            string[] output = SH.Shell(Program.WorkingDirectory, shellScript);

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

        public static void CleanUpScreens()
        {
            SH.Shell(Program.WorkingDirectory, @"-c ""screen -wipe;""");
        }
    }
}
