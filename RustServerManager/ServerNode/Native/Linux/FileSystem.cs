using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Native.Linux
{
    internal static class FileSystem
    {
        public static void Chmod(string perms, string path, bool recursive = false)
        {
            Linux.SH.Shell(Program.WorkingDirectory, $"-c \"chmod{(recursive ? " -R" : "")} {perms} {path}\"");
        }

        public static void Chown(string user, string directory, bool recursive = false)
        {
            Linux.SH.Shell(Program.WorkingDirectory, $"-c \"chown{(recursive ? " -R" : "")} {user} {directory}\"");
        }
    }
}
