using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Exceptions
{
    class UserNotVerifiedException : Exception
    {
        public UserNotVerifiedException(string message) : base(message)
        {

        }
    }
}
