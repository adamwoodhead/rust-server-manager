using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Terminal
{
    internal class TerminalStartUpOptions
    {
        internal string Name { get; }

        internal int InputTimeout { get; }

        internal TerminalStartUpOptions(string name, int inputtimeout)
        {
            this.Name = name;
            this.InputTimeout = inputtimeout;
        }
    }
}
