using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerNode.Native.Windows
{
    internal static class Utility
    {
        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        public static void SetTitle(Process process, string title)
        {
            SetWindowText(process.MainWindowHandle, title);
        }
    }
}
