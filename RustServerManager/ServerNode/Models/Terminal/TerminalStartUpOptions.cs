using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Models.Terminal
{
    public class TerminalStartUpOptions
    {
        public string Name { get; }

        public int? InputTimeout { get; }

        public TerminalStartUpOptions(string name, int? inputtimeout = null)
        {
            this.Name = name;
            this.InputTimeout = inputtimeout;
        }
    }
}
