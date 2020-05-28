using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Utility
{
    internal static class ByteMeasurements
    {
        internal static double BytesToKB(double bytes)
        {
            return bytes / 1024;
        }

        internal static double BytesToMB(double bytes)
        {
            return BytesToKB(bytes) / 1024;
        }
    }
}
