using KaosControl.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Events
{
    public class UserRankUpEventArgs : EventArgs
    {
        public KaosUser User { get; set; }
        public KaosRank Rank { get; set; }

    }
}
