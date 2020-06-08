using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.Logging
{
    internal class LogItem : IDisposable
    {
        public DateTime recordedAt;
        public string message;
        public LogType type;
        public ConsoleColor conColor;
        public bool hardLog;
        public bool conWrite;

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
