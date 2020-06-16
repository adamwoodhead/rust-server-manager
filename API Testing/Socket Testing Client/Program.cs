using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Socket_Testing_Client
{
    class Program
    {
        private static IPEndPoint LocalEndPoint { get; set; }

        static void Main(string[] args)
        {
            // Server Node IP Address

            Console.WriteLine("To test, launch both the servernode and this client, the order does not matter.");

            Console.WriteLine("Both must be launched vs debugger, OR both must be launched outside of the debugger for effective testing.");

            IPAddress ipAddress;

            if (Debugger.IsAttached)
            {
                // We're testing locally, use the local IP
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                ipAddress = ipHostInfo.AddressList[0];
                LocalEndPoint = new IPEndPoint(ipAddress, 11000);
            }
            else
            {
                // We're not testing locally, perhaps we're connected to the dedi?
                // The IP stated here related to the IP of the machine hosting the server node
                ipAddress = IPAddress.Parse("51.68.204.234");
                LocalEndPoint = new IPEndPoint(ipAddress, 11000);
            }

            string input = null;
            //loop
            while (true)
            {
                // lets give the client a small breather shall we?
                Thread.Sleep(500);

                // if we're not connected
                if (!NodeConnector.IsConnected)
                {
                    // connect
                    try
                    {
                        NodeConnector.Connect(LocalEndPoint);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                // get our desired input
                input ??= Console.ReadLine();

                // we could have lost connection
                if (!NodeConnector.IsConnected)
                {
                    // lets continue to loop again
                    continue;
                }

                // send the input
                NodeConnector.SendPacket(input);

                // we sent the packet, we want to assign input again
                input = null;
            }
        }
    }

    internal class NodeConnector
    {
        private static Socket Client { get; set; }

        public static bool IsConnected { get => Client?.Connected ?? false; }

        private static ManualResetEvent connectDone { get; } = new ManualResetEvent(false);

        private static ManualResetEvent sendDone { get; } = new ManualResetEvent(false);

        private static ManualResetEvent receiveDone { get; } = new ManualResetEvent(false);

        public static void Connect(EndPoint remoteEP)
        {
            try
            {
                Console.WriteLine("connecting");

                Client = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                Client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), Client);

                connectDone.WaitOne();

                if (Client.Connected)
                {
                    Console.WriteLine("connected");
                    Receive(Client);
                }
            }
            catch (Exception ex)
            {
                Client = null;
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                connectDone.Set();
                connectDone.Reset();
                Console.WriteLine(e.Message);
            }
        }

        public static void SendPacket(string data = "")
        {
            try
            {
                SocketPacket packet = new SocketPacket(data);

                string content = Newtonsoft.Json.JsonConvert.SerializeObject(packet);

                Console.WriteLine();
                Console.WriteLine($"Sending Packet:");
                Console.WriteLine(content);

                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(content);

                // Begin sending the data to the remote device.  
                Client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), Client);
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed to sent packet");
                Console.WriteLine(ex.Message);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine($"Sent {bytesSent} bytes to server.");

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                string incoming = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                SocketPacket packet = JsonConvert.DeserializeObject<SocketPacket>(incoming);
                
                if (packet.PassedValidation)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"servernode> {packet.Content}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Socket Packet was corrupted: ");
                    Console.WriteLine(incoming);
                    Console.ResetColor();
                }


                //receiveDone.Set();

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException sex)
            {
                // lost connection to server
                if (sex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Console.WriteLine("Lost connection to server...");
                }
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
                    passedValidation = ComputeSha256Hash(Content) == ValidationHash;
                }

                return (bool)passedValidation;
            }
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        internal SocketPacket() { }

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

    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 4096;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
}
