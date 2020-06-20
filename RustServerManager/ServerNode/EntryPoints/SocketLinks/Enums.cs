using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.EntryPoints.SocketLinks
{
    public enum NamedCommand
    {
        CREATE,
        DELETE,

        START,
        RESTART,
        STOP,
        KILL,

        INSTALL,
        UPDATE,
        REINSTALL,
        UNINSTALL,
    }

    public enum NamedSocketType
    {
        COMMAND
    }
}
