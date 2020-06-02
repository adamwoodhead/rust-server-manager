using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerNode.Native
{
    internal static class Windows
    {
        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        internal static void SetTitle(Process process, string title)
        {
            SetWindowText(process.MainWindowHandle, title);
        }
    }
}
