using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServerNode.Utility
{
    internal static class ConsoleHelper
    {
        public static async Task<string> InteruptReadForInput(bool redact = false, string redactText = "################ <REDACTED> ################")
        {
            TaskCompletionSource<string> interuptInput = new TaskCompletionSource<string>();
            Program.InteruptedInput = interuptInput;
            string input = await Program.InteruptedInput.Task;

            if (redact)
            {
                //Move cursor to just before the input just entered
                int top = Console.CursorTop;
                Console.CursorTop -= 1;
                Console.CursorLeft = 0;
                //blank out the content that was just entered
                Console.WriteLine(redactText.PadRight(Console.BufferWidth - redactText.Length));
                //move the cursor to just before the input was just entered
                Console.CursorTop = top;
                Console.CursorLeft = 0;
            }

            return input;
        }
    }
}
