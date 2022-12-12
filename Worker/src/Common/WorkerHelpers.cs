using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

namespace ArmoniK.DevelopmentKit.Worker.Common;

public class WorkerHelpers
{
  public static IConfiguration GetDefaultConfiguration()
  {
    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();

    return builder.Build();
  }

  public static ILoggerFactory GetDefaultLoggerFactory(IConfiguration configuration = null)
  {
    configuration ??= GetDefaultConfiguration();

    var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(configuration)
                                                .WriteTo.Console(new CompactJsonFormatter())
                                                .Enrich.FromLogContext()
                                                .CreateLogger();
    return LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(loggerConfig));
  }
}
