using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace Client
{
    internal class Program
    {

        static Thread BroadcastThread;
        static bool readBroadcast = true;

        static Socket TCPSocket;
        static Thread TCPThread;
        static bool readTCP = true;

        static void Main(string[] args)
        {

            // Client Code
            // search for servers
            // get responses
            // output them
            // let the ui handle them
            // get input from the ui to see which one to connect to / re-search the servers
            // try connect
            // if successful then output something
            // communicate

            string user = "";

            BroadcastThread = new Thread(ReadBraudcast);
            BroadcastThread.Start();


            do
            {
                user = Console.ReadLine();
                user = user.Trim();
                if (user == "write")
                {
                    WriteBraudcast();
                }

                else if (user.Contains("send")) {
                    string dataToSend = user.Split(new[] { "send " }, StringSplitOptions.None)[1];
                    TCPSend(dataToSend);
                }

                else if (user.Contains("connect"))
                {
                    string ip = user.Split(' ')[1];
                    int port = int.Parse(user.Split(' ')[2]);

                    SetupTCPSocket(ip, port);

                    readBroadcast = false;

                    TCPThread = new Thread(TCPRead);
                    TCPThread.Start();

                }

            } while (user != "exit");

            TCPSocket.Close();

        }


        static void ReadBraudcast() {
            Console.WriteLine("Waiting For Server Response");
            byte[] buffer = new byte[1024];
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            IPEndPoint recieveEnd = new IPEndPoint(IPAddress.Any, 16790);
            socket.Bind(recieveEnd);

            EndPoint Remote = new IPEndPoint(IPAddress.Any, 0);

            while (readBroadcast)
            {
                int recv = socket.ReceiveFrom(buffer, ref Remote);
                Console.WriteLine("Message received from {0}:", Remote.ToString());
                string data = Encoding.ASCII.GetString(buffer, 0, recv);
                Console.WriteLine(data);
            }
            socket.Close();
        }

        static void SetupTCPSocket(string ipToConnect, int port) {
            IPAddress ip = IPAddress.Parse(ipToConnect);
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(ip, port);
            TCPSocket.Connect(ep);
            Console.WriteLine("Connected Successfully");
        }

        static void TCPSend(string data) {
            TCPSocket.Send(Encoding.UTF8.GetBytes(data));
        }

        static void TCPRead() {
            try
            {
                byte[] buffer = new byte[1024];

                while (readTCP)
                {
                    int recv = TCPSocket.Receive(buffer);
                    string data = Encoding.ASCII.GetString(buffer, 0, recv);
                    Console.WriteLine(data);
                }
            }
            catch(Exception e){
                Console.WriteLine(e);
                readTCP= false;
                TCPSocket.Close();
            }
        }


        static void WriteBraudcast()
        {
            int PORT = 9050;
            UdpClient udpClient = new UdpClient();
            var data = Encoding.UTF8.GetBytes("request servers");
            udpClient.Send(data, data.Length, "255.255.255.255", PORT);
        }

            static IPAddress ComputeBroadcastIP()  // (ip address | !subnet_mask) = broadcast Address
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = host.AddressList[0];
            
            foreach (IPAddress address in host.AddressList) {
                if (address.AddressFamily == AddressFamily.InterNetwork) { 
                    ipAddress = address;
                    break;
                }
            }

            IPAddress mask = GetSubnetMask(ipAddress);

            byte[] IPaddress = ipAddress.GetAddressBytes();
            byte[] SubnetMask = mask.GetAddressBytes();
            byte[] broadcastBytes = new byte[IPaddress.Length];

            for (int i = 0; i < broadcastBytes.Length; i++)
            {
                broadcastBytes[i] = (byte)(IPaddress[i] | ~SubnetMask[i]);
            }

            return new IPAddress(broadcastBytes);
        }

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }
    }

    
}



// System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();  -  add to UI code and display a error message if network is not availible (di not let the client create a new host (save memory and processsing))