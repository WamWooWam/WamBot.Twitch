using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
                optionsBuilder.UseSqlite("Data Source=bot.dev.db;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
        }

        public DbSet<DbUser> DbUsers { get; set; }
        public DbSet<DbChannel> DbChannels { get; set; }
        public DbSet<DbChannelUser> DbChannelUsers { get; set; }
    }

    public class DbUser
    {
        [Key]
        public string Name { get; set; }
        public long OnyxPoints { get; set; } = 0;
        public int PenisOffset { get; set; } = 0;
        public PenisType PenisType { get; set; } = PenisType.Normal;
        public List<DbChannelUser> DbChannelUsers { get; set; }
    }

    public class DbChannel
    {
        [Key]
        public string Name { get; set; }
        public string LastStreamId { get; set; }
        public List<DbChannelUser> DbChannelUsers { get; set; }
    }

    public class DbChannelUser
    {
        public string UserName { get; set; }
        public string ChannelName { get; set; }

        public decimal Balance { get; set; }
        public string LastStreamId { get; set; }

        public DbUser DbUser { get; set; }
        public DbChannel DbChannel { get; set; }
    }

    public enum PenisType
    {
        None = -1,
        Normal,
        Large,
        Inverse,
        Tiny
    }
}
