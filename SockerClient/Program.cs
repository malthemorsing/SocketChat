using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter your username: ");
            string username = Console.ReadLine();

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(new IPEndPoint(IPAddress.Loopback, 1604));

            // send username
            byte[] usernameBuffer = Encoding.UTF8.GetBytes(username);
            client.Send(usernameBuffer);

            // receive color
            byte[] colorBuffer = new byte[4];
            client.Receive(colorBuffer);
            ConsoleColor color = (ConsoleColor)BitConverter.ToInt32(colorBuffer, 0);
            Console.ForegroundColor = color;

            // receive messages
            StateObject state = new StateObject();
            state.WorkSocket = client;
            client.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, new AsyncCallback(ReceiveCallback), state);

            // send messages
            while (true)
            {
                string message = Console.ReadLine();
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                client.Send(buffer);
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.WorkSocket;
            int bytesRead = 0;
            try
            {
                bytesRead = client.EndReceive(ar);
                Console.WriteLine(Encoding.UTF8.GetString(state.Buffer, 0, bytesRead));
                client.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                client.Close();
            }
        }
    }

    public class StateObject
    {
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        public Socket WorkSocket = null;
    }
}
