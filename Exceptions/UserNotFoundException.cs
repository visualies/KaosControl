using System;
using System.Collections.Generic;
using System.Text;

namespace PermissionRanks.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string message) : base(message)
        {

        }
    }
}
