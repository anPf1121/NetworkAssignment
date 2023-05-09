using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;


namespace ChatRoomServer
{
    class Program
    {
        static TcpListener server;
        static List<TcpClient> clients = new List<TcpClient>();
        static AesManaged aes = new AesManaged();
        static readonly byte[] key = aes.Key; // Khóa bí mật
        static readonly byte[] iv = aes.IV; // Vector khởi tạo


        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");
            server = new TcpListener(IPAddress.Any, 8888);
            server.Start();

            Console.WriteLine("Secret key: " + Convert.ToBase64String(key));
            Console.WriteLine("Secret IV: " + Convert.ToBase64String(iv));


            while (true)
            {
                Console.WriteLine("Waiting for client...");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connected!");

                // Add the client to the list
                clients.Add(client);

                // Start a new thread to handle the client
                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            // Send the shared key and IV to the client
            stream.Write(key, 0, key.Length);
            stream.Write(iv, 0, iv.Length);
            while (true)
            {
                try
                {
                    // Đọc tin nhắn đã nhận từ client
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    // Giải mã tin nhắn
                    byte[] decryptedBytes = buffer;
                    string message = Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);

                    // Phát tin nhắn đến tất cả client khác
                    BroadcastMessage(message);

                    Console.WriteLine("Message received: " + message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client disconnected: " + ex.Message);
                    clients.Remove(client);
                    client.Close();
                    return;
                }
            }
        }

        static void BroadcastMessage(string message)
        {
            foreach (TcpClient client in clients)
            {
                NetworkStream stream = client.GetStream();

                // Mã hóa tin nhắn
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] encryptedBytes = messageBytes;

                // Gửi tin nhắn đã mã hóa đến client
                stream.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }
        static public byte[] AesEncrypt(byte[] message)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] encryptedMessage = encryptor.TransformFinalBlock(message, 0, message.Length);
                return encryptedMessage;
            }
        }

        static public byte[] AesDecrypt(byte[] encryptedMessage)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] decryptedMessage = decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);
                return decryptedMessage;
            }
        }
    }
}
