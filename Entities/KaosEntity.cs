using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Entities
{
    public abstract class KaosEntity
    {
        internal KaosClient Client { get; set; }
        public KaosEntity() { }
    }
}
