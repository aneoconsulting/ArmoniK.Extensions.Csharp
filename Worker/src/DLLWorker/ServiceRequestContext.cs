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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker;

public class ArmonikServiceWorker : IDisposable
{
  public ArmonikServiceWorker()
    => Initialized = false;

  public ServiceId ServiceId { get; set; }

  public AppsLoader  AppsLoader { get; set; }
  public IGridWorker GridWorker { get; set; }

  public bool Initialized { get; set; }

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public void Dispose()
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker?.Dispose();
    }

    GridWorker = null;
    AppsLoader.Dispose();
    AppsLoader  = null;
    Initialized = false;
  }

  public void CloseSession()
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker?.SessionFinalize();
    }
  }

  public void Configure(IConfiguration configuration,
                        TaskOptions    requestTaskOptions)
  {
    if (Initialized)
    {
      return;
    }

    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker.Configure(configuration,
                           requestTaskOptions,
                           AppsLoader);
    }

    Initialized = true;
  }

  public void InitializeSessionWorker(Session     sessionId,
                                      TaskOptions taskHandlerTaskOptions)
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker.InitializeSessionWorker(sessionId,
                                         taskHandlerTaskOptions);
    }
  }

  public byte[] Execute(ITaskHandler taskHandler)
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      return GridWorker.Execute(taskHandler);
    }
  }


  /// <summary>
  ///   Call the GridWorker callback to let the user know when the service will be unloaded
  /// </summary>
  public void DestroyService()
  {
    using (AppsLoader.UserAssemblyLoadContext.EnterContextualReflection())
    {
      GridWorker.DestroyService();
    }
  }
}

public class ServiceRequestContext
{
  private readonly ILogger<ServiceRequestContext> logger_;

  public ServiceRequestContext(ILoggerFactory loggerFactory)
  {
    LoggerFactory  = loggerFactory;
    CurrentService = null;
    logger_        = loggerFactory.CreateLogger<ServiceRequestContext>();
  }

  public Session SessionId { get; set; }

  public ILoggerFactory LoggerFactory { get; set; }

  /// <summary>
  ///   Gets the current service worker.
  /// </summary>
  [CanBeNull]
  public ArmonikServiceWorker? CurrentService { get; private set; }

  public bool IsNewSessionId(Session sessionId)
  {
    if (SessionId == null)
    {
      return true;
    }

    return SessionId.Id != sessionId.Id;
  }

  public bool IsNewSessionId(string sessionId)
  {
    if (sessionId == null)
    {
      throw new ArgumentNullException(nameof(sessionId));
    }

    if (SessionId == null)
    {
      return true;
    }

    var currentSessionId = new Session
                           {
                             Id = sessionId,
                           };

    return IsNewSessionId(currentSessionId);
  }

  public ArmonikServiceWorker CreateOrGetArmonikService(IConfiguration            configuration,
                                                        ApplicationPackageManager appPackageManager,
                                                        string                    engineTypeName,
                                                        PackageId                 packageId,
                                                        TaskOptions               requestTaskOptions)
  {
    if (string.IsNullOrEmpty(requestTaskOptions.ApplicationNamespace))
    {
      throw new WorkerApiException("Cannot find namespace service in TaskOptions. Please set the namespace");
    }

    var serviceId = new ServiceId(packageId,
                                  requestTaskOptions.ApplicationNamespace,
                                  EngineTypeHelper.ToEnum(engineTypeName));

    if (CurrentService?.ServiceId == serviceId)
    {
      return CurrentService;
    }

    logger_.LogInformation($"Worker needs to load new context, from {CurrentService?.ServiceId.ToString() ?? "null"} to {serviceId}");

    CurrentService?.DestroyService();
    CurrentService?.Dispose();
    CurrentService = null;


    var appsLoader = new AppsLoader(appPackageManager,
                                    LoggerFactory,
                                    engineTypeName,
                                    packageId);

    CurrentService = new ArmonikServiceWorker
                     {
                       AppsLoader = appsLoader,
                       GridWorker = appsLoader.GetGridWorkerInstance(configuration,
                                                                     LoggerFactory),
                       ServiceId = serviceId,
                     };

    CurrentService.Configure(configuration,
                             requestTaskOptions);

    return CurrentService;
  }
}
