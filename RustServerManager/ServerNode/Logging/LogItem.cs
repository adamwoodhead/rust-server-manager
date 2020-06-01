using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Logging
{
    internal class LogItem : IDisposable
    {
        internal DateTime recordedAt;
        internal string message;
        internal LogType type;
        internal ConsoleColor conColor;
        internal bool hardLog;
        internal bool conWrite;

        /// <summary>
        /// Construct a log item
        /// </summary>
        /// <param name="value"></param>
        /// <param name="color"></param>
        public LogItem(string value, LogType logType, (bool, bool, ConsoleColor) p)
        {
            this.recordedAt = DateTime.Now;
            this.message = value;
            this.type = logType;
            this.conWrite = p.Item1;
            this.hardLog = p.Item2;
            this.conColor = p.Item3;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
