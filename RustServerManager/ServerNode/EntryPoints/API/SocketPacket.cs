using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace ServerNode.EntryPoints.API
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class SocketPacket
    {
        private bool? passedValidation = null;

        [JsonProperty("hash")]
        private NamedSocketType Type { get; set; }

        [JsonProperty("content")]
        public Content Content { get; private set; }

        [JsonProperty("hash")]
        private string ValidationHash { get; set; }

        public bool PassedValidation
        {
            get
            {
                if (passedValidation == null)
                {
                    string strContent = JsonConvert.SerializeObject(Content);
                    passedValidation = ComputeSha256Hash(strContent) == ValidationHash;
                }

                return (bool)passedValidation;
            }
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        [JsonConstructor]
        public SocketPacket() { }

        public SocketPacket(NamedSocketType type, Content content)
        {
            Type = type;
            Content = content;
            ValidationHash = ComputeSha256Hash(content.ToJson());
        }

        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
