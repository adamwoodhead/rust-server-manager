using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models
{
    internal class ConsoleWriter : TextWriter
    {
        public ObservableCollection<string> ConsoleEntries { get; set; } = new ObservableCollection<string>();

        public override Encoding Encoding => Encoding.ASCII;

        public new void WriteLine(string value)
        {
            if (ConsoleEntries.Count < 10000)
            {
                ConsoleEntries.Insert(0, value);
            }
            else if (ConsoleEntries.Count == 10000)
            {
                ConsoleEntries.Insert(0, value);
                ConsoleEntries.RemoveAt(ConsoleEntries.Count - 1);
            }
        }
    }
}
