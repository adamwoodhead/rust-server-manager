﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.EntryPoints
{
    internal static class Console
    {
        public static async Task ParseCommand(string command)
        {
            string[] commands = command.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length > 1)
            {
                System.Console.WriteLine($"Picked up multiple commands, sequentially parsing.");

                foreach (string com in commands)
                {
                    await ParseCommand(com);
                }

                return;
            }

            string subject = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            string[] arguments = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

            if (!string.IsNullOrEmpty(subject))
            {
                switch (subject)
                {
                    case "quit":
                        RunQuitCommand();
                        break;

                    case "exit":
                        RunQuitCommand();
                        break;

                    case "help":
                        RunHelpCommand(arguments);
                        break;

                    case "server":
                        await ServerCommands.Consume(arguments);
                        break;

                    case "app":
                        await AppCommands.Consume(arguments);
                        break;

                    case "log":
                        await LogCommands.Consume(arguments);
                        break;

                    case "schedule":
                        await ScheduleCommands.Consume(arguments);
                        break;

                    default:
                        break;
                }
            }
        }

        private static void RunQuitCommand()
        {
            Program.SafeExit();
        }

        private static void RunHelpCommand(string[] arguments)
        {
            string topic;

            if (arguments.Count() > 0)
            {
                topic = arguments[0];
            }
            else
            {
                topic = "help";
            }

            switch (topic)
            {
                case "help":
                    WrapHelpInfo(
                        "Available Commands",
                        "Help",
                        "Use these commands to control your server node. <command> <action> <id>\n" +
                        "Single Target: <command> <action> <id>\n" +
                        "Multi Target: <command> <action> <id,id,id>\n" +
                        "Target All: <command> <action> *",
                        new Dictionary<string, string> {
                            { "quit | exit", "Safely exit Server Node, cleaning up active servers and data." },
                            { "server", "Server Information, use 'server --help' for more info." },
                            { "app", "Apps Information, use 'app --help' for more info." },
                            { "log", "Logs Information, use 'log --help' for more info." },
                            { "schedule", "Task Schedule Information, use 'schedule --help' for more info." },
                            { "<command> --help", "View more in depth help on a specific command." }
                        });
                    break;

                case "exit":
                    WrapHelpInfo(
                        "Exit",
                        "Commands",
                        "The <exit> command will clean up any loose ends before shutting down. (Highly recommended)",
                        new Dictionary<string, string>());
                    break;

                case "quit":
                    WrapHelpInfo(
                        "Quit",
                        "Commands",
                        "The <quit> command will clean up any loose ends before shutting down. (Highly recommended)",
                        new Dictionary<string, string>());
                    break;

                case "server":
                    WrapHelpInfo(
                        "Server Commands",
                        "server",
                        "The <server> command is for controlling servers, such as installations, starting and stopping.",
                        new Dictionary<string, string> {
                            { "list", "lists all servers on this server node" },
                            { "cleanup", "removes any server folders that are un-used" },
                            { "list <appshortname>", "filtered list by app shortname (e.g. server list rust)" },
                            { "create <appshortname>", "create a new server with the app" },
                            { "start <id>", "starts a server by id" },
                            { "stop <id>", "starts a server by id" },
                            { "kill <id>", "kills a server by id" },
                            { "install <id>", "installs a server by id" },
                            { "update <id>", "updates a server by id" },
                            { "uninstall <id>", "uninstalls a server by id" },
                            { "reinstall <id>", "reinstalls a server by id" },
                            { "delete <id>", "deletes a server by id" },
                        });
                    break;

                case "app":
                    WrapHelpInfo(
                        "App Commands",
                        "apps",
                        "The <apps> command is for viewing data on available apps.",
                        new Dictionary<string, string> {
                            { "view <shortname>", "view app information" },
                            { "list", "lists all apps available by shortname:longname" },
                        });
                    break;

                case "log":
                    WrapHelpInfo(
                        "Logs Commands",
                        "logs",
                        "The <logs> command is for modifying the visibility of different log types.\n" +
                        "Log types available:\n" +
                        "verbose, informational, success, warnings, errors, debugging",
                        new Dictionary<string, string> {
                            { "enable <logtype>", "enable the specific log type" },
                            { "disable <logtype>", "disable the specific log type" },
                        });
                    break;

                case "schedule":
                    WrapHelpInfo(
                        "Schedule Commands",
                        "schedule",
                        "The <schedule> command is for scheduling tasks in a simplistic form.\n" +
                        "Used in the following format:\n" +
                        "schedule <seconds> <command> <parameters>",
                        new Dictionary<string, string> {
                            { "schedule 60 server start 0", "runs the command <server start 0> after 60 seconds" },
                            { "schedule 3600 server stop 0", "runs the command <server start 0> after 1 hour" },
                        });
                    break;

                default:
                    break;
            }
        }

        private static void WrapHelpInfo(string title, string command, string description, Dictionary<string, string> actions)
        {
            System.Console.WriteLine($"// ".PadRight(98, '-') + " //");
            System.Console.WriteLine($"// {title}".PadRight(98, ' ') + " //");
            System.Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            foreach (string line in description.Split("\n"))
            {
                System.Console.WriteLine($"// {line.Replace("\n", "")}".PadRight(98, ' ') + " //");
            }
            System.Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            System.Console.WriteLine($"// Command -> {command}".PadRight(98, ' ') + " //");
            System.Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            foreach (var action in actions)
            {
                System.Console.WriteLine($"// Action -> {action.Key}".PadRight(98, ' ') + " //");
                System.Console.WriteLine($"//        -> {action.Value}".PadRight(98, ' ') + " //");
                System.Console.WriteLine($"// ".PadRight(98, ' ') + " //");
            }
            System.Console.WriteLine($"// ".PadRight(98, '-') + " //");
        }
    }
}