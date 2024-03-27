using SharpTibiaProxy.Domain;
using System;
using System.Collections.Generic;
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
        public Point3D Position { get; set; }


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

            string inputtoupper = input.ToUpper();
            // Process player input, e.g., movement commands
            if (IsMoveCommand(inputtoupper))
            {
                Point3D newPlayerPosition = CalculateNewPosition(Position, GetDirectionFromInput(inputtoupper));
                MoveTo(newPlayerPosition);
            }

            // Handle other input commands based on your game's design
        }

        public bool IsMoveCommand(string inputtoupper)
        {
            // Assuming the input commands are structured as "MOVE_NORTH", "MOVE_SOUTH", etc.
            // Check if the input starts with "MOVE_" and is followed by a valid direction
            if (string.IsNullOrEmpty(inputtoupper))
            {
                return false;
            }

            return inputtoupper == "NORTH" ||
                   inputtoupper == "EAST" ||
                   inputtoupper == "SOUTH" ||
                   inputtoupper == "WEST" ||
                   inputtoupper == "NE" ||
                   inputtoupper == "NW" ||
                   inputtoupper == "SE" ||
                   inputtoupper == "SW";
        }


        public Point3D CalculateNewPosition(Point3D currentPosition, Direction direction)
        {
            switch (direction)
            {
                case Direction.DIRECTION_NORTH:
                    // Move up, decrease Y
                    return new Point3D(currentPosition.X, currentPosition.Y - 1, currentPosition.Z);
                case Direction.DIRECTION_EAST:
                    // Move right, increase X
                    return new Point3D(currentPosition.X + 1, currentPosition.Y, currentPosition.Z);
                case Direction.DIRECTION_SOUTH:
                    // Move down, increase Y
                    return new Point3D(currentPosition.X, currentPosition.Y + 1, currentPosition.Z);
                case Direction.DIRECTION_WEST:
                    // Move left, decrease X
                    return new Point3D(currentPosition.X - 1, currentPosition.Y, currentPosition.Z);
                case Direction.DIRECTION_NE:
                    // Move up and right
                    return new Point3D(currentPosition.X + 1, currentPosition.Y - 1, currentPosition.Z);
                case Direction.DIRECTION_NW:
                    // Move up and left
                    return new Point3D(currentPosition.X - 1, currentPosition.Y - 1, currentPosition.Z);
                case Direction.DIRECTION_SE:
                    // Move down and right
                    return new Point3D(currentPosition.X + 1, currentPosition.Y + 1, currentPosition.Z);
                case Direction.DIRECTION_SW:
                    // Move down and left
                    return new Point3D(currentPosition.X - 1, currentPosition.Y + 1, currentPosition.Z);
                default:
                    // In case of an undefined direction, don't move
                    return currentPosition;
            }
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

        public void MoveTo(Point3D newPosition)
        {
            // Update the player's position
            Position = newPosition;

            // Notify other players about the movement
            gameWorld.UpdatePlayerState(this, Position);
        }




    }
}
