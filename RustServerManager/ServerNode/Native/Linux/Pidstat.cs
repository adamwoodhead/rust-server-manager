using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Native.Linux
{
    internal static class Pidstat
    {
        public static bool IsPidstatAvailable()
        {
            string[] output = SH.Shell(Program.WorkingDirectory, @"-c pidstat", null, null);

            if (output.Any(x => x.Contains("not found")))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
