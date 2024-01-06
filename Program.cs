using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using MyGameServer.Reader;
class Program
{
    static void Main(string[] args)
    {
        string fileName = "forgotten.otbm"; // Replace with your OTBM file path
        OTBMReader otbmReader = new OTBMReader(fileName);
        otbmReader.ReadOTBMFile();

        // Create an instance of your Game class (assuming it contains the Map and Houses data)
        Game game = new Game();

        // Create an instance of IOMapSerialize and load house items
        IOMapSerialize mapSerializer = new IOMapSerialize(game);
        mapSerializer.LoadHouseItems(game.Map);

        // Print all tiles on the map
        PrintAllTiles(game.Map);

        // Start the server after reading the OTBM file and loading house items
        SimpleTcpServer server = new SimpleTcpServer(1300);
        server.Start();
    }




    static void PrintAllTiles(Map map)
    {
        foreach (House house in map.Houses)
        {
            foreach (HouseTile tile in house.Tiles)
            {
                Console.WriteLine($"HouseTile - X: {tile.Position.X}, Y: {tile.Position.Y}, Z: {tile.Position.Z}");
                PrintItemsOnTile(tile);
            }
        }
    }

    static void PrintItemsOnTile(HouseTile tile)
    {
        foreach (Item item in tile.Items)
        {
            Console.WriteLine($"Item ID: {item.ID}");
        }
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