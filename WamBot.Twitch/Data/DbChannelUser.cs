namespace WamBot.Twitch.Data
{
    public class DbChannelUser
    {
        public string UserName { get; set; }
        public string ChannelName { get; set; }

        public decimal Balance { get; set; }
        public string LastStreamId { get; set; }

        public DbUser DbUser { get; set; }
        public DbChannel DbChannel { get; set; }
    }
}
