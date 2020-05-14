using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Models.WebRcon
{
    internal class Callback
    {
        internal Func<string, object> CallbackFunc { get; set; }

        internal int? Identifier { get; set; }

        internal Callback(Func<string, object> callbackFunc, int? identifier = -1)
        {
            CallbackFunc = callbackFunc;
            Identifier = identifier;
        }
    }
}
