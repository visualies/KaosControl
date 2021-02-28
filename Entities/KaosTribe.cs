using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaosControl.Entities
{
    public class KaosTribe : KaosEntity
    {
        public int Id { get; set; }
        public int TribeId { get; set; }
        public long ServerId { get; set; }
        public string ServerName { get; set; }
        public string TribeName { get; set; }
        public long OwnerSteamId { get; set; }
        public int PveBubble { get; private set; }

        public async Task<List<KaosUser>> GetMembersAsync()
        {
            return await Client.GetMembersAsync(this);
        }

        public async Task AddBubbleExperienceAsync(double amount)
        {
            await Client.AddBubbleExperienceAsync(this, amount);
        }

        public async Task<int> GetTribeSizeAsync()
        {
            return await Client.GetTribeSizeAsync(this)
        }
    }
}
