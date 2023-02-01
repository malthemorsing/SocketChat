using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class Program
    {
        static List<Socket> connectedClients = new List<Socket>();

        static void Main(string[] args)
        {
            Socket listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, 1604));
            listener.Listen(10);
            listener.BeginAccept(AcceptCallback, listener);

            while (true)
            {
                var input = Encoding.UTF8.GetBytes(Console.ReadLine());
                foreach (var client in connectedClients)
                {
                    client.BeginSend(input, 0, input.Length, SocketFlags.None, SendCallback, client);
                }
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            int bytesSent = client.EndSend(ar);
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            // Accept new connection
            Socket listener = (Socket)ar.AsyncState;
            Socket client = listener.EndAccept(ar);
            connectedClients.Add(client);
            Console.WriteLine("New connection established.");

            // Create state of connection
            StateObject state = new StateObject();
            state.WorkSocket = client;

            // Assign color, and send to client.
            Console.ForegroundColor = (ConsoleColor)new Random().Next(1, 16);
            Console.WriteLine("Assigned color: " + Console.ForegroundColor);
            byte[] colorInfo = BitConverter.GetBytes((int)Console.ForegroundColor);
            state.Color = Console.ForegroundColor;
            Console.ResetColor();
            client.BeginSend(colorInfo, 0, colorInfo.Length, SocketFlags.None, SendColorInfoCallback, client);

            // Get username
            client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveUsernameCallback, state);
        }

        private static void ReceiveUsernameCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                // store the username
                state.Username = Encoding.UTF8.GetString(state.Buffer, 0, bytesRead);
                Console.WriteLine($"User connected is: {state.Username}");
            }

            // Begin receive messages from user.
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }


        private static void SendColorInfoCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            int bytesSent = client.EndSend(ar);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.WorkSocket;
            int bytesRead = 0;
            try
            {
                bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    Console.ForegroundColor = state.Color;
                    Console.WriteLine($"{state.Username}: {Encoding.UTF8.GetString(state.Buffer, 0, bytesRead)}");
                    Console.ResetColor();
                }
                client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveCallback, state);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                connectedClients.Remove(client);
                client.Close();
            }
        }
    }

    public class StateObject
    {
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        public Socket WorkSocket = null;
        public ConsoleColor Color = ConsoleColor.White;
        public string Username = null;
    }
}
