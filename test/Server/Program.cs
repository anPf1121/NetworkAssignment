using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace ChatRoomServer
{
    class Program
    {
        static bool isRunning = false;
        static TcpListener tcpListener = default!;
        static List<TcpClient> clients = new List<TcpClient>();

        static void Main(string[] args)
        {
            RSA rsa = RSA.Create(2048);
            CertificateRequest req = new CertificateRequest("CN=server", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));

            Console.Title = "Chat Room Server";

            Console.Write("Enter the IP address of the server: ");
            string? ip = Console.ReadLine();
            if (ip != null)
            {
                IPAddress ipAddress = IPAddress.Parse(ip);

                Console.Write("Enter the port number of the server: ");
                int port;
                int.TryParse(Console.ReadLine(), out port);

                tcpListener = new TcpListener(ipAddress, port);

                Console.WriteLine($"Server started on {ipAddress}:{port}");

                byte[] certData = cert.Export(X509ContentType.Pfx, "password");
                File.WriteAllBytes("server.pfx", certData);

                X509Certificate2 serverCertificate = new X509Certificate2("server.pfx", "password");
                StartServer(serverCertificate);

                Console.ReadLine();
            }
        }
        static private bool ValidateClientCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static void StartServer(X509Certificate2 serverCertificate)
        {
            isRunning = true;
            tcpListener.Start();
            // Create a new thread to handle client communication
            while (isRunning)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClientCommunication(client, serverCertificate));
                clientThread.Start();
                clients.Add(client);
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} connected");
            }
        }
        static void HandleClientCommunication(TcpClient client, X509Certificate2 serverCertificate)
        {
            SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateClientCertificate));

            try
            {
                sslStream.AuthenticateAsServer(serverCertificate, true, SslProtocols.Tls, false);
                while (true)
                {
                    byte[] buffer = new byte[2048];
                    int bytesRead = sslStream.Read(buffer, 0, buffer.Length);
                    string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    BroadcastMessage(client, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                lock (clients)
                {
                    clients.Remove(client);
                }
                client.Close();
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} disconnected");
            }
        }

        static void BroadcastMessage(TcpClient senderClient, string message)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    if (client != senderClient)
                    {
                        SslStream sslStream = new SslStream(client.GetStream(), false);
                        try
                        {
                            sslStream.Write(buffer, 0, buffer.Length);
                            sslStream.Flush();
                        }
                        catch (Exception ex)
                        {
                            string e = ex.Message;
                        }
                    }
                }
            }
            Console.WriteLine($"{senderClient.Client.RemoteEndPoint}: {message}");
        }
    }
}