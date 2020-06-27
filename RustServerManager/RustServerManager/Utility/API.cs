using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Utility
{
    internal class API
    {
        internal enum RequestType { authenticate, create_user, update_user }

        private static HttpClient HttpClient = new HttpClient();

        internal static void Initialize()
        {
            // Update port # in the following line.
            HttpClient.BaseAddress = new Uri("https://rustsmapi.adamwoodhead.co.uk/");

            HttpClient.DefaultRequestHeaders.Accept.Clear();

            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        internal static async Task<APIResponse> AuthenticateUserAsync(string email, string password)
        {
            // The API seems to have a serious problem with the content sent using this, not sure why at the moment
            // APIRequest request = new APIRequest(RequestType.authenticate, email, password);

            // This works at the moment
            var credentials = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password)
            });

            // Just send as POST async for now, no JSON.
            HttpResponseMessage response = await HttpClient.PostAsync("api/login", credentials);

            string body = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<APIResponse>(body);
        }

        internal static async Task<APIResponse> CreateUserAsync(string username, string password)
        {
            APIRequest request = new APIRequest(RequestType.create_user, username, password);

            HttpResponseMessage response = await HttpClient.PostAsJsonAsync("", request);

            string body = await response.Content.ReadAsStringAsync();

            Console.WriteLine(body);

            return JsonConvert.DeserializeObject<APIResponse>(body);
        }
    }

    [JsonObject]
    internal class APIResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("error")]
        public bool IsError { get; set; }
    }

    [JsonObject]
    internal class APIRequest
    {
        [JsonProperty("auth")]
        internal Authentication Authentication { get; set; }

        [JsonProperty("request")]
        internal string Request { get; set; }

        [JsonProperty("version")]
        internal string Version { get; set; } = App.Version;

        internal APIRequest(API.RequestType requestType)
        {
            Request = requestType.ToString();
        }

        public APIRequest(API.RequestType requestType, string email, string password)
        {
            Request = requestType.ToString();

            Authentication = new Authentication
            {
                Email = email,
                Password = password
            };
        }
    }

    [JsonObject]
    internal class Authentication
    {
        [JsonProperty("id")]
        internal int ID { get; set; }

        [JsonProperty("email")]
        internal string Email { get; set; }

        [JsonProperty("password")]
        internal string Password { get; set; }
    }
}
