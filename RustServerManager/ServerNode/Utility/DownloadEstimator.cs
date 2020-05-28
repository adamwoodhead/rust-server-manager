using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Utility
{
    internal class DownloadEstimator
    {
        /// <summary>
        /// Estimate the download speed based on current bytes, total bytes and time consumed
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="currentPos"></param>
        /// <param name="consumed"></param>
        /// <returns></returns>
        internal static double EstimateSpeed(long startPos, long currentPos, DateTime timeStarted)
        {
            TimeSpan consumedTime = DateTime.Now - timeStarted;

            long downloadedBytes = currentPos - startPos;

            return (downloadedBytes / consumedTime.TotalSeconds);
        }

        /// <summary>
        /// Eistimate the time left to complete download, based on start bytes, current downloaded, total to download, and time elapsed
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="currentPos"></param>
        /// <param name="endPos"></param>
        /// <param name="consumed"></param>
        /// <returns></returns>
        internal static TimeSpan EstimateTimeLeft(long startPos, long currentPos, long endPos, DateTime timeStarted)
        {
            long downloadedAmount = currentPos;
            long toDownload = endPos - downloadedAmount;
            double seconds = (toDownload / EstimateSpeed(startPos, currentPos, timeStarted));

            return TimeSpan.FromSeconds(seconds);
        }
    }
}
