using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ApiConnector.Models
{
    internal class Authorisation : ApiBase
    {
        /// <summary>
        /// Currently Active Authorisation Token, set upon succesfull login
        /// </summary>
        public static string AuthorsationToken { get; private set; }
        public static DateTime TokenExpiry { get; private set; }

        private static string LoginURL { get; set; } = $"{Connector.RootURI}login";
        private static string LogoutURL { get; set; } = $"{Connector.RootURI}logout";
        private static string RegisterURL { get; set; } = $"{Connector.RootURI}register";

        public Authorisation() { }

        /// <summary>
        /// Sends a Login request to the API, and sets the current token, returns true if successful
        /// </summary>
        /// <returns></returns>
        public static bool Login(string email, string password)
        {
            string content = Request(RequestType.POST, LoginURL);

            Models.Auth.Login parsedResponse = JsonConvert.DeserializeObject<Models.Auth.Login>(content);

            if (!string.IsNullOrEmpty(parsedResponse.AccessToken))
            {
                AuthorsationToken = parsedResponse.AccessToken;
                TokenExpiry = DateTime.Now.AddSeconds(parsedResponse.ExpiresInSeconds);

                return true;
            }
            else
            {
                AuthorsationToken = "";
                TokenExpiry = DateTime.Now.Subtract(TimeSpan.FromSeconds(1));

                return false;
            }
        }

        /// <summary>
        /// Sends a Logout request to the API, and removes the current token
        /// </summary>
        /// <returns></returns>
        public static bool Logout()
        {
            // Fire & Forget, we don't need the response.
            Request(RequestType.POST, LogoutURL);

            AuthorsationToken = "";
            TokenExpiry = DateTime.Now.Subtract(TimeSpan.FromSeconds(1));

            return true;
        }

        public static bool Register(string email, string password)
        {
            throw new NotImplementedException();
        }
    }
}
