using System.IO;

using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Grpc.Core;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
    public class ServiceFactory
    {
      private ServiceFactory()
      {
      }

      private static   ServiceFactory _instanceFactory;

      public static ServiceFactory GetInstance()
      {
        if (_instanceFactory == null) _instanceFactory = new ServiceFactory();

        return _instanceFactory;
      }

      public Service CreateService(string serviceType, Properties props)
      {
        return new Service(props.Configuration,
                           serviceType, props.TaskOptions);
      }
    }
}
