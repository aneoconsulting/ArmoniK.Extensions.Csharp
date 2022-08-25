using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public class GridWorkerExt
  {
    public static IConfiguration GetDefaultConfiguration()
    {
      var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json",
                                 true,
                                 false)
                    .AddEnvironmentVariables();

      return builder.Build();
    }

    public static ILoggerFactory GetDefaultLoggerFactory(IConfiguration configuration = null)
    {
      configuration ??= GetDefaultConfiguration();

      var loggerConfig = new LoggerConfiguration().ReadFrom.KeyValuePairs(configuration.AsEnumerable())
                                            .WriteTo.Console(new CompactJsonFormatter())
                                            .Enrich.FromLogContext()
                                            .CreateLogger();
      return LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(loggerConfig));

    }
  }

  /// <summary>
  /// An Optional Base Service Container when Developer wants to
  /// manage Armonik tools coming from the main workerAPI  object
  /// </summary>
  public class WorkerHelpers
  {
    public ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// The constructor that will be call from the inherited services
    /// </summary>
    /// <param name="loggerFactory"></param>
    public WorkerHelpers(ILoggerFactory loggerFactory)
    {
      LoggerFactory = loggerFactory;
    }
  }
}
