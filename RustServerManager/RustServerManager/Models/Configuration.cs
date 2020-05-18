using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        public string WorkingDirectory { get; set; }

        [DataMember]
        public int SavedWidth { get; set; }

        [DataMember]
        public int SavedHeight { get; set; }

        public Configuration(int w, int h)
        {
            SavedWidth = w;
            SavedHeight = h;
        }
    }
}
