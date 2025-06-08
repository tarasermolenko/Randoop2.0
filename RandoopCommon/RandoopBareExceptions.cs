using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common.RandoopBareExceptions
{
    [Serializable]
    public class InvalidUserParamsException : Exception
    {
        public InvalidUserParamsException(string message)
            : base(Common.Environment.RandoopBareInvalidUserParametersErrorMessage + ": " + message)
        {
        }
    }

    [Serializable]
    public class InternalError : Exception
    {
        public InternalError(string message)
            : base(Common.Environment.RandoopBareInternalErrorMessage + ": " + message)
        {
        }
    }
}