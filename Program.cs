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
using ClientCreature = MyGameServer.player.ClientCreature;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using MyGameServer;
using System.Xml.Linq;

class Program
{
    //public static List<PlayerGame> players = new List<PlayerGame>(); // Declare and initialize the players list



     

    public static OtMap LoadMap()
    {
        // Load items definitions
        OtItems items = new OtItems();
        items.Load("items.otb");

        // Initialize OtMap
        OtMap map = new OtMap(items);
        var tileLocations = map.Tiles.Select(t => t.Location.ToIndex()).ToHashSet();
        string spawnFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-spawn.xml");

        string otbmFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.otbm");
        map.Load(otbmFilePath, replaceTiles: true);


        map.LoadSpawn(spawnFilePath, tileLocations);
      

        return map;
    }




    //Console.WriteLine(creature.Location.X + spawn.Location.X);
    //            Console.WriteLine(creature.Location.Y + spawn.Location.Y);

    public static void Main(string[] args)
    {
        LuaScripting luaScripting = new LuaScripting();

        string monsterxmlstring = @"Data\Monster\monster.xml";

        luaScripting.LoadMonsterData(monsterxmlstring);


        var dbContext = new GameContext();
        OtMap map = LoadMap();  // LoadMap now also includes spawn loading


        // Print details of all creatures
       // PrintAllCreatureLocations(map);

        // Continue with server startup
        SimpleTcpServer server = new SimpleTcpServer(1300, dbContext, map);
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


public class SimpleTcpServer
{
    public List<PlayerGame> Players { get; } = new List<PlayerGame>();
    private GameWorld gameWorld;
    private PlayerActionProcessor actionProcessor;
    private TcpListener tcpListener;
    private GameContext dbContext;
    private OtMap map;
    public SimpleTcpServer(int port, GameContext dbContext, OtMap map)
    {

        tcpListener = new TcpListener(IPAddress.Loopback, port);
        this.dbContext = dbContext;
        this.map = map;  // Assign the passed map to the class's map field
        this.gameWorld = new GameWorld(this);
        actionProcessor = new PlayerActionProcessor(gameWorld);
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



    private List<OtCreature> PrintAllCreatureLocations(OtMap map)
    {
        var OtCreaturelist = new List<OtCreature>();
        foreach (var spawn in map.Spawns)
        {
            spawn.GetCreatures();
            foreach (var creature in spawn.GetCreatures())
            {
                Console.WriteLine($"spawn.Location: {spawn.Location}");
                Console.WriteLine($"spawn.Radius: {spawn.Radius}");
                string creatureType = creature.Type == CreatureType.NPC ? "NPC" : "Monster";
                Console.WriteLine($"Creature Id: {creature.Id}, Creature Type: {creatureType}, " +
                    $"Name: {creature.Name}, Location: {creature.Location}");
                // Assuming creature.Location and spawn.Location return a struct (e.g., Point, or a custom struct)
                var newX = creature.Location.X + spawn.Location.X;
                var newY = creature.Location.Y + spawn.Location.Y;

                // Now, you need to create a new Location with these values
                // Assuming there's a constructor that takes X and Y values
                creature.Location = new Location(newX, newY, creature.Location.Z);

                Console.WriteLine(creature.Location);
                OtCreaturelist.Add(creature);
            }
        }
        return OtCreaturelist;
    }

    public void SendCreactureToClient(NetworkStream networkStream, OtMap mapData)
    {
        try
        {
            var filtermapcreacture = PrintAllCreatureLocations(mapData);

            var mapCreacturePacket = new
            {
                
                Type = "CreactureDescription",
                Creature = filtermapcreacture.Select(creature =>
                {
                    bool exists = LuaScripting.AllMonsters.Any(obj => obj.Name.ToLower() == creature.Name);
                    Debug.WriteLine(creature.Name);
                    Debug.WriteLine(exists);
                    if (exists)
                    {
                        var creatureBuild = LuaScripting.AllMonsters.SingleOrDefault(monster => monster.Name.ToLower() == creature.Name);
                        return new
                        {
                            type = creature.Type,
                            id = creature.Id,
                            name = creature.Name,
                            looktype = creatureBuild.LookType,
                            HealthNow = creatureBuild.HealthNow,
                            HealthMax = creatureBuild.HealthMax,
                            Location = new { creature.Location.X, creature.Location.Y, creature.Location.Z }
                        };
                    }
                    else return null;
                }).ToList()
            };



            string jsonPacket = JsonConvert.SerializeObject(mapCreacturePacket);
            byte[] packetBytes = Encoding.UTF8.GetBytes(jsonPacket);
            networkStream.Write(packetBytes, 0, packetBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
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
                    Items = tile.Items.Select(item => new
                    {
                        Id = item.Id,
                        Name = item.Name,
                        BlockPathFind = item.Type != null ? item.Type.BlockPathFind : default(bool),
                        BlockProjectile = item.Type != null ? item.Type.BlockProjectile : default(bool),
                        Description = item.Type != null ? item.Type.Description : default(string),
                        LookThrough = item.Type != null ? item.Type.LookThrough : default(bool),
                        AlwaysOnTop = item.Type?.AlwaysOnTop ?? false,
                        AlwaysontopOrder = item.Type != null ? item.Type.AlwaysOnTopOrder : default(int),
                        BlockObject = item.Type != null ? item.Type.BlockObject : default(bool),
                        IsMoveable = item.Type != null ? item.Type.IsMoveable : default(bool),
                        IsPickupable = item.Type != null ? item.Type.IsMoveable: default(bool),

                    }).ToList(),

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
                    tileData.Items.Add(new ItemData { Id = item.Type.Id, Name = item.Type.Name, Type = item.Type });
                }

                filteredTiles.Add(tileData);
            }
        }

        return filteredTiles;
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
        string gameDataJson;

        // Check if gameData is already a JSON string
        if (gameData is string)
        {
            gameDataJson = (string)gameData;
        }
        else
        {
            gameDataJson = JsonConvert.SerializeObject(gameData);
        }

        byte[] jsonDataBytes = Encoding.UTF8.GetBytes(gameDataJson);

        // Prefix data with length
        byte[] lengthPrefix = BitConverter.GetBytes(jsonDataBytes.Length);
        byte[] dataToSend = new byte[lengthPrefix.Length + jsonDataBytes.Length];

        // Copy the length prefix and JSON data into the dataToSend array
        lengthPrefix.CopyTo(dataToSend, 0); // Start at index 0
        jsonDataBytes.CopyTo(dataToSend, lengthPrefix.Length);

        networkStream.Write(dataToSend, 0, dataToSend.Length);
    }





    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream networkStream = client.GetStream();
        PlayerGame Playeringame = new PlayerGame(gameWorld); // Initialize but don't have player-specific data yet
        Playeringame.NetworkStream = networkStream;

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

                // Assume ProcessLoginRequest validates the player and fetches their data.
                var (isValidLogin, playerData) = ProcessLoginRequest(networkStream);

                if (isValidLogin && playerData != null)
                {
                    // Now that the player is validated, assign the fetched details to the Playeringame object.
                    Playeringame.PlayerId = playerData.PlayerId;
                    Playeringame.Name = playerData.Name;
                    Playeringame.Health = playerData.Health;
                    Playeringame.HealthMax = playerData.HealthMax;
                    Playeringame.Mana = playerData.Mana;
                    Playeringame.ManaMax = playerData.ManaMax;
                    Playeringame.Level = playerData.Level;

                    Playeringame.Position = new Point(playerData.PosX, playerData.PosY); // Make sure this is initialized in your player data fetching logic.

                    Players.Add(Playeringame); // Now add the player to the list after all details are set.

                    // Send necessary data to the client.
                    SendDataToClient(networkStream, playerData);
                    SendMapDataToClient(networkStream, this.map, playerData);
                    SendCreactureToClient(networkStream, this.map);
                    //SendHeartbeatToClient(networkStream);

                    // Processing actions from the client.
                    while (true)
                    {
                        string input = ReadPlayerInputFromNetwork(Playeringame);
                        if (string.IsNullOrEmpty(input))
                        {
                            break; // Exit the loop if the client has disconnected or sent empty input.
                        }

                        actionProcessor.ProcessAction(input, Playeringame);
                        if (Playeringame.IsMoveCommand(input))
                        {
                            Point newPlayerPosition = Playeringame.CalculateNewPosition(Playeringame.Position, Playeringame.GetDirectionFromInput(input));
                            Playeringame.MoveTo(newPlayerPosition);
                        }

                        Thread.Sleep(16); // Sleep to control the loop's execution rate.
                    }
                }
                else
                {
                    Console.WriteLine("Invalid login credentials or player does not exist.");
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
            networkStream.Close();
            client.Close();
            Players.Remove(Playeringame); // Clean up by removing the player from the list.
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