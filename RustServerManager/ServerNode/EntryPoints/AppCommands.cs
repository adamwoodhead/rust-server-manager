using ServerNode.Models.Steam;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.EntryPoints
{
    internal class AppCommands
    {
        public static async Task Consume(string[] arguments)
        {
            string command = "";
            string[] parameters = new string[] { };

            if (arguments.Count() > 0)
            {
                command = arguments[0];

                parameters = arguments.Skip(1).ToArray();
            }            

            switch (command)
            {
                case "list":
                    PreAPIHelper.Apps.Select(x => x.Value).ToList().ForEach(x => System.Console.WriteLine($"App: {x.ShortName} : {x.Name}"));
                    break;

                case "view":
                    foreach (string parameter in parameters)
                    {
                        System.Console.WriteLine("TODO");
                    }
                    break;

                case "install":
                    foreach (string parameter in parameters)
                    {
                        if (FindApp(parameter, out SteamApp steamApp))
                        {
                            await steamApp.InstallAsync();
                        }
                    }
                    break;

                default:
                    System.Console.WriteLine($"App Command <{command}> not recognised.");
                    break;
            }
        }

        private static bool FindApp(string shortName, out SteamApp steamApp)
        {
            if (PreAPIHelper.Apps.ContainsKey(shortName))
            {
                steamApp = PreAPIHelper.Apps[shortName];
                return true;
            }

            steamApp = null;
            return false;
        }
    }
}
