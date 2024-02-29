using Newtonsoft.Json;
using SharpTibiaProxy.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MyGameServer.player
{
    public class PlayerActionProcessor
    {
        public GameWorld GameWorld { get; set; }

        public PlayerActionProcessor(GameWorld gameWorld)
        {
            GameWorld = gameWorld;
        }

        public void ProcessAction(string input, PlayerGame player)
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
                        HandleMove(player, command);
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



        private void HandleMove(PlayerGame player, dynamic command)
        {
            try
            {
                // Ensure that command.direction is a string before passing it
                string directionStr = command.direction.ToString(); // Convert dynamic to string explicitly

                // Now, use the string version of direction for further processing
                Direction direction = player.GetDirectionFromInput(directionStr);

                Point newPosition = player.CalculateNewPosition(player.Position, direction);
                player.MoveTo(newPosition);

                Console.WriteLine($"Moved to {newPosition}");
            }
            catch (Exception ex)
            {
                // It's good practice to handle potential exceptions, especially when dealing with dynamic types
                Console.WriteLine($"Error in HandleMove: {ex.Message}");
            }
        }


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
