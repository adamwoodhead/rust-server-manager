using System;
using System.Runtime.InteropServices;

namespace ServerNode
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (Utility.OperatingSystemHelper.IsWindows())
            {
                // check if steam cmd is installed
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                // A requirement for steamcmd on ubuntu/debian is lib32gcc1 (sudo apt-get install lib32gcc1)
                // This should be provided as an installation instruction

                // check if steam cmd is installed
            }
            else
            {
                Console.WriteLine("This operating system is not currently supported...");
            }

            Console.Read();
        }
    }
}
