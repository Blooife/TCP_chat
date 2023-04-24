using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

ServerObject? Server = null;
Thread ListenThread;
ThreadPool.SetMinThreads(2, 2);
ThreadPool.SetMaxThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 4);
try
{
    Server = new ServerObject();
    ListenThread = new Thread(new ThreadStart(Server.Listen));
    ListenThread.Start();
}
catch(Exception ex)
{
    if(Server != null)
        Server.Disconnect();
    Console.WriteLine(ex.Message);
}
public class ServerObject
    { 
        TcpListener? Listener=null;
        List<ClientObject> Clients = new List<ClientObject>();
        IPAddress? ip;
        public int port;
        
        public ServerObject()
        {
            bool busy = true;
            while (busy)
            {
                Console.WriteLine("Enter a server ip address");
                string? ipStr = Console.ReadLine();
                
                while (!IPAddress.TryParse(ipStr, out ip))
                {
                    Console.WriteLine("IP is incorrect. Try Again."); 
                    ipStr = Console.ReadLine();
                }
                Console.WriteLine("Enter a port");
                string? portStr = Console.ReadLine();
                port = Convert.ToInt32(portStr);
                busy = CheckPortIsBusy(ip, port);
                if(busy)
                    Console.WriteLine("Port is busy, try again.");
                else
                {
                    try
                    {
                        this.Listener = new TcpListener(ip, port);
                    }
                    catch(SocketException)
                    {
                        Console.WriteLine("There was a mistake. Try again");
                        busy = true;
                    }
                }
            }
        }
        
        protected bool CheckPortIsBusy(IPAddress addr, int port)
        {
            var colPortBusy = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners();
            foreach (var endPoint in colPortBusy)
            {
                if (endPoint.Address.Equals(addr) && endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }

        protected internal void AddConection(ClientObject clientObject)
        {
            Clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id)
        {
            foreach (var cl in Clients)
            {
                if (cl.Id == id)
                {
                    Clients.Remove(cl);
                    cl.Close();
                    break;
                }
            }
        }
        protected internal void Listen()
        {
            try
            {
                Listener = new TcpListener(ip, port);
                Listener.Start();
                Console.WriteLine(Listener.LocalEndpoint.ToString());
                Console.WriteLine($"Server {ip}:{port} started. Waiting for connections...");

                while(true)
                {
                    TcpClient Client = Listener.AcceptTcpClient();

                    ClientObject CO = new ClientObject(Client, this);
                    Thread ClientThread = new Thread(new ThreadStart(CO.Process));
                    ClientThread.Start();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }
        protected internal void BroadcastMessage(string message, string id)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);
            for(int i = 0; i < Clients.Count; i++)
            {
                if(Clients[i].Id != id)
                {
                    Clients[i].Stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        protected internal void Disconnect()
        {
            Listener.Stop();

            for(int i = 0; i < Clients.Count; i++)
            {
                Clients[i].Close();
            }
            Environment.Exit(0);
        }
    }


public class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal NetworkStream? Stream { get; private set; } = null;
        string UserName;
        TcpClient Client;
        ServerObject Server;

        public ClientObject(TcpClient client, ServerObject server)
        {
            Id = Guid.NewGuid().ToString();
            Client = client;
            Server = server;
            server.AddConection(this);
        }

        public void Process()
        {
            try
            {
                Stream = Client.GetStream();
                string Message = GetMessage();
                UserName = Message;

                Message = UserName + " joined chat";
                Server.BroadcastMessage(Message, this.Id);
                Console.WriteLine(Message);
                while(true)
                {
                    try
                    {
                        Message = GetMessage();
                        Message = String.Format("{0}: {1}", UserName, Message);
                        Console.WriteLine(Message);
                        Server.BroadcastMessage(Message, this.Id);
                    }
                    catch
                    {
                        Message = String.Format("{0}: left chat...", UserName);
                        Console.WriteLine(Message);
                        Server.BroadcastMessage(Message, this.Id);
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Server.RemoveConnection(this.Id);
                Close();
            }
        }
        string GetMessage()
        {
            byte[] buffer = new byte[64];
            StringBuilder Builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = Stream.Read(buffer, 0, buffer.Length);
                Builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
            } while (Stream.DataAvailable);

            return Builder.ToString(); ;
        }

        protected internal void Close()
        {
            if(Stream != null)
            {
                Stream.Close();
            }
            if(Client != null)
            {
                Client.Close();
            }
        }
    }

    