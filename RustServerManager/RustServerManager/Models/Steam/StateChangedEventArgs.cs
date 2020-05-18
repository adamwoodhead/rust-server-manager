using System;
using static RustServerManager.Models.Steam.SteamCMD;

namespace RustServerManager.Models.Steam
{
    internal class StateChangedEventArgs : EventArgs
    {
        internal SteamCMDState State { get; private set; }

        internal StateChangedEventArgs(SteamCMDState _state)
        {
            State = _state;
        }
    }
}
