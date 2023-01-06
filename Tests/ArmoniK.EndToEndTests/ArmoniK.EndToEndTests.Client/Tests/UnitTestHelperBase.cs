using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Serilog;
using Serilog.Extensions.Logging;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ArmoniK.EndToEndTests.Client.Tests;

internal abstract class UnitTestHelperBase
{
  private readonly ConcurrentDictionary<string, object> expectedResults_ = new();
  protected        Properties                           Props;

  public UnitTestHelperBase(EngineType engineType,
                            string     applicationNamespace,
                            string     applicationService)
  {
    InitConfig();
    InitLogger();
    InitProperties(engineType,
                   applicationNamespace,
                   applicationService);
  }

  public TaskOptions? TaskOptions { get; protected set; }

  public    ILogger         Log           { get; private set; }
  protected ILoggerFactory? LoggerFactory { get; set; }

  protected IConfiguration Configuration { get; set; }

  public void InitConfig()
  {
    var builder = new ConfigurationBuilder().SetBasePath(TestContext.CurrentContext.TestDirectory)
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();


    Configuration = builder.Build();
  }

  public void InitLogger()
  {
    LoggerFactory = new LoggerFactory(new[]
                                      {
                                        new SerilogLoggerProvider(new LoggerConfiguration().ReadFrom.Configuration(Configuration)
                                                                                           .CreateLogger()),
                                      },
                                      new LoggerFilterOptions().AddFilter("Grpc",
                                                                          LogLevel.Trace));


    Log = LoggerFactory.CreateLogger<Program>();


    Log.LogInformation("Configure taskOptions");
  }

  public void InitProperties(EngineType engineType,
                             string     applicationNamespace,
                             string     applicationService)
  {
    TaskOptions = InitializeTaskOptions(engineType,
                                        applicationNamespace,
                                        applicationService);

    Props = new Properties(TaskOptions,
                           Configuration.GetSection("Grpc")["EndPoint"],
                           5001);
  }

  public static object?[] ParamsHelper(params object?[] elements)
    => elements;


  protected TaskOptions? InitializeTaskOptions(EngineType engineType,
                                               string     applicationNamespace,
                                               string     applicationService)
    => new()
       {
         MaxDuration = new Duration
                       {
                         Seconds = 300,
                       },
         MaxRetries      = 5,
         Priority        = 1,
         PartitionId     = Environment.GetEnvironmentVariable("PARTITION") ?? "",
         ApplicationName = "ArmoniK.EndToEndTests.Worker",
         ApplicationVersion = Regex.Replace(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()
                                                                                   .Location)
                                                           .ProductVersion,
                                            @"\+.*", // Remove Hash build From Version
                                            "") ?? "1.0.0-700",
         ApplicationNamespace = applicationNamespace,
         ApplicationService   = applicationService,
         EngineType           = engineType.ToString(),
       };
}
