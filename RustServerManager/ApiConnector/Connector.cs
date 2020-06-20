using System;

namespace ApiConnector
{
    public static class Connector
    {
        private static bool isInitialised = false;

        internal static string RootURI { get; set; } = @"https://adamwoodhead.co.uk/api/";

        public static void Initialise(string baseAPI)
        {
            if (!isInitialised)
            {
                isInitialised = true;
                RootURI = baseAPI;
            }
            else
            {
                throw new Exception("API Connector already initialised.");
            }
        }
    }
}
