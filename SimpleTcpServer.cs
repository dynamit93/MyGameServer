using Microsoft.EntityFrameworkCore;
using MyGameServer.player;
using Newtonsoft.Json;
using OpenTibiaCommons.Domain;
using SharpTibiaProxy.Domain;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Timers;


namespace MyGameServer
{
    public class SimpleTcpServer
    {
        public List<PlayerGame> Players { get; } = new List<PlayerGame>();
        private GameWorld gameWorld;
        private System.Timers.Timer gameLoopTimer;
        private PlayerActionProcessor actionProcessor;
        private TcpListener tcpListener;
        private GameContext dbContext;
        private OtMap map;
        private Socket listenSocket;
        private bool isRunning;
        private Thread gameLoopThread;

        public SimpleTcpServer(int port, GameContext dbContext, OtMap map)
        {
            this.dbContext = dbContext;
            this.map = map;
            this.gameWorld = new GameWorld(this, this.map);
            this.gameWorld.InitializeMonstersFromMap();
            this.actionProcessor = new PlayerActionProcessor(this.gameWorld, this.FilterMapData, map);

            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            listenSocket.Listen(100); // the parameter here is the backlog size


            // Initialize and start the game loop timer
            gameLoopTimer = new System.Timers.Timer(100); // Run the loop every 100ms (adjust as needed)
            gameLoopTimer.Elapsed += OnGameLoopTick;
            gameLoopTimer.AutoReset = true;
            gameLoopTimer.Enabled = true;
            isRunning = false;
        }

        public void Start()
        {
            Console.WriteLine("Server started. Waiting for connections...");
            // Test the account query when the server starts
            TestAccountQuery();
            // Start listening for incoming connections in a separate thread
            isRunning = true;
            Thread listenThread = new Thread(ListenForClients);
            listenThread.Start();

            // Start the game loop in a separate thread
            gameLoopThread = new Thread(GameLoop);
            gameLoopThread.Start();

            Console.WriteLine("Server and game loop have started.");
        }

        private void ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    Socket clientSocket = listenSocket.Accept();
                    Console.WriteLine("Client connected.");
                    ThreadPool.QueueUserWorkItem(HandleClient, clientSocket);
                }
                catch (Exception e)
                {
                    if (isRunning) // Ignore socket exceptions after stopping the server
                    {
                        Console.WriteLine($"Error accepting client: {e.Message}");
                    }
                }
            }
        }

        private void GameLoop()
        {
            while (isRunning)
            {
                // Update game world state, handle NPC actions, etc.
                gameWorld.ScheduledUpdate();

                // For example, update every 100ms
                Thread.Sleep(100);
            }
        }


        public void Stop()
        {
            // Stop the server and the game loop
            isRunning = false;

            // Close the listener socket
            listenSocket.Close();

            // Wait for the game loop thread to finish
            gameLoopThread.Join();

            Console.WriteLine("Server stopped.");
        }

        private void TestAccountQuery()
        {
            // Assuming dbContext is your GameContext instance
            var account = dbContext.Account.AsNoTracking().FirstOrDefault(a => a.Name == "TEST");
            if (account != null)
            {
                Console.WriteLine($"Test query found account: {account.Name}");
            }
            else
            {
                Console.WriteLine("Test query did not find the 'TEST' account.");
            }
        }


        private void OnGameLoopTick(Object source, ElapsedEventArgs e)
        {
            // Call the update method in GameWorld
            gameWorld.ScheduledUpdate();

            // Any additional periodic updates can be called here
        }

        public List<OtCreature> PrintAllCreatureLocations(OtMap map)
        {
            var OtCreaturelist = new List<OtCreature>();
            foreach (var spawn in map.Spawns)
            {
                foreach (var creature in spawn.GetCreatures())
                {
                    Console.WriteLine($"spawn.Location: {spawn.Location}");
                    Console.WriteLine($"spawn.Radius: {spawn.Radius}");
                    string creatureType = creature.Type == CreatureType.NPC ? "NPC" : "Monster";
                    Console.WriteLine($"Creature Id: {creature.Id}, Creature Type: {creatureType}, " +
                        $"Name: {creature.Name}, Location: {creature.Location}");

                    var newX = creature.Location.X;
                    var newY = creature.Location.Y;
                    // Correctly adjust the creature's location based on the spawn point
                    if (!creature.wasSpawnedBefore)
                    {
                        newX = creature.Location.X + spawn.Location.X;
                        newY = creature.Location.Y + spawn.Location.Y;
                    }


                    // Now, you need to create a new Location with these values
                    creature.Location = new Location(newX, newY, creature.Location.Z);
                    creature.wasSpawnedBefore = true;
                    Console.WriteLine($"Adjusted Creature Location: {creature.Location}");
                    OtCreaturelist.Add(creature);
                }
            }
            return OtCreaturelist;
        }


        public void SendCreactureToClient(Socket clientSocket, OtMap mapData)
        {
            using (NetworkStream networkStream = new NetworkStream(clientSocket))
            {
                try
                {
                    var filtermapcreacture = PrintAllCreatureLocations(mapData);

                    var mapCreacturePacket = new
                    {
                        Type = "CreactureDescription",
                        Creature = filtermapcreacture.Select(creature =>
                        {
                            bool exists = LuaScripting.AllMonsters.Any(obj => obj.Name.ToLower() == creature.Name.ToLower());
                            if (exists)
                            {
                                var creatureBuild = LuaScripting.AllMonsters.SingleOrDefault(monster => monster.Name.ToLower() == creature.Name.ToLower());
                                return new
                                {
                                    type = creature.Type,
                                    id = creature.Id,
                                    name = creature.Name,
                                    looktype = creatureBuild.LookType,
                                    HealthNow = creatureBuild.HealthNow,
                                    HealthMax = creatureBuild.HealthMax,
                                    Location = new { creature.Location.X, creature.Location.Y, creature.Location.Z },
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
        }



        private const int BufferSize = 1024;
        public void SendMapDataToClient(Socket clientSocket, OtMap mapData, Player playerData)
        {
            using (NetworkStream networkStream = new NetworkStream(clientSocket))
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
                            IsPickupable = item.Type != null ? item.Type.IsMoveable : default(bool),

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
        }

        public void SendCreatureUpdateToClient(Socket clientSocket, PlayerGame playerGame)
        {
            using (NetworkStream networkStream = new NetworkStream(clientSocket))
            {
                try
                {
                    var creaturesToUpdate = gameWorld.GetCreaturesInViewRange(playerGame);

                    var creatureUpdatePacket = new
                    {
                        Type = "CreatureUpdate",
                        Creatures = creaturesToUpdate.Select(creature =>
                        {
                            // Access the target directly from the creatureTargets dictionary.
                            var target = gameWorld.creatureTargets.ContainsKey(creature) ? gameWorld.creatureTargets[creature] : null;

                            return new
                            {
                                Id = creature.Id,
                                Name = creature.Name,
                                Position = new { X = creature.Location.X, Y = creature.Location.Y, Z = creature.Location.Z },
                                Health = creature.HealthNow, // Assuming HealthNow represents the creature's current health.
                                Target = target != null ? new { PlayerId = target.PlayerId, Position = target.Position } : null
                            };
                        }).ToList()
                    };

                    string jsonPacket = JsonConvert.SerializeObject(creatureUpdatePacket);
                    byte[] packetBytes = Encoding.UTF8.GetBytes(jsonPacket);
                    networkStream.Write(packetBytes, 0, packetBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending creature update to client: {ex.Message}");
                }
            }
        }




        //public void blockedtile(OtMap mapData, Player playerData)


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




        public List<TileData> FilterMapData(OtMap mapData, Player playerData)
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
        public void SendDataToClient(Socket clientSocket, Player playerData)
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
            //ClientCreature creature = new ClientCreature
            //{
            //    CreatureID = playerData.PlayerId,
            //    CreatureName = playerData.Name,

            //};
            var dataToSendObject = new
            {

                player = customPlayer,
                //Creature = creature
            };
            string json = JsonConvert.SerializeObject(dataToSendObject);
            byte[] jsonDataBytes = Encoding.UTF8.GetBytes(json);

            // You can also include the length prefix if you're using a framed protocol
            byte[] lengthPrefix = BitConverter.GetBytes((ushort)jsonDataBytes.Length);
            byte[] dataToSend = new byte[lengthPrefix.Length + jsonDataBytes.Length];
            lengthPrefix.CopyTo(dataToSend, 0);
            jsonDataBytes.CopyTo(dataToSend, lengthPrefix.Length);

            clientSocket.Send(dataToSend);
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





        private void HandleClient(object clientSocketObj)
        {
            Socket clientSocket = (Socket)clientSocketObj;


            // Get client endpoint information
            IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
            string clientIP = clientEndPoint.Address.ToString();
            int clientPort = clientEndPoint.Port;

            Console.WriteLine($"Client connected: {clientIP}:{clientPort}");

            NetworkStream networkStream = new NetworkStream(clientSocket);
            PlayerGame Playeringame = new PlayerGame(gameWorld); // Initialize but don't have player-specific data yet
            Playeringame.NetworkStream = networkStream;

            try
            {
                string authToken = "ExpectedAuthToken";
                byte[] buffer = new byte[1024];
                int bytesRead = clientSocket.Receive(buffer);
                string receivedToken = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine(receivedToken);
                if (receivedToken == authToken)
                {
                    Console.WriteLine("Client authenticated.");
                    Console.WriteLine($"Client {clientIP}:{clientPort} authenticated.");

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

                        Playeringame.Position = new Point3D(playerData.PosX, playerData.PosY, playerData.PosZ); // Make sure this is initialized in your player data fetching logic.

                        Players.Add(Playeringame); // Now add the player to the list after all details are set.

                        // Send necessary data to the client.
                        SendDataToClient(clientSocket, playerData);
                        SendMapDataToClient(clientSocket, this.map, playerData);
                        SendCreactureToClient(clientSocket, this.map);
                        SendCreatureUpdateToClient(clientSocket, Playeringame);
                        //SendHeartbeatToClient(networkStream);

                        // Processing actions from the client.
                        while (true)
                        {
                            string input = ReadPlayerInputFromNetwork(Playeringame);
                            if (string.IsNullOrEmpty(input))
                            {
                                break; // Exit the loop if the client has disconnected or sent empty input.
                            }

                            actionProcessor.ProcessAction(input, Playeringame, playerData);
                            if (Playeringame.IsMoveCommand(input))
                            {
                                Point3D newPlayerPosition = Playeringame.CalculateNewPosition(Playeringame.Position, Playeringame.GetDirectionFromInput(input));
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
                    Console.WriteLine($"Client {clientIP}:{clientPort} failed authentication.");

                    Console.WriteLine("Client failed authentication.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientIP}:{clientPort}: {ex.Message}");


            }
            finally
            {
                networkStream.Close();
                clientSocket.Close();  // Corrected from 'client' to 'clientSocket'
                Console.WriteLine($"Connection closed for client {clientIP}:{clientPort}");
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
            Console.WriteLine("LOGIN_SUCCESS ", "LOGIN_FAILURE ", response);
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            networkStream.Write(responseBytes, 0, responseBytes.Length);

            return (false, null);
        }



    }

}
