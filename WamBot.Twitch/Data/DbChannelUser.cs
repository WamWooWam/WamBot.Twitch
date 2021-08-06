namespace WamBot.Twitch.Data
{
    public class DbChannelUser
    {
        public long UserId { get; set; }
        public long ChannelId { get; set; }

        public decimal Balance { get; set; }
        public string LastStreamId { get; set; }

        public DbUser DbUser { get; set; }
        public DbChannel DbChannel { get; set; }
    }
}
