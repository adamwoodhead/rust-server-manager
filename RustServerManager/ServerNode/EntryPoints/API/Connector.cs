using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ServerNode.EntryPoints.API
{
    internal static class Connector
    {
        public static void Initialise() => ApiConnector.Connector.Initialise(Program.ApiRootURL);
    }
}
