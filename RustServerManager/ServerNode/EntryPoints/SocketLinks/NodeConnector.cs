using Newtonsoft.Json;
using ServerNode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.EntryPoints.SocketLinks
{
    public class AsynchronousSocketListener
    {
        public static bool IsClientConnected { get => Client?.Connected ?? false; }

        private static Socket Client { get; set; }

        private static ManualResetEvent allDone = new ManualResetEvent(false);

        private static IPAddress WhitelistedAddress { get; set; } = null;

        public AsynchronousSocketListener()
        {
        }

        // Add cancellation token to node connector listen task
        public static void BeginListening(IPAddress onlyAllowed = null)
        {
            WhitelistedAddress = onlyAllowed ?? null;

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

            if (WhitelistedAddress != null && ((IPEndPoint)handler.RemoteEndPoint).Address != WhitelistedAddress)
            {
                handler.Send(Encoding.ASCII.GetBytes("Connection Unauthorised\r\n"));
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                allDone.Reset();
                return;
            }

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
                                //EntryPoints.Console.Console.ParseCommand(packet.Content, true).Wait();
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

        internal static void SendCommand(Content content)
        {
            SocketPacket packet = new SocketPacket(NamedSocketType.COMMAND, content);

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
                Log.Verbose(e.ToString());
            }
        }
    }
}
