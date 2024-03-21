using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyGameServer.player; 

namespace MyGameServer
{
    public class GameContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Account> Account { get; set; }
        public DbSet<Player_Items> PlayerItems { get; set; }

        public GameContext() { }

        public GameContext(DbContextOptions<GameContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseMySql("server=localhost;database=mmorpg;user=mmorpg;password=00dLW8qbrKNn64DX",
                        new MySqlServerVersion(new Version(8, 0, 23)));
            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignore the Skills property

            // Configure the Player_Items entity
            modelBuilder.Entity<Player_Items>()
                .HasOne(pi => pi.Player) // Navigation property in Player_Items
                .WithMany() // No collection property in Player
                .HasForeignKey(pi => pi.PlayerId); // Foreign key in Player_Items

            // Define an index on the Name column in the Account table
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Name)
                .IsUnique();
            // ... Other model configurations ...
        }
    }

    public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
    {
        public GameContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
            optionsBuilder.UseMySql("server=localhost;database=mmorpg;user=mmorpg;password=00dLW8qbrKNn64DX",
                new MySqlServerVersion(new Version(8, 0, 23)));

            return new GameContext(optionsBuilder.Options);
        }
    }


}
