using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyGameServer.player;

namespace MyGameServer
{
    public class LoginManager
    {
        private readonly GameContext dbContext;

        public LoginManager(GameContext context)
        {
            dbContext = context;
        }

        public (bool success, string playerName) ValidateUserLogin(string username, string password)
        {
            try
            {


            // Try to find a matching account by username
            var account = dbContext.Account.SingleOrDefault(a => a.Name == username);
                if (account != null)
                {
                    // Validate the password (you should have a proper password hashing and validation logic here)
                    if (ValidatePassword(password, account.Password))
                    {
                        // User login is successful
                        var player = dbContext.Players.SingleOrDefault(p => p.AccountId == account.Id);
                        if (player != null)
                        {
                            // Return the player's name along with success status
                            return (true, player.Name);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error accessing database: {ex.Message}");
                // Optionally rethrow the exception or handle it as needed
                return (false, null);
            }


            // User login failed or player not found
            return (false, null);
        }


        // You should implement a proper password validation/hashing method
        public bool ValidatePassword(string inputPassword, string hashedPassword)
        {
            // Implement your password validation/hashing logic here
            // Compare the inputPassword with the hashedPassword securely
            // Return true if they match, false otherwise
            // It's recommended to use a secure password hashing library like BCrypt or Argon2
            return inputPassword == hashedPassword;
        }
    }
}
