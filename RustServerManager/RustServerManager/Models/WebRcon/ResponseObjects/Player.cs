using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models.WebRcon.ResponseObjects
{
    [DataContract]
    class Player
    {
        [DataMember]
        public string SteamID { get; set; }

        [DataMember]
        public long OwnerSteamID { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public long Ping { get; set; }

        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public long ConnectedSeconds { get; set; }

        [DataMember]
        public long VoiationLevel { get; set; }

        [DataMember]
        public long CurrentLevel { get; set; }

        [DataMember]
        public long UnspentXp { get; set; }

        [DataMember]
        public long Health { get; set; }
    }
}
