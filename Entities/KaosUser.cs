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
        public int Experience { get; private set; }

        public async Task RankUpAsync()
        {
            await Client.RankUpAsync(this);
        }
        public async Task DeRankAsync()
        {
            await Client.DeRankAsync(this);
        }
        public async Task SetRankAsync(KaosPlayerRank rank)
        {
            await Client.SetRankAsync(this, rank);
        }

        public async Task AddExperienceAsync(int amount)
        {
            await Client.AddExperienceAsync(this, amount);
        }
        public async Task RemoveExperienceAsync(int amount)
        {
            await Client.RemoveExperienceAsync(this, amount);
        }


        public async Task AddPointsAsync(int amount)
        {
            await Client.AddPointsAsync(this, amount);
        }
        public async Task RemovePointsAsync(int amount)
        {
            await Client.RemovePointsAsync(this, amount);
        }

        public async Task AddExperienceMultiplierAsync(double multiplier, string type, int duration)
        {
            await Client.AddExperienceMultiplierAsync(this, multiplier, type, duration);
        }
        public async Task RemoveExperienceMultiplierAsync(string type)
        {
            await Client.RemoveExperienceMultiplierAsync(this, type);
        }

        public async Task<ulong> GetDiscordIdAsync()
        {
            return await Client.GetDiscordIdAsync(this);
        }
        public async Task<int> GetMaxTribeSizeAsync()
        {
            return await Client.GetMaxTribeSizeAsync(this);
        }
        public async Task<List<KaosTribe>> GetTribesAsync()
        {
            return await Client.GetTribesAsync(this);
        }

        public async Task AddPermissionGroupAsync(string name)
        {
            await Client.AddPermissionGroupAsync(this, name);
        }

        public async Task RemovePermissionGroupAsync(string name)
        {
            await Client.RemovePermissionGroupAsync(this, name);
        }
    }
}
