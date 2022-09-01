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

using System;
using System.Diagnostics;
using System.Reflection;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Common;

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

  //protected TaskOptions TaskOptions { get; set; }
  protected ILoggerFactory LoggerFactory { get; set; }

  protected virtual TaskOptions InitializeTaskOptions()
  {
    TaskOptions taskOptions = new()
                              {
                                MaxDuration = new Duration
                                              {
                                                Seconds = 300,
                                              },
                                MaxRetries  = 5,
                                Priority    = 1,
                                PartitionId = Environment.GetEnvironmentVariable("PARTITION") ?? "",
                              };

    taskOptions.Options[AppsOptions.GridAppNameKey] = "ArmoniK.EndToEndTests";
    var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()
                                                         .Location)
                                 .ProductVersion;
    //var    version         = typeof(ClientBaseTest<T>).Assembly.GetName().Version;
    if (version != null)
    {
      taskOptions.Options[AppsOptions.GridAppVersionKey] = version;
    }
    else
    {
      taskOptions.Options[AppsOptions.GridAppVersionKey] = "1.0.0-700";
    }

    taskOptions.Options[AppsOptions.GridAppNamespaceKey] = typeof(T).Namespace;

    taskOptions.Options[AppsOptions.EngineTypeNameKey] = EngineType.Symphony.ToString();

    return taskOptions;
  }


  public abstract void EntryPoint();
}

public static class TaskOptionsExt
{
  public static TaskOptions SetVersion(this TaskOptions taskOptions,
                                       string           version)
  {
    taskOptions.Options[AppsOptions.GridAppVersionKey] = version;

    return taskOptions;
  }

  public static TaskOptions SetEngineType(this TaskOptions taskOptions,
                                          string           engineType)
  {
    taskOptions.Options[AppsOptions.EngineTypeNameKey] = engineType;

    return taskOptions;
  }

  public static TaskOptions SetNamespaceService(this TaskOptions taskOptions,
                                                string           namespaceService)
  {
    taskOptions.Options[AppsOptions.GridAppNamespaceKey] = namespaceService;

    return taskOptions;
  }

  public static TaskOptions SetApplicationName(this TaskOptions taskOptions,
                                               string           applicationName)
  {
    taskOptions.Options[AppsOptions.GridAppNameKey] = applicationName;

    return taskOptions;
  }

  public static TaskOptions SetSessionPriority(this TaskOptions taskOptions,
                                               int              priority)
  {
    taskOptions.Priority = priority;

    return taskOptions;
  }
}
