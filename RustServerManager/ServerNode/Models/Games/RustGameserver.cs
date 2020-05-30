using ServerNode.Models.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ServerNode.Models.Games
{
    internal class RustGameserver : Server
    {
        internal RustGameserver()
        {
            App = SteamApp.Apps.FirstOrDefault(x => x.ShortName == "css");
            ID = 1;
        }
    }
}
