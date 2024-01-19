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
using System.Reflection.PortableExecutable;
using TiledSharp;
using ZstdNet;
using MyGameServer;
using MyGameServer.player;
using OpenTibiaCommons.Domain;
using OpenTibiaCommons.IO;
using Newtonsoft.Json;

class Program
{





    public static void Main(string[] args)
    {

        var dbContext = new GameContext();

        // Load items definitions (if necessary)
        OtItems items = new OtItems();
        items.Load("items.otb");

        // Initialize OtMap
        OtMap map = new OtMap(items);

        // Read OTBM file
        using (OtFileReader otbmReader = new OtFileReader("Thais_War.otbm"))
        {
            map.Load(otbmReader, replaceTiles: true);
        }

        // Iterate through all tiles in the map
        foreach (var tile in map.Tiles)
        {

            //Console.WriteLine($"Tile at {tile.Location}:");
            foreach (var item in tile.Items)
            {
                if (item.Type.Id == 2471)
                {
                    Console.WriteLine($"Tile at {tile.Location}");
                    Console.WriteLine($" - Item: {item.Type.Id}{item.Type.Name}");
                }
            }
        }


        // Start the server after reading the OTBM file and loading house items
        SimpleTcpServer server = new SimpleTcpServer(1300, dbContext);
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



}


class SimpleTcpServer
{
    private TcpListener tcpListener;
    private GameContext dbContext;
    public SimpleTcpServer(int port, GameContext dbContext)
    {
        tcpListener = new TcpListener(IPAddress.Loopback, port);
        this.dbContext = dbContext;
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

    public string SerializePlayerToJson(Player player)
    {
        return JsonConvert.SerializeObject(player);
    }
    private Player FetchPlayerData()
    {
        // Implement your logic to fetch player data
        // For example, you might fetch it from the dbContext based on some criteria
        return dbContext.Players.FirstOrDefault(); // Example: Fetch the first player
    }
    private void SendDataToClient(NetworkStream networkStream, Player playerData)
    {
        string playerJson = JsonConvert.SerializeObject(new { player = playerData });
        byte[] jsonDataBytes = Encoding.UTF8.GetBytes(playerJson);

        // Prefix data with length
        byte[] lengthPrefix = BitConverter.GetBytes(jsonDataBytes.Length);
        byte[] dataToSend = new byte[lengthPrefix.Length + jsonDataBytes.Length];
        lengthPrefix.CopyTo(dataToSend, 2);
        jsonDataBytes.CopyTo(dataToSend, lengthPrefix.Length);

        networkStream.Write(dataToSend, 0, dataToSend.Length);
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

                var (isValidLogin, player) = ProcessLoginRequest(networkStream);

                if (isValidLogin) { 
                // Example: Fetch the player data (adjust according to your logic)
                Player playerData = FetchPlayerData(); // Implement this method based on your data retrieval logic
                    Console.WriteLine("playerData: ", playerData.Name);
                // Send the player data to the client
                SendDataToClient(networkStream, playerData);
                }
                else
                {
                    Console.WriteLine("USERNAME OR PASSWORD OR PLAYER DOSE NOT EXIST");
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
        //finally
        //{
        //    client.Close();
        //}
    }


    private void SendFramedResponse(NetworkStream networkStream, string response)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        byte[] lengthPrefix = BitConverter.GetBytes(responseBytes.Length);
        byte[] framedResponse = new byte[lengthPrefix.Length + responseBytes.Length];
        lengthPrefix.CopyTo(framedResponse, 0);
        responseBytes.CopyTo(framedResponse, lengthPrefix.Length);

        networkStream.Write(framedResponse, 0, framedResponse.Length);
    }




    private (bool isValidLogin, Player player) ProcessLoginRequest(NetworkStream networkStream)
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
            //return false;
            return (false, null);
        }

        string username = loginInfo[1];
        string password = loginInfo[2];

        Console.WriteLine(username + ":" + password);


        // If validation failed or player not found, return false and null

        LoginManager loginManager = new LoginManager(dbContext);
        //LoginManager loginManager = new LoginManager(dbContext);
        var (isValid, playerName) = loginManager.ValidateUserLogin(username, password);

        if (isValid)
        {
            Console.WriteLine("playerName: ", playerName);
            // Retrieve the Player object based on the playerName
            Player player = dbContext.Players.FirstOrDefault(p => p.Name == playerName);
            if (player != null)
            {
                Console.WriteLine($"{player.Name}");
                // Return true and the Player object
                return (true, player);
            }
        }


        // Send a response to the client indicating whether the login was successful
        string response = isValid ? "LOGIN_SUCCESS" : "LOGIN_FAILURE";
        SendFramedResponse(networkStream, response);
        Console.WriteLine("LOGIN_SUCCESS " , "LOGIN_FAILURE ",response);
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        networkStream.Write(responseBytes, 0, responseBytes.Length);

        return (false, null);
    }


}