using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Native
{
    internal static class Native
    {
        /// <summary>
        /// Perform a native shell execution in the stated working directory, returns shell script output.
        /// Script will have to be predefined for the native system.
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        internal static string[] Shell(string workingDir, string script)
        {
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                return Windows.Powershell.Shell(workingDir, script);
            }
            else
            {
                return Linux.SH.Shell(workingDir, script);
            }
        }
    }
}
