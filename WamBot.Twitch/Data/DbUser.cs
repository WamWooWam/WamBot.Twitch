using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WamBot.Twitch.Data
{
    public class DbUser
    {
        public long Id { get; set; }

        [Key]
        public string Name { get; set; }

        public long OnyxPoints { get; set; } = 0;
        public int PenisOffset { get; set; } = 0;
        public PenisType PenisType { get; set; } = PenisType.Normal;

        public int ConsecutiveWins { get; set; } = 0;
        public int ConsecutiveLosses { get; set; } = 0;

        public int AllTimeConsecutiveWins { get; set; } = 0;
        public int AllTimeConsecutiveLosses { get; set; } = 0;

        public List<DbChannelUser> DbChannelUsers { get; set; }

        [NotMapped]
        public decimal TotalBalance => DbChannelUsers.Sum(u => u.Balance);

        [NotMapped]
        public double? PenisSize { get; set; }

        public void IncrementConsecutiveWins()
        {
            ConsecutiveLosses = 0;
            ConsecutiveWins += 1;
            if (ConsecutiveWins > AllTimeConsecutiveWins)
                AllTimeConsecutiveWins = ConsecutiveWins;
        }

        public void IncrementConsecutiveLosses()
        {
            ConsecutiveWins = 0;
            ConsecutiveLosses += 1;
            if (ConsecutiveLosses > AllTimeConsecutiveLosses)
                AllTimeConsecutiveLosses = ConsecutiveLosses;
        }

    }
}
