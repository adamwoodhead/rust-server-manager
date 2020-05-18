using System;

namespace RustServerManager.Models.Steam
{
    internal class ProgressChangedEventArgs : EventArgs
    {
        internal double Progress { get; private set; }

        internal ProgressChangedEventArgs(double _progress)
        {
            Progress = _progress;
        }
    }
}
