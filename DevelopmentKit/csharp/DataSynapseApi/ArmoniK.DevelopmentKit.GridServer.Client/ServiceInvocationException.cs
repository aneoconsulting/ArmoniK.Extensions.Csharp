using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
    public class ServiceInvocationException : WorkerApiException
    {
      public ServiceInvocationException(Exception exception) : base(exception)
      {
        
      }
    }
}
