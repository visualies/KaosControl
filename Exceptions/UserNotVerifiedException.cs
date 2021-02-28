using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Exceptions
{
    public class UserNotVerifiedException : Exception
    {
        public UserNotVerifiedException(string message) : base(message)
        {

        }
    }
}
