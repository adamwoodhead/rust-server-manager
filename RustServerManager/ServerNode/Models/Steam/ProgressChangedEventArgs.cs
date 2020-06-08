using System;

namespace ServerNode.Models.Steam
{
    internal class ProgressChangedEventArgs : EventArgs
    {
        public double Progress { get; private set; }

        public ProgressChangedEventArgs(double _progress)
        {
            Progress = _progress;
        }
    }
}
