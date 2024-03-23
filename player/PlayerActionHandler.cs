using Newtonsoft.Json;
using OpenTibiaCommons.Domain;
using SharpTibiaProxy.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MyGameServer.player.PlayerActionProcessor;

namespace MyGameServer.player
{
    public class PlayerActionProcessor
    {
        public delegate List<TileData> FilterMapDataDelegate(OtMap mapData, Player playerData);
        private readonly FilterMapDataDelegate filterMapData;
        public GameWorld gameWorld { get; set; }
        public OtMap mapData { get; set; }
        public PlayerActionProcessor(GameWorld gameWorld, FilterMapDataDelegate filterMapDataDelegate, OtMap otMap)
        {
            this.gameWorld = gameWorld;
            this.filterMapData = filterMapDataDelegate;
            this.mapData = otMap;
        }

        public void ProcessAction(string input, PlayerGame player,Player playerData)
        {
            try
            {
                // Ensure that the input string is correctly trimmed and formatted
                string cleanInput = input.Trim().Trim('"').Replace("\\\"", "\"").Trim();

                // Check if the last character is an extra quote and remove it if present
                if (cleanInput.EndsWith("\""))
                {
                    cleanInput = cleanInput.Substring(0, cleanInput.Length - 1);
                }

                dynamic command = JsonConvert.DeserializeObject(cleanInput);

                switch ((string)command.action)
                {
                    case "move":
                        HandleMove(player, command, playerData);
                        break;
                    case "attack":
                        HandleAttack(player, command);
                        break;
                    case "useItem":
                        HandleUseItem(player, command);
                        break;
                    default:
                        Console.WriteLine($"Unrecognized command: {command.action}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error processing action: {ex.Message}");
            }
        }



        private void HandleMove(PlayerGame player, dynamic command, Player playerData)
        {
            try
            {
                string directionStr = command.direction.ToString();
                Direction direction = player.GetDirectionFromInput(directionStr);
                Point3D newPosition = player.CalculateNewPosition(player.Position, direction);

                if (gameWorld.IsTileWalkable(newPosition))
                {
                    foreach (var tile in mapData.Tiles)
                    {
                        // Compare individual properties of the Point3D and Location
                        if (tile.Location.X == newPosition.X && tile.Location.Y == newPosition.Y && tile.Location.Z == newPosition.Z)
                        {
                            if (blockedtile(mapData, playerData, newPosition))
                            {
                                // Early exit from the method if the tile is blocked
                                Console.WriteLine("Sorry, not possible.");
                                return;
                            }
                            else
                            {
                                player.MoveTo(newPosition);
                                Console.WriteLine($"Player moved to {newPosition.X}, {newPosition.Y}, {newPosition.Z}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Cannot move to the new position as it's blocked.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleMove: {ex.Message}");
            }
        }




        public bool blockedtile(OtMap mapData, Player playerData,Point3D newPosition) // Ensure PlayerGame type is correct.
        {
            try
            {
                var filteredMapTiles = filterMapData(mapData, playerData);

                foreach (var item in filteredMapTiles)
                {
                    foreach (var tile in item.Items)
                    { 
                        if(item.Location.X == newPosition.X && item.Location.Y == newPosition.Y && item.Location.Z == newPosition.Z)
                        { 
                            if (tile.Type.BlockObject == true)
                            {
                                Console.WriteLine("tile.Type.BlockObject: " + tile.Type.BlockObject);
                                Console.WriteLine("tile.Name: " + tile.Name);
                                Console.WriteLine("tile.Id: " + tile.Id);
                                Console.WriteLine("tile.Type.Id: " + tile.Type.Id);
                                Console.WriteLine("tile.Type.Name: " + tile.Type.Name);
                                Console.WriteLine("item.Location: " + item.Location);
                                return true; // Block detected, return true.
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false; // In case of an exception, consider the tile not blocked.
            }

            return false; // If no blocks are detected, return false.
        }




        //private bool IsWalkable(Point3D position)
        //{
        //    Tile tile = GameWorld.GetTileAt(position);
        //    if (tile != null)
        //    {
        //        Console.WriteLine($"Checking tile at ({position.X}, {position.Y}, {position.Z}) - BlockProjectile: {tile.BlockProjectile}   - Tile id: {tile.id} - TileName {tile.TileName}");
        //        return !tile.BlockProjectile;
        //    }
        //    else
        //    {
        //        Console.WriteLine($"No tile found at ({position.X}, {position.Y}, {position.Z})");
        //        return false;
        //    }
        //}


        private void HandleAttack(PlayerGame player, dynamic command)
        {
            // Implement attack logic
            // Example: Identify the target and process the attack
            int targetId = command.targetId;
            Console.WriteLine($"Player {player.Name} is attacking target {targetId}.");

            // Process the attack and update game world or player state as necessary
        }

        private void HandleUseItem(PlayerGame player, dynamic command)
        {
            // Implement item use logic
            int itemId = command.itemId;
            Console.WriteLine($"Player {player.Name} is using item {itemId}.");
            // Process the item use and update game world or player state as necessary
        }
    }
}
