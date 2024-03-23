using System;
using System.Diagnostics;
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
                Console.WriteLine("Starting login attempt...");

                // Try to find a matching account by username
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                var account = dbContext.Account
                                               
                                               .FirstOrDefault(a => a.Name == username);



                stopwatch.Stop();
                Console.WriteLine($"Time taken for querying account: {stopwatch.Elapsed.TotalSeconds} seconds");

                if (account != null)
                {
                    stopwatch.Restart();
                    // Validate the password
                    if (ValidatePassword(password, account.Password))
                    {
                        stopwatch.Stop();
                        Console.WriteLine($"Time taken for password validation: {stopwatch.Elapsed.TotalSeconds} seconds");

                        stopwatch.Restart();
                        // User login is successful
                        var playerName = dbContext.Players
                                                  .AsNoTracking()
                                                  .Where(p => p.AccountId == account.Id)
                                                  .Select(p => p.Name)
                                                  .FirstOrDefault();
                        stopwatch.Stop();
                        Console.WriteLine($"Time taken for fetching player name: {stopwatch.Elapsed.TotalSeconds} seconds");

                        if (playerName != null)
                        {
                            return (true, playerName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"Error during login: {ex.Message}");
            }

            return (false, null);
        }

        public bool ValidatePassword(string inputPassword, string hashedPassword)
        {
            // Implement your hashing function here
            return HashPassword(inputPassword) == hashedPassword;
        }

        private string HashPassword(string password)
        {
            // Implement your hashing function here
            return password; // Placeholder for demonstration
        }
    }
}
