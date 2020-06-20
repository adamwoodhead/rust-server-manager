using System;
using System.Collections.Generic;
using System.Text;

namespace ServerNode.EntryPoints.API
{
    internal static class Authorisation
    {
        public static bool Login(string email, string password) => ApiConnector.Authorisation.Login(email, password);

        public static bool Logout() => ApiConnector.Authorisation.Logout();

        public static bool Register(string email, string password) => ApiConnector.Authorisation.Register(email, password);
    }
}
