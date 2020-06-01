using System;
using System.Runtime.InteropServices;

namespace ServerNode.Utility
{
    public static class OperatingSystemHelper
    {
        private static bool? isWindows = null;
        private static bool? isMacOS = null;
        private static bool? isLinux = null;
        private static bool? isFreeBSD = null;

        /// <summary>
        /// Is the current operating system Windows
        /// </summary>
        /// <returns></returns>
        public static bool IsWindows() => isWindows ??= RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Is the current operating system MacOS
        /// </summary>
        /// <returns></returns>
        public static bool IsMacOS() => isMacOS ??= RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Is the current operating system Linux
        /// </summary>
        /// <returns></returns>
        public static bool IsLinux() => isLinux ??= RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Is the current operating system FreeBSD
        /// </summary>
        /// <returns></returns>
        public static bool IsFreeBSD() => isFreeBSD ??= RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }
}
