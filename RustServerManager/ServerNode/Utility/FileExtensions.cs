using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
        public static bool DeleteOrTimeout(this FileInfo file, int timeoutms = 5000)
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

        public static bool DeleteOrTimeoutFilteredFilesInDirectory(string directory, params string[] filters)
        {
            DirectoryInfo dir = new DirectoryInfo(directory);
            // get list of files matching filters
            IEnumerable<string> files = filters.SelectMany(dir.EnumerateFiles).Select(x => x.FullName);

            // for every file
            foreach (string file in files)
            {
                // lets double check that the file path is correct
                if (File.Exists(file))
                {
                    // get the fileinfo of the path
                    FileInfo fileInfo = new FileInfo(file);
                    // delete and wait for non-existence, or timeout.
                    // if the file has been deleted successfully
                    if (DeleteOrTimeout(fileInfo))
                    {
                        Log.Verbose($"File Deleted ({fileInfo.Name})");
                    }
                    // if the file still exists after the deletion timeout
                    else
                    {
                        Log.Error($"File Not Deleted ({fileInfo.Name})");
                    }
                }
            }

            return files.All(x => !File.Exists(x));
        }
    }
}
