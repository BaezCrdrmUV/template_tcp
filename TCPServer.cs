using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace tcp_com
{
    public class TCPServer
    {
        public TcpListener listener { get; set; }
        public bool acceptFlag { get; set; }
        public List<Message> messages { get; set; }
        public List<int> threadsIds { get; set; }
        public bool hasOpenedThreads;

        public TCPServer(string ip, int port, bool start = false)
        {
            messages = new List<Message>();
            threadsIds = new List<int>();
            hasOpenedThreads = false;

            IPAddress address = IPAddress.Parse(ip);
            this.listener = new TcpListener(address, port);

            if(start == true)
            {
                listener.Start();
                Console.WriteLine("Servidor iniciado en la dirección {0}:{1}",
                    address.MapToIPv4().ToString(), port.ToString());
                acceptFlag = true;
            }
        }

        public void Listen()
        {
            if(listener != null && acceptFlag == true)
            {
                int id = 0;
                Thread watch = new Thread(new ThreadStart(watchOpenedThreads));
                watch.Start();

                while(true)
                {
                    Console.WriteLine("Esperando conexión del cliente...");
                    if(hasOpenedThreads == true && threadsIds.Count == 0) break;

                    try
                    {
                        var clientSocket = listener.AcceptSocket();
                        Console.WriteLine("Cliente aceptado");

                        Thread thread = new Thread(new ParameterizedThreadStart(HandleCommunication));
                        thread.Start(new ThreadParams(clientSocket, id));
                        threadsIds.Add(id);
                        id++;
                        hasOpenedThreads = true;
                    }
                    catch (System.Exception)
                    {
                        
                    }
                }

                watch.Interrupt();
                return;
            }
        }

        public void HandleCommunication(Object obj)
        {
            ThreadParams param = (ThreadParams)obj;
            Socket client = param.obj;

            if(client != null)
            {
                Console.WriteLine("Cliente conectado. Esperando datos");
                string msg = "";
                Message message = new Message();

                while(message != null && !message.MessageString.Equals("bye"))
                {
                    try
                    {
                        // Enviar un mensaje al cliente
                        byte[] data = Encoding.UTF8.GetBytes("Envía datos. Envía \"bye\" para terminar");
                        client.Send(data);

                        // Escucha por nuevos mensajes
                        byte[] buffer = new byte[1024];
                        client.Receive(buffer);

                        msg = Encoding.UTF8.GetString(buffer);
                        Console.WriteLine(msg);
                        message = JsonConvert.DeserializeObject<Message>(msg);
                        messages.Add(message);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Exception", msg, ex.Message);
                    }
                }
                Console.WriteLine("Cerrando conexión");
                client.Dispose();
                foreach (var item in threadsIds)
                {
                    Console.WriteLine(item);
                }
                Console.WriteLine("------------");
                threadsIds.Remove(param.id);
                foreach (var item in threadsIds)
                {
                    Console.WriteLine(item);
                }
                Thread.CurrentThread.Join();
            }
        }

        public void watchOpenedThreads()
        {
            while(true)
            {
                if(hasOpenedThreads == true && threadsIds.Count == 0)
                {
                    Console.WriteLine("Deja de escuchar");
                    listener.Stop();
                    listener = null;
                    break;
                }
            }
            Console.WriteLine("Opened messages");
            displayMessages();
            Thread.CurrentThread.Join();
        }

        public void displayMessages()
        {
            Console.WriteLine("Mensajes en la colección");
            foreach (Message msg in messages)
            {
                Console.WriteLine("{0} >> {1}", msg.User, msg.MessageString);
            }
        }
    }

    public class ThreadParams
    {
        public Socket obj { get; set; }
        public int id { get; set; }

        public ThreadParams(Socket obj, int id)
        {
            this.obj = obj;
            this.id = id;
        }
    }
}