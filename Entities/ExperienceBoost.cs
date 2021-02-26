using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Entities
{
    public class ExperienceBoost
    {
        public int Id { get; set; }
        public long SteamId { get; set; }
        public double Multiplier { get; set; }
        public string Type { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
