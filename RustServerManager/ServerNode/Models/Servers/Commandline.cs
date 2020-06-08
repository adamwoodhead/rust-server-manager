using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Models.Servers
{
    internal static class Commandline
    {
        public static string[] BuildCommandline(Server server)
        {
            string[] commandline = server.CommandLine;

            foreach (Variable variable in server.Variables.Where(x => x.ForCommandline))
            {
                string lineVar = "!{" + variable.Name + "}";

                for (int i = 0; i < server.CommandLine.Length; i++)
                {
                    string val = server.CommandLine[i];

                    if (val.Contains(lineVar))
                    {
                        commandline[i] = commandline[i].Replace(lineVar, variable.Value);
                    }
                }
            }

            return commandline;
        }
    }
}
