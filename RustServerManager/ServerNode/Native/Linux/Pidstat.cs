using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Native.Linux
{
    internal static class Pidstat
    {
        internal static bool IsPidstatAvailable()
        {
            string[] output = SH.Shell(Program.WorkingDirectory, @"-c pidstat", null, null, true);

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
