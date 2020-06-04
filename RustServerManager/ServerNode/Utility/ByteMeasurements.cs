using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Utility
{
    internal static class ByteMeasurements
    {
        /// <summary>
        /// Convert Bytes To KiloBytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        internal static double BytesToKB(double bytes)
        {
            return bytes / 1024;
        }

        internal static double KiloBytesToMB(double kilobytes)
        {
            return kilobytes / 1024;
        }

        /// <summary>
        /// Convert Bytes To MegaBytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        internal static double BytesToMB(double bytes)
        {
            return BytesToKB(bytes) / 1024;
        }
    }
}
