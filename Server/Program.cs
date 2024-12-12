using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class ChatServer
{
    private TcpListener tcpListener;
    private List<TcpClient> clients;

    public ChatServer(string ip, int port)
    {
        tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        clients = new List<TcpClient>();
    }

    public void Start()
    {
        tcpListener.Start();
        Console.WriteLine("Server is running...");
        
        // Chờ các kết nối từ client
        while (true)
        {
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            clients.Add(tcpClient);
            Console.WriteLine("A client connected.");

            // Xử lý mỗi client trong một thread riêng
            Thread clientThread = new Thread(HandleClient!);
            clientThread.Start(tcpClient);
        }
    }

    private void HandleClient(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        NetworkStream stream = tcpClient.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (true)
        {
            try
            {
                // Đọc tin nhắn từ client
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine(message);

                // Gửi tin nhắn cho tất cả client khác
                BroadcastMessage(message, tcpClient);
            }
            catch
            {
                break;
            }
        }

        // Khi client ngắt kết nối
        Console.WriteLine("A client disconnected.");
        clients.Remove(tcpClient);
        tcpClient.Close();
    }

    private void BroadcastMessage(string message, TcpClient sender)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        foreach (var client in clients)
        {
            if (client != sender)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
        }
    }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ChatServer server = new ChatServer("127.0.0.1", 5000);
        server.Start();
    }
}