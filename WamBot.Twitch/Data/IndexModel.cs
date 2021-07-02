using System.Collections.Generic;

namespace WamBot.Twitch.Data
{
    public record IndexModel(IEnumerable<DbUser> Users, int Page, int TotalPages, int TotalCount)
    {
        public bool HasNextPage => (Page < TotalPages);
        public bool HasPreviousPage => Page > 1;
    }
}
