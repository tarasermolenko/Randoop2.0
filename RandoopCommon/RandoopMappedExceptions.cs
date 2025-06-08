using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Serializable]
    public class InvalidFileFormatException : Exception
    {
        public InvalidFileFormatException(string message)
            : base("Method Mapping File Format is invalid: " + message)
        {
        }

    }
}
