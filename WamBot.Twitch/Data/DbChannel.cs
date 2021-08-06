using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WamBot.Twitch.Data
{
    [Index(nameof(Name))]
    public class DbChannel
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; }

        public string LastStreamId { get; set; }
        public List<DbChannelUser> DbChannelUsers { get; set; }
        
        [NotMapped]
        public int TotalUsers { get; set; }
    }
}
