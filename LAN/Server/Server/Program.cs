using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

namespace Server
{
    internal class Program
    {
        static IPAddress localIP;
        static Thread thread;
        static int TCP_Port = 9054;
        static bool ListenBroadCast = true;

        static void Main(string[] args)
        {
            // listen to the broadcast ip
            // after two connect responses set up listener
            // send a connect request to the client via broadcast / client ip
            // wait for connect confirmation then write code to re-transmit the messages sent
            
            //getLocalIp();
            thread = new Thread(listenBroadcast); // set up the broadcast loop that will broadcast its information
            thread.Start();
            TCPConnection();
        }

        static void getLocalIp() {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = host.AddressList[0];

            foreach (IPAddress address in host.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = address;
                    break;
                }
            }
        }


        static void sendBroadcast(string data) 
        
        {
            int PORT = 16790;
            UdpClient udpClient = new UdpClient();
            var Bytedata = Encoding.UTF8.GetBytes(data);
            udpClient.Send(Bytedata, Bytedata.Length, "255.255.255.255", PORT);
            udpClient.Close();
        }

        static void TCPConnection() {
            Socket TCPClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, TCP_Port);
            TCPClient.Bind(ep);
            TCPClient.Listen(1000);
            TCPClient.BeginAccept(new AsyncCallback(AcceptCallback), TCPClient);
        }


        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Handle the connection
            sendBroadcast("Server No Longer Availible");
            
            ListenBroadCast = false;

            handler.Send(Encoding.ASCII.GetBytes("Hello, World!"));
        }

        static void listenBroadcast() { 

            byte[] buffer = new byte[1024];

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint recieveEnd = new IPEndPoint(IPAddress.Any, 9050);
            socket.Bind(recieveEnd);

            Console.WriteLine("Waiting for client");

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)(sender);
            while (ListenBroadCast)
            {
                int recv = socket.ReceiveFrom(buffer, ref Remote);
                Console.WriteLine("Message received from {0}:", Remote.ToString());
                string data = Encoding.ASCII.GetString(buffer, 0, recv);
                Console.WriteLine(data);

                if (data == "request servers")
                {
                    // send response server name and other info and ip
                    sendBroadcast("Server: " + localIP.ToString() + " PORT: 9054");
                }
            }

            socket.Close();
        }
    }
}
