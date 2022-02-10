
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// The service Factory to load service previously registered
  /// </summary>
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
    /// The method to create new Service
    /// </summary>
    /// <param name="serviceType">Future value no usage for now.
    /// This is the Service type reflection for method</param>
    /// <param name="props">Properties for the service containing IConfiguration and TaskOptions</param>
    /// <returns>returns the new instantiated service</returns>
    public Service CreateService(string serviceType, Properties props)
    {
      var factory = new LoggerFactory(new[]
      {
        new SerilogLoggerProvider(new LoggerConfiguration()
                                  .ReadFrom
                                  .Configuration(props.Configuration)
                                  .CreateLogger())
      });
      return new Service(props.Configuration,
                         serviceType,
                         factory,
                         props.TaskOptions);
    }
  }
}