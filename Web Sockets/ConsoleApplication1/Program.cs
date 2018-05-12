using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ConsoleApplication1
{
    class Program
    {
        private static Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static byte[] buff = new byte[2048];
        static void Main(string[] args)
        {
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, 6950));

            listenSocket.Listen(10);

            listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

            Console.ReadLine();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket clientSocket = null;
            try { clientSocket = listenSocket.EndAccept(ar); }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }

            if (clientSocket != null)
            {
                Console.WriteLine("Client " + clientSocket.RemoteEndPoint + " connected");
                clientSocket.BeginReceive(buff, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
            }
            
            if(listenSocket != null)
                listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket clientSocket = ar.AsyncState as Socket;
            int receivedBytes;

            try { receivedBytes = clientSocket.EndReceive(ar); }
            catch (ObjectDisposedException) { return; }
            catch (SocketException)
            {
                Console.WriteLine("Client " + clientSocket.RemoteEndPoint + " forcefully disconnected");
                clientSocket.Disconnect(true);
                clientSocket.Close();
                return;
            }

            byte[] receivedData = new byte[receivedBytes];
            Array.Copy(buff, receivedData, receivedBytes);
            string text = Encoding.UTF8.GetString(receivedData);
            Console.WriteLine("From: " + clientSocket.RemoteEndPoint + ". Text received: " + text);
            string response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine +
                "Upgrade: websocket" + Environment.NewLine +
                "Connection: Upgrade" + Environment.NewLine;
            try { clientSocket.Send(Encoding.UTF8.GetBytes(response), 0, Encoding.UTF8.GetBytes(response).Length, SocketFlags.None); }
            catch (SocketException) { clientSocket.Close(); }

            if (clientSocket != null)
                try { clientSocket.BeginReceive(buff, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket); }
                catch (SocketException e) { Console.WriteLine(e); }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Socket clientSocket = ar.AsyncState as Socket;
            int sentBytes;

            try { sentBytes = clientSocket.EndSend(ar); }
            catch (ObjectDisposedException) { return; }
            catch (SocketException)
            {
                Console.WriteLine("Client " + clientSocket.RemoteEndPoint + " forcefully disconnected");
                clientSocket.Disconnect(true);
                clientSocket.Close();
                return;
            }
        }
    }
}
