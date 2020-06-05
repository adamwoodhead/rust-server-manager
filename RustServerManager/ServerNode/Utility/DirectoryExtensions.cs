using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Utility
{
    internal static class DirectoryExtensions
    {
        /// <summary>
        /// Delete a directory recursively and wait for it to not exist, or timeout
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="timeoutms"></param>
        /// <exception cref="ArgumentException"/>
        /// <returns></returns>
        internal static bool DeleteOrTimeout(this DirectoryInfo directory, int timeoutms = 5000)
        {
            if (directory.Exists)
            {

                directory.Delete(true);

                if (timeoutms > 0)
                {
                    Task waitTask = Task.Run(async () =>
                    {
                        // wait for the directory to not exist
                        while (Directory.Exists(directory.FullName))
                        {
                            await Task.Delay(2);
                        }
                    });

                    if (Task.WhenAny(waitTask, Task.Delay(timeoutms)).Result == waitTask)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    throw new ArgumentException("timeoutms cannot be equal to or less than zero");
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Delete a directory recursively and wait for it to not exist, or timeout
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="timeoutms"></param>
        /// <exception cref="ArgumentException"/>
        /// <returns></returns>
        internal static bool DeleteOrTimeout(string directory, int timeoutms = 5000)
        {
            return DeleteOrTimeout(new DirectoryInfo(directory), timeoutms);
        }

        internal static long GetDirectorySize(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            long bytes = 0;

            foreach (string name in files)
            {
                FileInfo info = new FileInfo(name);
                bytes += info.Length;
            }

            return bytes;
        }
    }
}
