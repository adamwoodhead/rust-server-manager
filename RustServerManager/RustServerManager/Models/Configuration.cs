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
    }
}
