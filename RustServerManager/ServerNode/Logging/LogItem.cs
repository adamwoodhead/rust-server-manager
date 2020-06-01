using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Logging
{
    internal class LogItem : IDisposable
    {
        internal DateTime recordedAt;
        internal string message;
        internal ConsoleColor color;

        /// <summary>
        /// Construct a log item
        /// </summary>
        /// <param name="value"></param>
        /// <param name="color"></param>
        public LogItem(string value, ConsoleColor color = ConsoleColor.DarkGray)
        {
            this.recordedAt = DateTime.Now;
            this.message = value;
            this.color = color;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
