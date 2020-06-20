using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ApiConnector
{
    public class Authorisation
    {
        public static bool Login(string email, string password) => Models.Authorisation.Login(email, password);

        public static bool Logout() => Models.Authorisation.Logout();

        public static bool Register(string email, string password) => Models.Authorisation.Login(email, password);
    }
}
