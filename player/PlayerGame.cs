using SharpTibiaProxy.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyGameServer;
using System.Net.Sockets; // Adjust this to the actual namespace where GameWorldObject is defined


namespace MyGameServer
{
    public class PlayerGame
    {
        private GameWorld gameWorld; // Add a reference to the GameWorld instance

        public PlayerGame(GameWorld gameWorld)
        {
            this.gameWorld = gameWorld;
        }

        public NetworkStream NetworkStream { get; set; }
        public int ViewRange { get; set; }
        public Point Position { get; set; }


            public int AccountId { get; set; }
            public int PlayerId { get; set; }
            public string Name { get; set; }
            public int Level { get; set; } = 1;
            public long Balance { get; set; }
            public byte Blessings { get; set; }
            public int Cap { get; set; } = 400;
            public long Experience { get; set; }
            public int GroupId { get; set; } = 1;
            public int Health { get; set; } = 150;
            public int HealthMax { get; set; } = 150;
            public long LastLogin { get; set; }
            public long LastLogout { get; set; }
            public int LookAddons { get; set; }
            public int LookBody { get; set; }
            public int LookFeet { get; set; }
            public int LookHead { get; set; }
            public int LookLegs { get; set; }
            public int Mana { get; set; }
            public int ManaMax { get; set; }
            public long ManaSpent { get; set; }
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int PosZ { get; set; }
            public byte Save { get; set; } = 1;
            public int Sex { get; set; }
            public int SkillAxe { get; set; } = 10;
            public long SkillAxeTries { get; set; }
            public int SkillClub { get; set; } = 10;
            public long SkillClubTries { get; set; }
            public int SkillDist { get; set; } = 10;
            public long SkillDistTries { get; set; }
            public int SkillFishing { get; set; } = 10;
            public long SkillFishingTries { get; set; }
            public int SkillFist { get; set; } = 10;
            public bool IsConnected { get; set; } = true;


        public void HandlePlayerInput(string input)
        {
            // Process player input, e.g., movement commands
            if (IsMoveCommand(input))
            {
                Point newPlayerPosition = CalculateNewPosition(Position, GetDirectionFromInput(input));
                MoveTo(newPlayerPosition);
            }

            // Handle other input commands based on your game's design
        }

        public bool IsMoveCommand(string input)
        {
            // Implement logic to check if the input is a movement command
            // For example, you might check if input equals "MOVE_UP" or "MOVE_DOWN"
            // Return true if it's a movement command, otherwise return false
            return false; // Placeholder return
        }

        public Point CalculateNewPosition(Point currentPosition, Direction direction)
        {
            // Implement logic to calculate the new position based on the current position and direction
            // This will depend on how your game world is structured
            // Return the new position as a Point
            return currentPosition; // Placeholder return
        }

        public Direction GetDirectionFromInput(string input)
        {
            // Convert the input to uppercase to ensure the method is case-insensitive
            string normalizedInput = input.ToUpper();

            switch (normalizedInput)
            {
                case "NORTH":
                    return Direction.DIRECTION_NORTH;
                case "EAST":
                    return Direction.DIRECTION_EAST;
                case "SOUTH":
                    return Direction.DIRECTION_SOUTH;
                case "WEST":
                    return Direction.DIRECTION_WEST;
                // Add cases for other directions if necessary
                default:
                    throw new ArgumentException("Invalid direction input"); // Throw an exception for invalid input
            }
        }


        public void InteractWithObject(GameWorldObject gameObject)
        {
            // Logic for interacting with an in-game object
        }

        public void MoveTo(Point newPosition)
        {
            // Update the player's position
            Position = newPosition;

            // Notify other players about the movement
            gameWorld.UpdatePlayerState(this, Position);
        }




    }
}
