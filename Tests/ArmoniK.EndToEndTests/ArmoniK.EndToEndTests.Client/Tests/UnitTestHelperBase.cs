// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

  public TaskOptions TaskOptions { get; protected set; }

  public    ILogger        Log           { get; private set; }
  protected ILoggerFactory LoggerFactory { get; set; }

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

    Props = new Properties(Configuration,
                           TaskOptions);
  }

  public static object[] ParamsHelper(params object[] elements)
    => elements;


  protected TaskOptions InitializeTaskOptions(EngineType engineType,
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
