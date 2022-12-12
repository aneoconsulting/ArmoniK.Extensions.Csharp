// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Common;

[PublicAPI]
public abstract class ClientBaseTest<T>
{
  public ClientBaseTest(IConfiguration configuration,
                        ILoggerFactory loggerFactory)
  {
    Configuration = configuration;

    LoggerFactory = loggerFactory;

    Log = LoggerFactory.CreateLogger<T>();
  }

  protected IConfiguration Configuration { get; set; }

  protected static ILogger<T> Log { get; set; }

  protected ILoggerFactory LoggerFactory { get; set; }

  protected virtual TaskOptions InitializeTaskOptions()
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
         ApplicationNamespace = typeof(T).Namespace.Replace("Client",
                                                            "Worker"),
         ApplicationService = (typeof(T).Name + "Worker").Replace("Client",
                                                                  ""),

         EngineType = EngineType.Symphony.ToString(),
       };


  public abstract void EntryPoint();
}

[PublicAPI]
public static class TaskOptionsExt
{
  public static TaskOptions SetVersion(this TaskOptions taskOptions,
                                       string           version)
  {
    taskOptions.ApplicationVersion = version;

    return taskOptions;
  }

  public static TaskOptions SetEngineType(this TaskOptions taskOptions,
                                          string           engineType)
  {
    taskOptions.EngineType = engineType;

    return taskOptions;
  }

  public static TaskOptions SetNamespaceService(this TaskOptions taskOptions,
                                                string           namespaceService)
  {
    taskOptions.ApplicationNamespace = namespaceService;

    return taskOptions;
  }

  public static TaskOptions SetApplicationName(this TaskOptions taskOptions,
                                               string           applicationName)
  {
    taskOptions.ApplicationName = applicationName;

    return taskOptions;
  }

  public static TaskOptions SetSessionPriority(this TaskOptions taskOptions,
                                               int              priority)
  {
    taskOptions.Priority = priority;

    return taskOptions;
  }
}

