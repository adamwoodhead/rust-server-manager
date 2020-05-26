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

        public static bool IsWindows() => isWindows ??= RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS() => isMacOS ??= RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() => isLinux ??= RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static bool IsFreeBSD() => isFreeBSD ??= RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }
}
