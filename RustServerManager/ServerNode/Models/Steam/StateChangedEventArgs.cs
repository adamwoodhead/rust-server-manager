using System;
using static ServerNode.Models.Steam.SteamCMD;

namespace ServerNode.Models.Steam
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
