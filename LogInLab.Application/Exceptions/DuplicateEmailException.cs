using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Application.Exceptions
{
    public class DuplicateEmailException : Exception
    {
        public DuplicateEmailException() : base("The email address is already in use. Please choose a different email address.")
        {
        }
    }
}
