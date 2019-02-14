using System;
using System.Runtime.Serialization;

namespace OctopusTest.BusinessLogic.Contract.Exceptions
{
    public abstract class BusinessLogicException : Exception
    {
        protected BusinessLogicException()
        {
        }

        protected BusinessLogicException(string message)
            : base(message)
        {
        }

        protected BusinessLogicException(string message,
                                         Exception innerException)
            : base(message,
                   innerException)
        {
        }

        protected BusinessLogicException(SerializationInfo info,
                                         StreamingContext context)
            : base(info,
                   context)
        {
        }
    }
}