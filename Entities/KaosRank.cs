using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Entities
{
    public class KaosRank
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ColorCode { get; set; }
        public string PermissionGroup { get; set; }
        public int RequiredExperience { get; set; }
        public int PointsReward { get; set; }
        public ulong DiscordRoleId { get; set; }
    }
}
