using System;
using static ServerNode.Models.Steam.SteamCMD;

namespace ServerNode.Models.Steam
{
    internal class StateChangedEventArgs : EventArgs
    {
        public SteamCMDState State { get; private set; }

        public StateChangedEventArgs(SteamCMDState _state)
        {
            State = _state;
        }
    }
}
