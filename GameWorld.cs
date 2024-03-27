using MyGameServer.player;
using Newtonsoft.Json;
using OpenTibiaCommons.Domain;
using SharpTibiaProxy.Domain;
using SharpTibiaProxy.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGameServer
{

    public struct Point3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Point3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }


    public class GameWorld
    {
        private Pathfinding pathfinding;
        private const int VIEW_RANGE = 100;
        private SimpleTcpServer tcpServer; // Reference to the SimpleTcpServer instance
        private List<PlayerGame> players;
        private List<OtCreature> monsters;
        private OtMap map;
        public Dictionary<OtCreature, PlayerGame> creatureTargets = new Dictionary<OtCreature, PlayerGame>();
        private Dictionary<OtCreature, List<Point3D>> monsterPaths = new Dictionary<OtCreature, List<Point3D>>();


        public GameWorld(SimpleTcpServer server, OtMap initialMap)
        {
            tcpServer = server;
            players = tcpServer.Players;
            this.map = initialMap;
            this.monsters = new List<OtCreature>();
            this.pathfinding = new Pathfinding(this);
        }
        public void InitializeMonstersFromMap()
        {
            // Initialize monsters from map and possibly set initial targets
            foreach (var spawn in this.map.Spawns)
            {
                foreach (var creature in spawn.GetCreatures())
                {
                    if (creature.Type == CreatureType.MONSTER)
                    {
                        // Initialize without targets or with default/initial targets
                        this.monsters.Add(creature);
                        creatureTargets[creature] = null; // Initialize without a target
                    }
                }
            }
        }


        private void UpdateMonsters()
        {
            foreach (var monster in monsters)
            {
                if (monster != null)  // Check if the monster is not null
                {
                    UpdateMonsterAI();
                    UpdateMonsterPath(monster);  // Make sure this is efficiently updating the path
                    ExecuteMonsterMovement(monster);
                }
            }
        }


        public void UpdatePlayerState(PlayerGame player, Point3D newPosition)
        {
            player.Position = newPosition; // Update player's position

            // Serialize the movement data for the moving player
            var playerMovementData = SerializeMovementData(player);

            // Optionally, notify the moving player about the update
            // This might be necessary if the client needs confirmation or additional data processing
            tcpServer.SendDataToClientInGame(player.NetworkStream, playerMovementData);

            foreach (var otherPlayer in players)
            {
                if (otherPlayer != player && ShouldNotifyOtherPlayerOfMovement(otherPlayer, player))
                {
                    var movementData = SerializeMovementData(player);
                    NotifyPlayerOfMovement(otherPlayer, player);
                    tcpServer.SendDataToClientInGame(otherPlayer.NetworkStream, movementData);
                }
            }
        }

        private void UpdateMonsterPath(OtCreature monster)
        {
            var target = creatureTargets[monster];
            if (target != null)
            {
                var monsterPosition = new Point3D(monster.Location.X, monster.Location.Y, monster.Location.Z);
                var targetPosition = new Point3D(target.Position.X, target.Position.Y, target.Position.Z);

                var path = pathfinding.FindPath(monsterPosition, targetPosition);
                if (path != null && path.Count > 0)
                {
                    monsterPaths[monster] = path;
                    MoveMonster(monster, path);
                }
            }
        }



        private void MoveMonster(OtCreature monster, List<Point3D> path)
        {
            if (path.Any())
            {
                var nextStep = path.First();
                // Update monster's Location instead of Position
                monster.Location = new Location(nextStep.X, nextStep.Y, nextStep.Z);
            }
        }

        private void ExecuteMonsterMovement(OtCreature monster)
        {
            if (monsterPaths.ContainsKey(monster) && monsterPaths[monster].Any())
            {
                var nextStep = monsterPaths[monster].First();
                monster.Location = new Location(nextStep.X, nextStep.Y, nextStep.Z);
                monsterPaths[monster].RemoveAt(0);

                NotifyClientsOfMonsterMovement(monster);
            }
        }


        private void NotifyClientsOfMonsterMovement(OtCreature monster)
        {
            // Retrieve the target from the creatureTargets dictionary
            var target = creatureTargets.TryGetValue(monster, out PlayerGame targetPlayer) ? targetPlayer : null;

            // Retrieve the path from the monsterPaths dictionary
            if (monsterPaths.TryGetValue(monster, out List<Point3D> path))
            {
                Console.WriteLine($"{monster.Name} (ID: {monster.Id}) moved to location ({monster.Location.X}, {monster.Location.Y}, {monster.Location.Z}).");
                Console.WriteLine("Path:");
                foreach (var step in path)
                {
                    Console.WriteLine($"Step to ({step.X}, {step.Y}, {step.Z})");
                }
            }

            // Now, log the target information
            if (target != null)
            {
                Console.WriteLine($"Current target for {monster.Name} is {target.Name} at location ({target.Position.X}, {target.Position.Y}, {target.Position.Z}).");
            }
            else
            {
                Console.WriteLine($"{monster.Name} has no current target.");
            }

            // This method should ideally also involve sending the relevant monster and target information to clients
            // For example, if you're tracking client connections or sessions, you'd send an update message here
        }



        // In your Monster AI routine:
        private void UpdateMonsterAI()
        {
            // Basic AI loop to assign or update targets for each monster
            foreach (var monster in monsters)
            {
                var target = FindTargetForMonster(monster); // Implement this method based on your AI logic
                creatureTargets[monster] = target;
            }
        }

        private PlayerGame FindTargetForMonster(OtCreature monster)
        {
            // Implement logic to find the nearest or most suitable PlayerGame target for the monster
            // Placeholder: return the first player or any specific logic you prefer
            return players.FirstOrDefault();
        }



        private bool ShouldNotifyOtherPlayerOfMovement(PlayerGame otherPlayer, PlayerGame movingPlayer)
        {
            // Example logic to determine if the other player is within the view range
            // and should be notified of the moving player's new position.

            // Calculate the distance between the other player and the moving player
            var distance = CalculateDistance(otherPlayer.Position, movingPlayer.Position);

            // Determine if this distance is within the view range
            if (distance <= VIEW_RANGE) // Assuming VIEW_RANGE is defined somewhere
            {
                return true; // Notify the other player
            }
            else
            {
                return false; // Do not notify the other player
            }
        }

        private double CalculateDistance(Point3D p1, Point3D p2)
        {
            // Implement the distance calculation (e.g., Euclidean distance)
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private string SerializeMovementData(PlayerGame player)
        {
            // Serialize the player's movement data with an additional type field
            return JsonConvert.SerializeObject(new
            {
                type = "PlayerMove",
                PlayerId = player.PlayerId,
                NewPosition = $"{player.Position.X}, {player.Position.Y}",

            });
        }

        public Tile ConvertOtTileToMyGameServerTile(OtTile otTile)
        {
            if (otTile == null) return null;

            var myTile = new Tile
            {
                // Assuming you have similar properties in your Tile class.
                // You'll need to adjust the property names and types according to your actual Tile definition.
                X = otTile.Location.X,
                Y = otTile.Location.Y,
                Z = otTile.Location.Z,
                // Add other property mappings as necessary
            };

            return myTile;
        }

        public List<OtCreature> GetCreaturesInViewRange(PlayerGame playerGame)
        {
            List<OtCreature> creaturesInView = new List<OtCreature>();

            // Define the visibility range around the player
            int rangeX = 20; // Horizontal visibility range
            int rangeY = 20; // Vertical visibility range
            int rangeZ = 2;  // Depth visibility range

            // Calculate the bounds based on the player's position
            int minX = playerGame.Position.X - rangeX;
            int maxX = playerGame.Position.X + rangeX;
            int minY = playerGame.Position.Y - rangeY;
            int maxY = playerGame.Position.Y + rangeY;
            int minZ = Math.Max(playerGame.Position.Z - rangeZ, 0);
            int maxZ = playerGame.Position.Z + rangeZ;

            // Iterate through all creatures and check if they are within the view range
            foreach (var creature in monsters) // Assuming 'monsters' is your List<OtCreature>
            {
                if (creature.Location.X >= minX && creature.Location.X <= maxX &&
                    creature.Location.Y >= minY && creature.Location.Y <= maxY &&
                    creature.Location.Z >= minZ && creature.Location.Z <= maxZ)
                {
                    creaturesInView.Add(creature);
                }
            }

            return creaturesInView;
        }

        // Assuming you have a method to get the tile at a specific position
        public Tile GetTileAt(Point3D position)
        {
            Location location = new Location(position.X, position.Y, position.Z);
            var otTile = map.GetTile(location); // This returns an OtTile
            return ConvertOtTileToMyGameServerTile(otTile); // Convert and return your Tile type
        }


        public List<Tile> GetTilesInViewRange(PlayerGame player)
        {
            List<Tile> visibleTiles = new List<Tile>();

            // Calculate the range of coordinates to check based on the player's view range.
            int minX = player.Position.X - player.ViewRange;
            int maxX = player.Position.X + player.ViewRange;
            int minY = player.Position.Y - player.ViewRange;
            int maxY = player.Position.Y + player.ViewRange;
            int minZ = player.Position.Z - player.ViewRange;
            int maxZ = player.Position.Z + player.ViewRange;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        Point3D tilePosition = new Point3D(x, y, z);
                        if (IsWithinWorldBoundaries(tilePosition))
                        {
                            Tile tile = GetTileAt(tilePosition);
                            if (tile != null)
                            {
                                visibleTiles.Add(tile);
                            }
                        }
                    }
                }
            }
            return visibleTiles;
        }

        private bool IsWithinWorldBoundaries(Point3D position)
        {
            // Implement your logic to check if the position is within the game world boundaries
            return true; // Placeholder return
        }


        public void UpdateWorldState()
        {

            UpdateTimeOfDay();
            UpdateWeather();
            SpawnNPCsAndMonsters();
        }

        private void UpdateTimeOfDay()
        {
            // Logic to update the game's time of day
        }

        private void UpdateWeather()
        {
            // Logic to change the weather conditions
        }

        private void SpawnNPCsAndMonsters()
        {
            // Logic to spawn NPCs and monsters based on certain conditions
        }

        // Scheduled update (could be called every few minutes or based on a game tick system)
        public void ScheduledUpdate()
        {
            UpdateWorldState();
            UpdateMonsters();
            // Any other regular updates
        }

        public void HandlePlayerInteraction(PlayerGame player, object interactionObject)
        {
            // Logic to handle player interactions with objects
        }

        private void NotifyPlayerOfMovement(PlayerGame recipient, PlayerGame mover)
        {
            // Example of sending player movement data
            var message = new
            {
                MessageType = "PlayerMove",
                PlayerId = mover.PlayerId,
                NewPosition = mover.Position
            };

            tcpServer.SendDataToClientInGame(recipient.NetworkStream, message);

        }
        public bool IsTileWalkable(Point3D position)
        {
            Location location = new Location(position.X, position.Y, position.Z);
            var otTile = map.GetTile(location);
            if (otTile == null) return false;

            // Check if the ground or any item on the tile blocks movement
            bool isGroundBlocking = otTile.Ground != null && otTile.Ground.Type.BlockObject;
            bool isAnyItemBlocking = otTile.Items.Any(item => item.Type.BlockObject);

            return !isGroundBlocking && !isAnyItemBlocking;
        }


    }
}
