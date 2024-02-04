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
using System.Drawing;
using SharpTibiaProxy.Domain;
using MyGameServer.player;
using ClientCreature = MyGameServer.player.ClientCreature;
using Newtonsoft.Json.Linq;

class Program
{
    public static List<PlayerGame> players = new List<PlayerGame>(); // Declare and initialize the players list


    public static OtMap LoadMap()
    {
        // Load items definitions
        OtItems items = new OtItems();
        items.Load("items.otb");

        // Initialize OtMap
        OtMap map = new OtMap(items);

        // Read OTBM file
        using (OtFileReader otbmReader = new OtFileReader("test.otbm"))
        {
            map.Load(otbmReader, replaceTiles: true);
        }

        return map;
    }

    public static void Main(string[] args)
    {

        var dbContext = new GameContext();
        OtMap map = LoadMap();
        //// Load items definitions (if necessary)
        //OtItems items = new OtItems();
        //items.Load("items.otb");

        //// Initialize OtMap
        //OtMap map = new OtMap(items);

        //// Read OTBM file
        //using (OtFileReader otbmReader = new OtFileReader("Thais_War.otbm"))
        //{
        //    map.Load(otbmReader, replaceTiles: true);
        //}

        //// Iterate through all tiles in the map
        //foreach (var tile in map.Tiles)
        //{

        //    //Console.WriteLine($"Tile at {tile.Location}:");
        //    foreach (var item in tile.Items)
        //    {
        //        if (item.Type.Id == 2471)
        //        {
        //            Console.WriteLine($"Tile at {tile.Location}");
        //            Console.WriteLine($" - Item: {item.Type.Id}{item.Type.Name}");
        //        }
        //    }
        //}




        // Start the server after reading the OTBM file and loading house items
        SimpleTcpServer server = new SimpleTcpServer(1300, dbContext,map);
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
    public List<PlayerGame> Players { get; } = new List<PlayerGame>();
    private GameWorld gameWorld;
    private TcpListener tcpListener;
    private GameContext dbContext;
    private OtMap map;
    public SimpleTcpServer(int port, GameContext dbContext, OtMap map)
    {
        tcpListener = new TcpListener(IPAddress.Loopback, port);
        this.dbContext = dbContext;
        this.map = map;
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

    private const int BufferSize = 1024;
    public void SendMapDataToClient(NetworkStream networkStream, OtMap mapData, Player playerData)
    {
        try
        {
            var filteredMapTiles = FilterMapData(mapData, playerData);

            var mapDescriptionPacket = new
            {
                Type = "MapDescription",
                Tiles = filteredMapTiles.Select(tile => new
                {
                    Location = new { tile.Location.X, tile.Location.Y, tile.Location.Z },
                    Items = tile.Items.Select(item => new { Id = item.Id, Name = item.Name }).ToList(),
                    Ground = tile.Ground != null ? new { Id = tile.Ground.Id, Name = tile.Ground.Name } : null
                }).ToList()
            };

            string jsonPacket = JsonConvert.SerializeObject(mapDescriptionPacket);
            byte[] packetBytes = Encoding.UTF8.GetBytes(jsonPacket);
            networkStream.Write(packetBytes, 0, packetBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public void SendHeartbeatToClient(NetworkStream networkStream)
    {
        try
        {
            var heartbeatPacket = new
            {
                Type = "Heartbeat",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            string jsonPacket = JsonConvert.SerializeObject(heartbeatPacket);
            byte[] packetBytes = Encoding.UTF8.GetBytes(jsonPacket);
            networkStream.Write(packetBytes, 0, packetBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }




    private List<TileData> FilterMapData(OtMap mapData, Player playerData)
    {
        var filteredTiles = new List<TileData>();

        // Define the visibility range around the player
        int rangeX = 20; // Horizontal visibility range
        int rangeY = 20; // Vertical visibility range
        int rangeZ = 2;  // Depth visibility range

        // Calculate the bounds based on the player's position
        int minX = playerData.PosX - rangeX;
        int maxX = playerData.PosX + rangeX;
        int minY = playerData.PosY - rangeY;
        int maxY = playerData.PosY + rangeY;
        int minZ = Math.Max(playerData.PosZ - rangeZ, 0); // Assuming Z cannot be negative
        int maxZ = playerData.PosZ + rangeZ;

        foreach (var tile in mapData.Tiles)
        {
            if (tile.Location.X >= minX && tile.Location.X <= maxX &&
                tile.Location.Y >= minY && tile.Location.Y <= maxY &&
                tile.Location.Z >= minZ && tile.Location.Z <= maxZ)
            {
                // Assuming `Tile` has a similar structure to what you described
                var tileData = new TileData
                {
                    Location = new MapLocation
                    {
                        X = tile.Location.X,
                        Y = tile.Location.Y,
                        Z = tile.Location.Z
                    },
                    Ground = tile.Ground != null ? new GroundData { Id = tile.Ground.Type.Id, Name = tile.Ground.Type.Name } : null
                };

                foreach (var item in tile.Items)
                {
                    tileData.Items.Add(new ItemData { Id = item.Type.Id, Name = item.Type.Name });
                }

                filteredTiles.Add(tileData);
            }
        }

        return filteredTiles;
    }






    private bool IsValidJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) { return false; }
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
            (strInput.StartsWith("[") && strInput.EndsWith("]"))) // For array
        {
            try
            {
                var obj = JToken.Parse(strInput);
                return true;
            }
            catch (JsonReaderException jex)
            {
                Console.WriteLine(jex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    private void SendDataInChunks(NetworkStream networkStream, byte[] dataToSend)
    {
        try
        {
            int bytesSent = 0;
            while (bytesSent < dataToSend.Length)
            {
                int bytesToSend = Math.Min(dataToSend.Length - bytesSent, BufferSize);
                networkStream.Write(dataToSend, bytesSent, bytesToSend);
                bytesSent += bytesToSend;
            }
            networkStream.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending data in chunks: " + ex.Message);
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
    public void SendDataToClient(NetworkStream networkStream, Player playerData)
    {


        CustomPlayer customPlayer = new CustomPlayer
        {
            Type = "PlayerLogin",
            PlayerId = playerData.PlayerId,
            AccountId = playerData.AccountId,
            Name = playerData.Name,
           // Level = playerData.Level,
            Balance = playerData.Balance,
            Blessings = playerData.Blessings,
            Cap = playerData.Cap,
            Experience = playerData.Experience,
            GroupId = playerData.GroupId,
            Health = playerData.Health,
            HealthMax = playerData.HealthMax,
            LastLogin = playerData.LastLogin,
            LastLogout = playerData.LastLogout,
            LookAddons = playerData.LookAddons,
            LookBody = playerData.LookBody,
            LookFeet = playerData.LookFeet,
            LookHead = playerData.LookHead,
            LookLegs = playerData.LookLegs,
            Mana = playerData.Mana,
            ManaMax = playerData.ManaMax,
            ManaSpent = playerData.ManaSpent,
            PosX = playerData.PosX,
            PosY = playerData.PosY,
            PosZ = playerData.PosZ,
            Save = playerData.Save,
            Sex = playerData.Sex,
            Level = new ClientSkill("Level", playerData.Level),
            //MagicLevel = new ClientSkill("Magic Level"), // Populate with actual magic level if available
            Skills = new Dictionary<string, ClientSkill>



            {
                { "SkillAxe", new ClientSkill("Axe Fighting", playerData.SkillAxe) },
                { "SkillClub", new ClientSkill("Club Fighting", playerData.SkillClub) },
                // ... add other skills ...
            }
        };
        ClientCreature creature = new ClientCreature
        {
            CreatureID = playerData.PlayerId,
            CreatureName = playerData.Name,
            
        };
        var dataToSendObject = new
        {

            player = customPlayer,
            Creature = creature
        };
        string json = JsonConvert.SerializeObject(dataToSendObject);
        byte[] jsonDataBytes = Encoding.UTF8.GetBytes(json);

        // Prefix data with length (excluding the length of the length prefix itself)
        byte[] lengthPrefix = BitConverter.GetBytes((ushort)jsonDataBytes.Length);
        byte[] dataToSend = new byte[lengthPrefix.Length + jsonDataBytes.Length];
        lengthPrefix.CopyTo(dataToSend, 0);
        jsonDataBytes.CopyTo(dataToSend, lengthPrefix.Length);

        networkStream.Write(dataToSend, 0, dataToSend.Length);
    }



    public void SendDataToClientInGame(NetworkStream networkStream, object gameData)
    {
        string gameDataJson = JsonConvert.SerializeObject(gameData);
        byte[] jsonDataBytes = Encoding.UTF8.GetBytes(gameDataJson);

        // Prefix data with length
        byte[] lengthPrefix = BitConverter.GetBytes(jsonDataBytes.Length);
        byte[] dataToSend = new byte[lengthPrefix.Length + jsonDataBytes.Length];
        lengthPrefix.CopyTo(dataToSend, 2);
        jsonDataBytes.CopyTo(dataToSend, lengthPrefix.Length);

        networkStream.Write(dataToSend, 0, dataToSend.Length);
    }




    private void HandleClient(object obj)
    {
       // OtMap map = Program.LoadMap();
        TcpClient client = (TcpClient)obj;
        NetworkStream networkStream = client.GetStream();
        

        // Create a new PlayerGame object for the connected client
        PlayerGame Playeringame = new PlayerGame(gameWorld);
        Playeringame.NetworkStream = networkStream;

        // Add the player to the list of players
        Players.Add(Playeringame); // Assuming 'players' is accessible here

        // Define and initialize player input object here
        // Replace this with the actual way you receive player input
       /* PlayerGame playerInput = new PlayerGame();*/ // Create your player input object


        try
        {
            string authToken = "ExpectedAuthToken";
            byte[] buffer = new byte[1024];
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            string receivedToken = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine(receivedToken);
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
                    SendMapDataToClient(networkStream,this.map, playerData);
                    SendHeartbeatToClient(networkStream);

                    //if(player.LastLogin > player.LastLogout)
                    //{
                    //    SendDataToClientInGame(Playeringame, );
                    //}

                    while (true)
                    {
                        string input = ReadPlayerInputFromNetwork(Playeringame);
                        if (input == null || input == "") // Check for empty input, indicating disconnection
                        {
                            break; // Exit the loop if the client has disconnected
                        }
                        // Example of handling movement input
                        if (Playeringame.IsMoveCommand(input))
                        {
                            Point newPlayerPosition = Playeringame.CalculateNewPosition(Playeringame.Position, Playeringame.GetDirectionFromInput(input));

                            Playeringame.MoveTo(newPlayerPosition);
                        }

                        // Other game logic

                        // Sleep or control frame rate (optional)
                        Thread.Sleep(16); // Sleep for approximately 60 frames per second
                    }



                }
                else
                {
                    Console.WriteLine();
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
        finally
        {
            client.Close();
            Players.Remove(Playeringame);
        }
    }

    // You should replace this method with your actual input reading mechanism
    private string ReadPlayerInputFromNetwork(PlayerGame playerInGame)
    {
        try
        {
            byte[] buffer = new byte[1024];
            int bytesRead = playerInGame.NetworkStream.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading player input: {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket exception: {ex.Message}");
            playerInGame.IsConnected = false;
            // Additional logic for handling player disconnection
        }

        return "";
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
        for (int i = 0; i < loginInfo.Length; i++)
        {
            Console.WriteLine(loginInfo[i]);
        }


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