using System;
using System.Collections.Generic;
using System.Text;

namespace PermissionRanks.Exceptions
{
    class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string message) : base(message)
        {

        }
    }
}
