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
        public string RustServerDirectory { get; set; }

        public string ActualServerDirectory { get => Path.Combine(RustServerDirectory, "RustDedicated"); }

        public string SteamCMDFolder { get => Path.Combine(RustServerDirectory, "SteamCMD"); }

        public string SteamCMDExecutable { get => Path.Combine(SteamCMDFolder, "steamcmd.exe"); }
    }
}
