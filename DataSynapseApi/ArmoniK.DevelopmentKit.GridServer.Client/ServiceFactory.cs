
using ArmoniK.DevelopmentKit.WorkerApi.Common;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  [MarkDownDoc]
  public class ServiceFactory
  {
    private ServiceFactory()
    {
    }

    private static ServiceFactory _instanceFactory;

    /// <summary>
    /// Get a single instance of ServiceFactory to create new Service
    /// </summary>
    /// <returns>Returns the ServiceFactory to create new Service</returns>
    public static ServiceFactory GetInstance()
    {
      if (_instanceFactory == null) _instanceFactory = new ServiceFactory();

      return _instanceFactory;
    }

    /// <summary>
    /// The methode to create new Service
    /// </summary>
    /// <param name="serviceType">Future value no usage for now.
    /// This is the Service type reflection for method</param>
    /// <param name="props">Properties for the service containing IConfiguration and TaskOptions</param>
    /// <returns>returns the new instantiated service</returns>
    public Service CreateService(string serviceType, Properties props)
    {
      return new Service(props.Configuration,
                         serviceType,
                         props.TaskOptions);
    }
  }
}