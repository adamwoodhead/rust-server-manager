using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerNode.Models
{
    internal class SteamCMD
    {
        private static string DownloadPath
        {
            get
            {
                if (Utility.OperatingSystemHelper.IsWindows()) // We're on Windows
                {
                    return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
                }
                else // We're on Linux
                {
                    return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";
                }
            }
        }

        private static string ExecutablePath
        {
            get
            {
                if (Utility.OperatingSystemHelper.IsWindows()) // We're on Windows
                {
                    return "";
                }
                else // We're on Linux
                {
                    return "";
                }
            }
        }

        internal static bool ExecutableExists()
        {
            return false;
        }

        internal enum SteamCMDState
        {
            UNDEFINED,

            //
            STEAMCMD_CHECKING_UPDATES,      // [  0%] Checking for available update...
            STEAMCMD_DOWNLOADING_UPDATES,   // [  0%] Downloading update (2,523 of 39,758 KB)...
            STEAMCMD_EXTRACTING_PACKAGES,   // [----] Extracting package...
            STEAMCMD_INSTALLING_UPDATE,     // [----] Installing update...
            STEAMCMD_VERIFYING,             // [----] Verifying installation...
            STEAMCMD_LOADED,                // Loading Steam API...OK.

            // 
            APP_VALIDATING,                 // Update state (0x5) validating, progress: 0.03 (1401888 / 5364833225)
            APP_PREALLOCATING,              // Update state (0x11) preallocating, progress: 31.23 (1675318076 / 5364833225)
            APP_DOWNLOADING,                // Update state (0x61) downloading, progress: 0.06 (3145728 / 5364833225)
            APP_POST_DOWNLOAD_VALIDATING,   // Update state (0x5) validating, progress: 0.03 (1401888 / 5364833225)
            APP_INSTALLED                   // Success! App '258550' fully installed.
        }
    }
}
