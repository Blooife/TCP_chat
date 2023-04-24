using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


        
        string? UserName = null;
        string? host = null;
        TcpClient? Client = null;
        NetworkStream? Stream=null;

        Console.Write("Enter your name: ");
        UserName = Console.ReadLine();
        Console.WriteLine("Enter a server address: ");
        host = Console.ReadLine();
        Console.WriteLine("Enter a port: ");
        int port = Convert.ToInt32(Console.ReadLine());        
        
            Client = new TcpClient();
            try
            {
                Client.Connect(IPAddress.Parse(host), port);
                Stream = Client.GetStream();
                string Message = UserName;
                byte[] buffer = Encoding.Unicode.GetBytes(Message);
                Stream.Write(buffer, 0, buffer.Length);
                Thread RecieveThread = new Thread(new ThreadStart(RecieveMessage));
                RecieveThread.Start();
                Console.WriteLine("Welcome, {0}", UserName);
               // Thread SendThread = new Thread(new ThreadStart(SendMessage));
                SendMessage();
                //SendThread.Start();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
            
        
        

        void SendMessage()
        {
            Console.WriteLine("Enter a message: ");
            while(true)
            {
                string Message = Console.ReadLine();
                byte[] buffer = Encoding.Unicode.GetBytes(Message);
                Stream.Write(buffer, 0, buffer.Length);
            }
        }

         void RecieveMessage()
        {
            while(true)
            {
                try
                {
                    byte[] buffer = new byte[64];
                    StringBuilder Builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = Stream.Read(buffer, 0, buffer.Length);
                        Builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
                    } while (Stream.DataAvailable);

                    string Message = Builder.ToString();
                    //Console.WriteLine(Message);
                    FineOutput(Message);
                }
                catch
                {
                    Console.WriteLine("Connection is lost");
                    Console.ReadLine();
                    Disconnect();
                }
            }
        }
        
        void FineOutput(string msg)
        {
            var position = Console.GetCursorPosition(); 
            int left = position.Left;  
            int top = position.Top;   
            Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
            Console.SetCursorPosition(0, top);
            Console.WriteLine(msg);
            Console.SetCursorPosition(left, top + 1);
        }
        
         void Disconnect()
        {
            if(Stream != null)
            {
                Stream.Close();
            }
            if(Client != null)
            {
                Client.Close();
            }
            Environment.Exit(0);
        }
    

