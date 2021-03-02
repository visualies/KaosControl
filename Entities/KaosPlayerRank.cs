using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Entities
{
    public class KaosPlayerRank
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int RequiredExperience { get; set; }
        public string PermissionGroup { get; set; }
    }
}
