using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;

namespace RustServerManager.Models.WebRcon
{
    internal class RconService
    {
        internal bool IsConnected
        {
            get
            {
                if (Socket == null)
                {
                    return false;
                }

                return this.Socket.ReadyState == WebSocketState.Open;
            }
        }

        internal WebSocket Socket { get; set; }

        internal string Address { get; set; }

        internal List<Callback> Callbacks { get; set; } = new List<Callback>();

        internal int LastIdentifier = 1001;

        internal void Connect(string addr, string pass)
        {
            this.Socket = new WebSocket("ws://" + addr + "/" + pass);

            this.Socket.Connect();

            this.Address = addr;

            this.Socket.OnMessage += this.OnMessage;

            this.Socket.OnOpen += this.OnOpen;
            this.Socket.OnClose += this.OnClose;
            this.Socket.OnError += this.OnError;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine("rcon OnMessage");
            Packet data = JsonConvert.DeserializeObject<Packet>(e.Data);

            //
            // This is a targetted message, it has an identifier
            // So feed it back to the right callback.
            //
            if (data.Identifier > 1000)
            {
                var callback = this.Callbacks.FirstOrDefault(x => x.Identifier == data.Identifier);

                if (callback != null)
                {
                    callback.CallbackFunc?.Invoke(data.Message);

                    this.Callbacks.Remove(callback);
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("rcon OnError");
            throw new NotImplementedException();
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("rcon OnClose");
            throw new NotImplementedException();
        }

        private void OnOpen(object sender, EventArgs e)
        {
            Console.WriteLine("rcon OnOpen");
            throw new NotImplementedException();
        }

        internal void Disconnect()
        {
            if (this.Socket != null)
            {
                this.Socket.Close();
                this.Socket = null;
            }

            this.Callbacks = null;
        }

        private void Command(string msg, int? identifier)
        {
            if (this.Socket == null || !this.IsConnected)
            {
                Console.WriteLine("Attempted to send command, but we're not connected!");
                return;
            }

            if (identifier == null)
            {
                identifier = -1;
            }

            Packet packet = new Packet(identifier, msg);

            this.Socket.Send(JsonConvert.SerializeObject(packet));
        }

        //
        // Make a request, call this function when it returns
        //
        internal void Request(string msg, Func<string, object> callbackFunc = null)
        {
            LastIdentifier++;

            if (callbackFunc != null)
            {
                this.Callbacks.Add(new Callback(callbackFunc, LastIdentifier));
            }

            this.Command(msg, LastIdentifier);
        }

        //
        // Helper for installing connectivity logic
        //
        // Basically if not connected, call this function when we are
        // And if we are - then call it right now.
        //
        //internal void InstallService(scope, func)
        //{
        //    scope.$on("OnConnected", function() {
        //        func();
        //    });

        //    if (this.IsConnected())
        //    {
        //        func();
        //    }
        //}

        internal void GetPlayers()
        {
            this.Request("playerlist", (s) => { Console.WriteLine(s); return null; });
        }

        internal void Say(string message)
        {
            this.Request($"say {message}");
        }
    }
}