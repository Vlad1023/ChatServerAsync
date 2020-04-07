using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace ChatServer
{
    class Program
    {
        static List<ClientInfo> clientsList = new List<ClientInfo>();
        class ClientInfo
        {
            string name;
            TcpClient client;
            public ClientInfo(TcpClient client)
            {
                this.name = null;
                this.client = client;
            }
            public string Name { get { return name; } set { name = value; } }
            public TcpClient Client { get { return client; } set { client = value; } }
        }
        static void Main(string[] args)
        {
            var localAddr = IPAddress.Parse("127.0.0.1");
            var port = 13000;

            try
            {
                var server = new TcpListener(localAddr, port);
                server.Start();
                Console.WriteLine("--Chat server started--");
                while (true)
                {
                    server.BeginAcceptTcpClient((IAsyncResult ar) => { addUser(ar); }, server);
                    for (int i = 0; i < clientsList.Count; i++)
                    {
                        if (clientsList[i].Client.Available > 0)
                            Process(clientsList.IndexOf(clientsList[i]));
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }

        static void addUser(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            var client = listener.EndAcceptTcpClient(ar);
            Console.WriteLine("Accept connection from {0}", client.Client.RemoteEndPoint);
            clientsList.Add(new ClientInfo(client));
        }
        static bool checkIfClientExist(string name, List<ClientInfo> clientsNetworks)
        {
            bool ifExist = false;
            foreach (var item in clientsNetworks)
            {
                if (item.Name != null && item.Name.Equals(name)) ifExist = true;
            }
            return ifExist;
        }
        static void Responce(NetworkStream stream, string message)
        {
            byte[] writeData = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(writeData, 0, writeData.Length);
        }
        static string Request(NetworkStream stream)
        {
            byte[] readData = new byte[64];
            int readbytes = stream.Read(readData, 0, readData.Length);
            var readString = Encoding.UTF8.GetString(readData, 0, readbytes);
            return readString;
        }
        static void Process(int index)
        {
            string comString = Request(clientsList[index].Client.GetStream());
            int lastInd = comString.LastIndexOf("\n");
            if (clientsList[index].Name == (null) && lastInd == -1)
            {
                if (!checkIfClientExist(comString, clientsList))
                {
                    clientsList[index].Name = comString;
                    Responce(clientsList[index].Client.GetStream(), "Connected");
                }
                else
                {
                    Responce(clientsList[index].Client.GetStream(), "Sorry, this name is occupied. Reconnect and try again");
                    clientsList.Remove(clientsList[index]);
                    clientsList[index].Client.Close();
                }
            }
            else
            {
                if (comString.Contains(".quit"))
                {
                    Responce(clientsList[index].Client.GetStream(), "Buy, see you next time");
                    clientsList[index].Client.Close();
                    clientsList.Remove(clientsList[index]);

                }
                else
                {
                    string name = comString.Substring(lastInd + 1);
                    string message = comString.Substring(0, lastInd);
                    broadcastMessage(index, ref clientsList, String.Format("{0}: {1}", name, message));
                }
            }
        }
        static void broadcastMessage(int index, ref List<ClientInfo> clientsList, string message)
        {
            for (int i = 0; i < clientsList.Count; i++)
            {
                if (index != i) Responce(clientsList[i].Client.GetStream(), message);
            }
        }
    }
}
