using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApiConnector.Models.Auth
{
    [JsonObject]
    internal class Login
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; }

        [JsonConstructor]
        public Login() { }
    }
}
