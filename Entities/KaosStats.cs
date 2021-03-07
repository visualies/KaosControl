using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Entities
{
    public class KaosStats
    {
        public long SteamId { get; set; }
        public string UserName { get; set; }
        public string SteamName { get; set; }
        public string TribeName { get; set; }
        public int TribeId { get; set; }
        public long PlayTime { get; set; }
        public long PlayerKills { get; set; }
        public double KillDeathRatio { get; set; }
        public long DinoKills { get; set; }
        public long WildDinoKills { get; set; }
        public long DinosTamed { get; set; }
        public long DeathByPlayer { get; set; }
        public long DeathByDino { get; set; }
        public long DeathByWildDino { get; set; }

    }
}
