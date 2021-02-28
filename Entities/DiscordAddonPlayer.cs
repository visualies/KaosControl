using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Models
{
    public class DiscordAddonPlayer
    {
        public long SteamId { get; set; }
        public string UserName { get; set; }
        public int secret { get; set; }
        public ulong? discid { get; set; }

    }
}
