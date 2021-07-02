using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WamBot.Twitch.Data
{
    public class DbChannel
    {
        [Key]
        public string Name { get; set; }
        public string LastStreamId { get; set; }
        public List<DbChannelUser> DbChannelUsers { get; set; }
        
        [NotMapped]
        public int TotalUsers { get; set; }
    }
}
