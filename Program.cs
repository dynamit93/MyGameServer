﻿using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;
using MyGameServer.Reader;
using System.Reflection.PortableExecutable;
using TiledSharp;
using ZstdNet;

class Program
{


    public static Dictionary<int, string> ParseItemsXml(string filePath)
    {
        var items = new Dictionary<int, string>();

        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);

        XmlNodeList itemList = doc.DocumentElement.SelectNodes("/items/item");
        foreach (XmlNode item in itemList)
        {
            
            string element = item.OuterXml.ToString();
            if (element.Contains("fromid="))
            {
                var fromIdRegex = new Regex(@"fromid=""(\d+)""");
                var toIdRegex = new Regex(@"toid=""(\d+)""");
                var nameRegex = new Regex(@"name=""([^""]*)""");

                var fromIdMatch = fromIdRegex.Match(element);
                var toIdMatch = toIdRegex.Match(element);
                var nameMatch = nameRegex.Match(element);

                if (fromIdMatch.Success && toIdMatch.Success && nameMatch.Success)
                {
                    int fromId = int.Parse(fromIdMatch.Groups[1].Value);
                    int toId = int.Parse(toIdMatch.Groups[1].Value);
                    string name = nameMatch.Groups[1].Value;
                    
                    for(int i = fromId; i <= toId; i++)
                    {
                        items[i] = name;
                    }
                }
                continue;

            }
            if (element.Contains("<article")) continue;
            if (element.Contains("<item id="))
            {
                var idRegex = new Regex(@"id=""(\d+)""");
                var nameRegex = new Regex(@"name=""([^""]*)""");

                var idMatch = idRegex.Match(element);
                var nameMatch = nameRegex.Match(element);

                if (idMatch.Success && nameMatch.Success)
                {
                    int id = int.Parse(idMatch.Groups[1].Value);
                    string name = nameMatch.Groups[1].Value;
                    items[id] = name;
                }
                continue;
            }
        }

        return items;
    }


    public static void Main(string[] args)
    {
        string tmxFilePath = @"C:\Users\dennis\source\repos\MyGameServer\Data\World\map.tmx";

        if (!File.Exists(tmxFilePath))
        {
            Console.WriteLine("TMX file not found.");
            return;
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(tmxFilePath);

        XmlNodeList chunkList = doc.DocumentElement.SelectNodes("//layer/data/chunk");

        foreach (XmlNode chunkNode in chunkList)
        {
            string base64CompressedData = chunkNode.InnerText.Trim();

            try
            {
                byte[] decompressedData = DecompressBase64ZstdData(base64CompressedData);

                // Process 'decompressedData' as needed
                PrintDecompressedData(decompressedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decompression: {ex.Message}");
            }
        }


        // Start the server after reading the OTBM file and loading house items
        SimpleTcpServer server = new SimpleTcpServer(1300);
        server.Start();
    }
    private static byte[] DecompressBase64ZstdData(string base64CompressedData)
    {
        byte[] compressedData = Convert.FromBase64String(base64CompressedData);

        using (var decompressor = new Decompressor())
        {
            return decompressor.Unwrap(compressedData);
        }
    }

    private static void PrintDecompressedData(byte[] data)
    {
        StringBuilder sb = new StringBuilder();

        // Limit the number of bytes to print to avoid overwhelming the console
        int bytesToPrint = Math.Min(data.Length, 100);
        for (int i = 0; i < bytesToPrint; i++)
        {
            sb.AppendFormat("{0:X2} ", data[i]);
        }

        Console.WriteLine("Decompressed Data (Hex): " + sb.ToString());
    }

}


class SimpleTcpServer
{
    private TcpListener tcpListener;

    public SimpleTcpServer(int port)
    {
        tcpListener = new TcpListener(IPAddress.Loopback, port);
    }

    public void Start()
    {
        tcpListener.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = tcpListener.AcceptTcpClient();
            Console.WriteLine("Client connected.");
            ThreadPool.QueueUserWorkItem(HandleClient, client);
        }
    }



    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream networkStream = client.GetStream();

        try
        {
            //using (SslStream sslStream = new SslStream(networkStream, false))
            //{
            //    // Load the server certificate
            //    string certificatePath = "path_to_your_certificate.pfx";
            //    string certificatePassword = "your_certificate_password";
            //    X509Certificate serverCertificate = new X509Certificate2(certificatePath, certificatePassword);

            //    // Authenticate as the server
            //    sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, SslProtocols.Tls12, checkCertificateRevocation: true);

            // Read authentication token using sslStream
            string authToken = "ExpectedAuthToken";
            byte[] buffer = new byte[1024];
            //int bytesRead = sslStream.Read(buffer, 0, buffer.Length);
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            string receivedToken = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (receivedToken == "ExpectedAuthToken")
                {
                    Console.WriteLine("Client authenticated.");
                    // Continue handling client
                }
                else
                {
                    Console.WriteLine("Client failed authentication.");
                    client.Close();
                    return;
                }

                // Further communication using sslStream
                // ...
            //}
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }



}