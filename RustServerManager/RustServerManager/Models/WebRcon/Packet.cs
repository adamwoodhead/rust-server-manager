using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models.WebRcon
{
    [DataContract]
    internal class Packet
    {
        [DataMember]
        internal int? Identifier { get; set; }

        [DataMember]
        internal string Message { get; set; }

        [DataMember]
        internal string Type { get; set; }

        [DataMember]
        internal string Stacktrace { get; set; }

        [DataMember]
        internal string Name { get; set; } = "WebRcon";

        internal Packet(int? identifier, string message)
        {
            Identifier = identifier;
            Message = message;
        }

        internal Packet() { }
    }
}
