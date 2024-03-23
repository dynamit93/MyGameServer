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
        private const int VIEW_RANGE = 100;
        private SimpleTcpServer tcpServer; // Reference to the SimpleTcpServer instance
        private List<PlayerGame> players;
        private OtMap map;
        public GameWorld(SimpleTcpServer server, OtMap initialMap)
        {
            tcpServer = server;
            players = tcpServer.Players;
            this.map = initialMap;
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

        public MyGameServer.Tile ConvertOtTileToMyGameServerTile(OpenTibiaCommons.Domain.OtTile otTile)
        {
            if (otTile == null) return null;

            var myTile = new MyGameServer.Tile
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



    }
}
