using System;
using System.Collections.Generic;
using System.Text;

namespace KaosControl.Exceptions
{
    public class MoreThanOneEntityFoundException : Exception
    {
        public MoreThanOneEntityFoundException(string message) : base(message)
        {

        }
    }
}
