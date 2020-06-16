using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode
{
    /// <summary>
    /// States of procedure that a SteamCMD object may reside.
    /// </summary>
    public enum SteamCMDState
    {
        UNDEFINED,
        AWAITING_INPUT,

        // steamcmd login
        LOGGING_IN,
        LOGGED_IN,
        LOGIN_REQUIRES_PASSWORD,
        LOGIN_REQUIRES_STEAMGUARD,
        LOGIN_FAILED_BAD_PASS,
        LOGIN_FAILED_RATE_LIMIT,
        LOGIN_FAILED_GUARD_MISMATCH,
        LOGIN_FAILED_GENERIC,

        // steamcmd installation section
        STEAMCMD_DOWNLOADING_INSTALLER, // null
        STEAMCMD_CHECKING_UPDATES,      // [  0%] Checking for available update...
        STEAMCMD_DOWNLOADING_UPDATES,   // [  0%] Downloading update (2,523 of 39,758 KB)...
        STEAMCMD_EXTRACTING_PACKAGES,   // [----] Extracting package...
        STEAMCMD_INSTALLING_UPDATE,     // [----] Installing update...
        STEAMCMD_VERIFYING,             // [----] Verifying installation...
        STEAMCMD_LOADED,                // Loading Steam API...OK.

        // app installation section
        APP_VALIDATING,                 // Update state (0x5) validating, progress: 0.03 (1401888 / 5364833225)
        APP_PREALLOCATING,              // Update state (0x11) preallocating, progress: 31.23 (1675318076 / 5364833225)
        APP_DOWNLOADING,                // Update state (0x61) downloading, progress: 0.06 (3145728 / 5364833225)
        APP_POST_DOWNLOAD_VALIDATING,   // Update state (0x5) validating, progress: 0.03 (1401888 / 5364833225)
        APP_VERIFYING,                  // Update state (0x5) verifying install, progress: 94.11 (2178497941 / 2314842927)
        APP_INSTALLED,                  // Success! App '258550' fully installed.
        APP_INSTALL_ERROR,              // Error! App '232330' state is ***** after update job
        APP_INSTALL_ERROR_NO_DISK       // Error! App '740' state is 0x202 after update job
    }

    public enum ExitType
    {
        PROCESS_EXIT,
        MANUAL,
        RESTART
    }
}
