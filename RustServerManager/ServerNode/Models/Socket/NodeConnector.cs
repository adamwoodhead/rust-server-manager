using Newtonsoft.Json;
using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Models.Connection
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 4096;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousSocketListener
    {
        public static bool IsClientConnected { get => Client?.Connected ?? false; }

        private static Socket Client { get; set; }

        private static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener()
        {
        }

        // Add cancellation token to node connector listen task
        public static void BeginListening()
        {
            Task.Run(() => {
                IPAddress ipAddress;
                IPEndPoint localEndPoint;

                if (Debugger.IsAttached)
                {
                    // We're debugging, ideally we want to just do this locally.
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    ipAddress = ipHostInfo.AddressList[0];
                    localEndPoint = new IPEndPoint(ipAddress, 11000);
                }
                else
                {
                    // Broadcast and listen externally
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    ipAddress = IPAddress.Parse("0.0.0.0");
                    localEndPoint = new IPEndPoint(ipAddress, 11000);
                }

                // Create a TCP/IP socket.  
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Bind the socket to the local endpoint and listen for incoming connections.  
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (true)
                    {
                        // Set the event to nonsignaled state.  
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.  
                        Log.Verbose("Socket awaiting connection...");

                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();
                    }

                }
                catch (Exception e)
                {
                    Log.Error($"Socket no longer listening!!");
                    Log.Error(e.ToString());
                }
            });
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;

            Socket handler = listener.EndAccept(ar);

            Log.Verbose($"Socket Accepted, Remote Address: {IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString())}");

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            try
            {
                string content = string.Empty;

                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                Client = state.workSocket;

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read
                    // more data.  
                    content = state.sb.ToString();

                    try
                    {
                        SocketPacket packet = JsonConvert.DeserializeObject<SocketPacket>(content);

                        if (packet != null)
                        {
                            if (packet.PassedValidation)
                            {
                                EntryPoints.Console.Console.ParseCommand(packet.Content, true).Wait();
                            }
                            else
                            {
                                Log.Verbose($"Socket packet failed validation");
                            }
                        }
                    }
                    catch (JsonReaderException jrex)
                    {
                        // if we lost connection with the client
                        Log.Error($"Socket Packet format incorrect.");
                        Log.Error(content);
                        Log.Error(jrex.Message);
                    }

                    // clear string builder
                    state.sb.Clear();

                    // begin awaiting for more received data
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
            catch (SocketException sex)
            {
                // if we lost connection with the client
                if (sex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Log.Error($"Connected Socket has lost connection!");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        internal static void Send(string data)
        {
            SocketPacket packet = new SocketPacket(data);

            string json = packet.ToJson();

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(json);

            // Begin sending the data to the remote device.  
            Client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), Client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal class SocketPacket
    {
        private bool? passedValidation = null;

        [JsonProperty("content")]
        public string Content { get; private set; }

        [JsonProperty("hash")]
        private string ValidationHash { get; set; }

        public bool PassedValidation
        {
            get
            {
                if (passedValidation == null)
                {
                    passedValidation = !string.IsNullOrEmpty(Content) && ComputeSha256Hash(Content) == ValidationHash;
                }

                return (bool)passedValidation;
            }
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        [JsonConstructor]
        public SocketPacket() { }

        public SocketPacket(string content)
        {
            Content = content;
            ValidationHash = ComputeSha256Hash(content);
        }

        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
