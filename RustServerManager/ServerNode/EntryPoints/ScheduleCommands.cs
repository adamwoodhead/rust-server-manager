using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.EntryPoints
{
    internal static class ScheduleCommands
    {
        public static void Consume(string[] arguments)
        {
            string time = "";
            string[] parameters = new string[] { };
            bool loop = false;

            if (arguments.Count() > 0)
            {
                time = arguments[0];
                parameters = arguments.Skip(1).ToArray();

                loop = parameters[0] == "loop";

                if (loop)
                {
                    parameters = parameters.Skip(1).ToArray();
                }
            }
            else
            {
                Log.Error($"schedule command requires time & action input: schedule <second> <command>");
            }

            string fullCommand = string.Join(' ', parameters);
            
            Task.Run(async() => {
                int laps = 0;
                while (true)
                {
                    await Task.Delay(1000 * Convert.ToInt32(time));

                    Log.Informational($"#### Scheduled Commands Executing...");
                    Log.Verbose($"Executing Scheduled Commands: {fullCommand}");

                    await Console.ParseCommand(fullCommand);

                    laps++;
                    Log.Success($"#### Scheduled Commands Completed #{laps}");

                    if (!loop)
                    {
                        break;
                    }
                }
            });

            Log.Informational($"Command Scheduled for {time} seconds -> {fullCommand}");
        }
    }
}
