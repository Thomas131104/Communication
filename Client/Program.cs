using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class ChatClient
{
    private TcpClient tcpClient;
    private NetworkStream stream;
    private string username;
    private Thread? receiveThread;
    private CancellationTokenSource cancellationTokenSource; // Dùng để gửi tín hiệu dừng thread

    public ChatClient(string ip, int port, string username)
    {
        this.username = username;
        tcpClient = new TcpClient(ip, port);
        stream = tcpClient.GetStream();
        cancellationTokenSource = new CancellationTokenSource();  // Khởi tạo CancellationTokenSource
    }

    public void Start()
    {
        // Tạo một thread để nhận tin nhắn
        receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start(cancellationTokenSource.Token);  // Truyền CancellationToken cho thread

        // Gửi tin nhắn từ bàn phím
        while (true)
        {
            string? message = Console.ReadLine();
            if (message is null)
                break;  // Thoát khi người dùng không nhập gì

            SendMessage(message);
        }

        // Khi người dùng kết thúc, tắt kết nối và thread nhận
        Stop();
    }

    private void SendMessage(string message)
    {
        string fullMessage = $"{username}: {message}";
        byte[] data = Encoding.ASCII.GetBytes(fullMessage);
        stream.Write(data, 0, data.Length);
    }

    private void ReceiveMessages(object? tokenObj)
    {
        if (tokenObj is CancellationToken token)  // Kiểm tra token trước khi unbox
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (!token.IsCancellationRequested)  // Kiểm tra token để dừng thread
            {
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Giả sử định dạng tin nhắn là "username: message"
                    string[] parts = message.Split(new[] { ": " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        string sender = parts[0];  // Tên người gửi
                        string content = parts[1];  // Nội dung tin nhắn

                        Console.WriteLine($"{sender}: {content}");  // In người gửi và tin nhắn
                    }
                    else
                    {
                        // Nếu tin nhắn không có định dạng hợp lệ
                        Console.WriteLine("Invalid message format received.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while receiving message: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine("Receiving thread stopped.");
        }
        else
        {
            Console.WriteLine("Failed to receive valid CancellationToken.");
        }
    }


    public void Stop()
    {
        // Gửi tín hiệu dừng thread nhận tin nhắn
        cancellationTokenSource.Cancel();

        // Đợi cho đến khi thread nhận kết thúc
        receiveThread?.Join();

        // Đóng kết nối
        stream.Close();
        tcpClient.Close();
        Console.WriteLine("Ngắt kết nối với máy chủ.");
    }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Write("Nhập tên của bạn: ");
        string? username = Console.ReadLine();
        if (username is null)
        {
            Console.WriteLine("Username is required.");
            return;
        }
        
        try
        {
            ChatClient client = new ChatClient("127.0.0.1", 5000, username);
            client.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to server: {ex.Message}");
        }
    }
}
