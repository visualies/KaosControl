using KaosControl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaosControl.Entities
{
    public class KaosUser : KaosEntity
    {
        public long SteamId { get; set; }
        public ulong? DiscordId { get; set; }
        public int Rank { get; set; }
        public int Experience { get; set; }

        public async Task RankUpAsync()
        {
            await Client.RankUpAsync(this);
        }

        public async Task SetRankAsync(int id)
        {
            await Client.SetRankAsync(this, id);
        }
        
        public async Task AddExperienceAsync(int amount)
        {
            await Client.AddExperienceAsync(this, amount);
        }
        
        public async Task AddPointsAsync(int amount)
        {
            await Client.AddPointsAsync(this, amount);
        }



    }
}
