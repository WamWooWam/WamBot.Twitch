using System;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace WamBot.Twitch.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext()
        {

        }

        public BotDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbUser>()
                .HasAlternateKey(u => u.Id);

            modelBuilder.Entity<DbChannelUser>()
                .HasKey(c => new { c.UserName, c.ChannelName });

            modelBuilder.Entity<DbChannelUser>()
                .HasOne(u => u.DbChannel)
                .WithMany(c => c.DbChannelUsers)
                .HasForeignKey(u => u.ChannelName);

            modelBuilder.Entity<DbChannelUser>()
                .HasOne(u => u.DbUser)
                .WithMany(c => c.DbChannelUsers)
                .HasForeignKey(u => u.UserName);

            modelBuilder.HasCollation("twitch_name", "en-u-ks-primary", "icu", false);

            modelBuilder.Entity<DbUser>()
                        .Property(u => u.Name)
                        .UseCollation("twitch_name");

            modelBuilder.Entity<DbChannel>()
                        .Property(u => u.Name)
                        .UseCollation("twitch_name");

            modelBuilder.Entity<DbChannelUser>()
                        .Property(u => u.UserName)
                        .UseCollation("twitch_name");

            modelBuilder.Entity<DbChannelUser>()
                        .Property(u => u.ChannelName)
                        .UseCollation("twitch_name");
        }

        public DbSet<DbUser> DbUsers { get; set; }
        public DbSet<DbChannel> DbChannels { get; set; }
        public DbSet<DbChannelUser> DbChannelUsers { get; set; }
    }
}
