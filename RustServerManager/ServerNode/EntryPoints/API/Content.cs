using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServerNode.EntryPoints.API
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class Content
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("command")]
        public NamedCommand Command { get; set; }

        [JsonProperty("params")]
        public string[] Arguments { get; set; }

        [JsonConstructor]
        public Content() { }

        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}
