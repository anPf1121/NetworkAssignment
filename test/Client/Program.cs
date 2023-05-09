using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace ChatRoomClient
{
    class Program
    {
        static byte[] key = default!;
        static byte[] iv = default!;
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting to server...");
            TcpClient client = new TcpClient("localhost", 8888);
            Console.WriteLine("Connected to server!");

            NetworkStream stream = client.GetStream();

            // Receive the shared key and IV from the server
            key = new byte[32];
            stream.Read(key, 0, key.Length);

            iv = new byte[16];
            stream.Read(iv, 0, iv.Length);

            Console.WriteLine("Secret key: " + Convert.ToBase64String(key));
            Console.WriteLine("Secret IV: " + Convert.ToBase64String(iv));

            // Start a new thread to read messages from the server
            Thread t = new Thread(new ParameterizedThreadStart(ReadMessages));
            t.Start(stream);

            while (true)
            {
                Console.Write("> ");
                // Read input from the user
                string message = Console.ReadLine();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] encrypted = messageBytes;

                // Send the message to the server
                stream.Write(encrypted, 0, encrypted.Length);
            }
        }

        static void ReadMessages(object obj)
        {
            NetworkStream stream = (NetworkStream)obj;
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    Console.Write("> ");
                    // Read the incoming message from the server
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    byte[] decrypted = buffer;
                    string message = Encoding.UTF8.GetString(decrypted);

                    Console.WriteLine("Message received: " + message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server disconnected: " + ex.Message);
                    break;
                }
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
