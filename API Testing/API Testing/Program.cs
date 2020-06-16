using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using ServerNode.Models.Servers;
using System.Text;

namespace API_Testing
{
    class Program
    {
        // Create a global http client we will use to communicate with the API / Node
        private static readonly HttpClient client = new HttpClient();
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        // Servers table properties
        public class ServerProps
        {
            public string name { get; set; }
            public string description { get; set; }
            public string slots { get; set; }
        }


        static void Main(string[] args)
        {
            // The base address for the test domain
            client.BaseAddress = new Uri("http://rustservermanager.test/");
            string command;
            // Exit the program is the exit key's are pressed.
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            // Ask the user for the commands to be issued
            Console.WriteLine("Rust Server Manager CLI | Version: 0.0.1 | 'quit' to exit | 'help' to view commands");
            ResetPrompt();

            _quitEvent.WaitOne();
            Environment.Exit(0);
        }

        // Get the API token for the authenticated user
        static async void Login()
        {
            // Just for testing purposes
            string loginUrl = "/api/login";
            string email;
            string password;

            Console.Write("Email: ");
            email = Console.ReadLine();
            Console.Write("Password: ");
            password = Console.ReadLine();

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

            // Deserialize the JSON array to an object
            var data = (JObject)JsonConvert.DeserializeObject(resultContent);

            // Extract the JSON token and store in configuration
            string accessToken = data["access_token"].Value<string>();
            ConfigurationManager.AppSettings["access_token"] = accessToken;

            // Success!
            Console.WriteLine("Your API token has been saved to your client configuration!");
            ResetPrompt();
        }

        private static string FormatJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        private static string MaskPassword()
        {
            Console.Write("Please enter your password: ");
            StringBuilder passwordBuilder = new StringBuilder();
            bool continueReading = true;
            char newLineChar = '\r';
            while (continueReading)
            {
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(true);
                char passwordChar = consoleKeyInfo.KeyChar;

                if (passwordChar == newLineChar)
                {
                    continueReading = false;
                }
                else
                {
                    passwordBuilder.Append(passwordChar.ToString());
                }
            }
            Console.Write(Environment.NewLine);
            return passwordBuilder.ToString();
        }

        static async void GetAllServers()
        {
            // Send the API request to create a new server
            string getAllServersUrl = "/api/servers";

            // Clear the headers and set auth token.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (ConfigurationManager.AppSettings["access_token"] != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigurationManager.AppSettings["access_token"]);
            }
            else
            {
                Console.WriteLine("You do not have your API token set! Please use 'login' to retrieve it...");
                ResetPrompt();
            }

            HttpResponseMessage response = await client.GetAsync(getAllServersUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Format the JSON and print to the console.
            Console.WriteLine(FormatJson(responseBody));

            // Reset the prompt
            ResetPrompt();
        }

        private static async void CreateServer()
        {
            string createServerUrl = "/api/servers/create";
            string name, description, slots;

            Console.Write("Please enter the name of your new server: ");
            name = Console.ReadLine();
            Console.Write("Please enter a short description: ");
            description = Console.ReadLine();
            Console.Write("Please enter the amount of slots the server will have: ");
            slots = Console.ReadLine();

            // Clear the headers and set auth token.
            client.DefaultRequestHeaders.Accept.Clear();
            // client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (ConfigurationManager.AppSettings["access_token"] != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigurationManager.AppSettings["access_token"]);
            }
            else
            {
                Console.WriteLine("You do not have your API token set! Please use 'login' to retrieve it...");
                ResetPrompt();
            }

            var serverDetails = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", name),
                new KeyValuePair<string, string>("description", description),
                new KeyValuePair<string, string>("slots", slots)
            });

            HttpResponseMessage response = await client.PostAsync(createServerUrl, serverDetails);

            string responseData = await response.Content.ReadAsStringAsync();

            var data = (JObject)JsonConvert.DeserializeObject(responseData);
            Console.WriteLine(data);
            ResetPrompt();
        }

        static void DeleteServer()
        {
            //
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
            Console.Write("% ");
            string _command = Console.ReadLine();
            ParseCommand(_command);
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
                case "server create":
                    Console.WriteLine("Creating new server...");
                    CreateServer();
                    break;
            }
            ResetPrompt();
        }
    }
}
