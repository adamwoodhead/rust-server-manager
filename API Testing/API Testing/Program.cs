using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using ServerNode.Models.Servers;

namespace API_Testing
{
    class Program
    {
        // Create a global http client we will use to communicate with the API / Node
        private static readonly HttpClient client = new HttpClient();

        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        // User/Servers table properties
        public class ServerProps
        {
            public int id { get; set; }
            public string name { get; set; }
            public string ip_address { get; set; }
            public string description { get; set; }
            public string status { get; set; }
            public string slots { get; set; }
            public string type { get; set; }
            public int user_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }


        static void Main(string[] args)
        {
            string command;
            // Exit the program is the exit key's are pressed.
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            // Ask the user for the commands to be issued
            Console.WriteLine("Rust Server Manager CLI | Version: 0.0.1 | 'quit' to exit | 'help' to view commands");
            Console.Write("% ");
            command = Console.ReadLine();
            ParseCommand(command);

            _quitEvent.WaitOne();
            Environment.Exit(0);
        }

        // Get the API token for the authenticated user
        static async void Login()
        {
            // Just for testing purposes
            string loginUrl = "http://rustservermanager.test/api/login";
            string email = "johndoe@gmail.com";
            string password = "password";

            // Store the creds in a Form encoded array
            var credentials = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password)
            });

            // Send a POST request to the API with the Form values
            HttpResponseMessage response = await client.PostAsync(loginUrl, credentials);

            // Read the response string
            string resultContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(resultContent);

            // Deserialize the JSON array to an object
            var data = (JObject)JsonConvert.DeserializeObject(resultContent);

            // Extract the JSON token and store in configuration
            string apiToken = data["access_token"].Value<string>();
            ConfigurationManager.AppSettings["api_token"] = apiToken;

            // Success!
            Console.WriteLine("\nYour API token has been saved to your client configuration!");
            ResetPrompt();
        }

        static async void GetAllServers()
        {
            // Send the API request to create a new server
            string createNewServerUrl = "http://rustservermanager.test/api/servers";
            HttpResponseMessage response = await client.GetAsync(createNewServerUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseBody);

            // string formatted = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
            JObject parsed = JObject.Parse(responseBody);

            foreach (var pair in parsed)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            Console.Write("% ");
            Console.ReadLine();
        }

        static void DeleteServer()
        {

        }

        static void GetServerInfo()
        {
            // Send the API requests to get server information
        }

        static void GetNodeInfo()
        {
            // Send API request to get Server Node information
        }

        static void ResetPrompt()
        {
            Console.WriteLine(Environment.NewLine);
            Console.Write("% ");
            string command = Console.ReadLine();
            ParseCommand(command);
        }

        static void ParseCommand(string command)
        {
            // Parse user commands for manipulating API / Node
            switch (command)
            {
                case "quit":
                    Environment.Exit(0);
                    break;
                case "help":
                    Console.WriteLine("Basic commands are: 'servers list', 'server get', 'server delete', 'server stop', 'server up'");
                    break;
                case "servers list":
                    Console.WriteLine("Getting all servers...");
                    GetAllServers();
                    break;
                case "login":
                    Console.WriteLine("logging in...");
                    Login();
                    break;
            }

            ResetPrompt();
        }
    }
}
