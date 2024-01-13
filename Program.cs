using System;
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
            string authToken = "ExpectedAuthToken"; // Replace with your expected authentication token
            byte[] buffer = new byte[1024];
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            string receivedToken = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (receivedToken == authToken)
            {
                Console.WriteLine("Client authenticated.");

                // Process login request and validate credentials
                bool isValidLogin = ProcessLoginRequest(networkStream);

                if (isValidLogin)
                {
                    // Continue handling the client, read and process data here
                    while (true)
                    {
                        bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("Client disconnected.");
                            break;
                        }

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Received data from client: {receivedData}");

                        // Handle the received data as needed
                    }
                }
                else
                {
                    Console.WriteLine("Login failed. Closing connection.");
                }
            }
            else
            {
                Console.WriteLine("Client failed authentication.");
            }
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


    private bool ProcessLoginRequest(NetworkStream networkStream)
    {
        // Read the login request (e.g., username and password) from the client
        byte[] buffer = new byte[1024];
        int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
        string loginRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Split the login request into username and password
        string[] loginInfo = loginRequest.Split(' ');
        if (loginInfo.Length != 3 || loginInfo[0] != "LOGIN")
        {
            // Invalid login request format
            return false;
        }

        string username = loginInfo[1];
        string password = loginInfo[2];

        // Validate the username and password (you should implement your validation logic here)
        bool isValid = ValidateCredentials(username, password);

        // Send a response to the client indicating whether the login was successful
        string response = isValid ? "LOGIN_SUCCESS" : "LOGIN_FAILURE";
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        networkStream.Write(responseBytes, 0, responseBytes.Length);

        return isValid;
    }

    private bool ValidateCredentials(string username, string password)
    {
        // Implement your validation logic here (e.g., check against a database)
        // Return true if valid, false if not
        return true; // For demonstration purposes, assuming it's valid
    }

}