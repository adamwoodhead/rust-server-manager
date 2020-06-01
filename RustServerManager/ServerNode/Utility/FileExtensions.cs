using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Utility
{
    internal static class FileExtensions
    {
        /// <summary>
        /// Delete a file and wait for it to not exist, or timeout
        /// </summary>
        /// <param name="file"></param>
        /// <param name="timeoutms"></param>
        /// <exception cref="ArgumentException"/>
        /// <returns></returns>
        internal static bool DeleteOrTimeout(this FileInfo file, int timeoutms = 5000)
        {
            if (file.Exists)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return false;
                }

                if (timeoutms > 0)
                {
                    Task waitTask = Task.Run(async () =>
                    {
                        // wait for the directory to not exist
                        while (File.Exists(file.FullName))
                        {
                            await Task.Delay(2);
                        }
                    });

                    if (Task.WhenAny(waitTask, Task.Run(async () => { await Task.Delay(timeoutms); })) == waitTask)
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
    }
}
