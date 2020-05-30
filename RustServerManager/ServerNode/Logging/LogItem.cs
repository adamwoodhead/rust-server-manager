using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Logging
{
    internal class LogItem : IDisposable
    {
        internal string message;
        internal ConsoleColor color;
        private bool disposedValue;

        public LogItem(string value, ConsoleColor color = ConsoleColor.DarkGray)
        {
            this.message = value;
            this.color = color;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
