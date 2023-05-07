using System.Net.Sockets;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace ChatRoomClient
{
    class ChatRoomClient
    {
        private TcpClient client;
        private SslStream sslStream;
        private X509Certificate2 certificate;
        private Thread readThread;

        public ChatRoomClient(string serverAddress, int port)
        {
            client = new TcpClient();
            client.Connect(serverAddress, port);
            certificate = new X509Certificate2("../Server/server.pfx", "password");
            sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate);
            sslStream.AuthenticateAsClient("server", null, SslProtocols.Tls, false);
            readThread = new Thread(new ThreadStart(ReadMessages));
            readThread.Start();
        }

        private bool ValidateServerCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private void ReadMessages()
        {
            while (true)
            {
                byte[] buffer = new byte[2048];
                int bytesRead = 0;
                try
                {
                    bytesRead = sslStream.Read(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    string e = ex.Message;
                    break;
                }

                if (bytesRead == 0)
                {
                    Console.WriteLine("Disconnected from server.");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received message: {0}", message);
            }
        }

        public void SendMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            sslStream.Write(buffer, 0, buffer.Length);
            sslStream.Flush();
        }

        public void Close()
        {
            sslStream.Close();
            client.Close();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the IP address of the server: ");
            string? ipAddress = Console.ReadLine();

            Console.Write("Enter the port number of the server: ");
            int port;
            int.TryParse(Console.ReadLine(), out port);
            if (ipAddress != null)
            {
                ChatRoomClient client = new ChatRoomClient(ipAddress, port);
                while (true)
                {
                    Console.Write("> ");
                    string message = Console.ReadLine();
                    if (message == "exit")
                    {
                        break;
                    }
                    client.SendMessage(message);
                }

                client.Close();
            }
        }
    }
}
