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

using System.Linq;
using System.Runtime.Loader;

using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Worker.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.Worker.Symphony;

[XmlDocIgnore]
public class GridWorker : IGridWorker
{
  private ServiceContainerBase serviceContainerBase_;
  private ServiceContext serviceContext_;
  private SessionContext sessionContext_;

  public GridWorker()
  {
    Configuration = GridWorkerExt.GetDefaultConfiguration();
    Logger = GridWorkerExt.GetDefaultLoggerFactory(Configuration)
                          .CreateLogger<GridWorker>();
  }

  public GridWorker(IConfiguration configuration,
                    ILoggerFactory factory)
  {
    Configuration = configuration;

    Logger = factory.CreateLogger<GridWorker>();
  }

  private ILogger<GridWorker> Logger { get; }

  public TaskOptions TaskOptions { get; set; }

  public string GridAppNamespace { get; set; }

  public string GridAppVersion { get; set; }

  public string GridAppName { get; set; }

  public IConfiguration Configuration { get; set; }


  public Session SessionId { get; set; }

  public TaskId TaskId { get; set; }

  public void Configure(IConfiguration configuration,
                        TaskOptions clientOptions,
                        IAppsLoader appsLoader)
  {
    GridAppName = clientOptions.ApplicationName;
    GridAppVersion = clientOptions.ApplicationVersion;
    GridAppNamespace = clientOptions.ApplicationNamespace;

    serviceContext_ = new ServiceContext
    {
      ApplicationName = GridAppName,
      ServiceName = $"{GridAppName}-{GridAppVersion}-Service",
      ClientLibVersion = GridAppVersion,
      AppNamespace = GridAppNamespace,
    };


    Logger.LogInformation("Loading ServiceContainer from Application package :  " + $"\n\tappName   :   {GridAppName}" + $"\n\tversion   :   {GridAppVersion}" +
                          $"\n\tnameSpace :   {GridAppNamespace}");

    serviceContainerBase_ = appsLoader.GetServiceContainerInstance<ServiceContainerBase>(GridAppNamespace,
                                                                                         "ServiceContainer");

    serviceContainerBase_.Configure(configuration,
                                    clientOptions);
    Logger.LogDebug("Call OnCreateService");

    OnCreateService();
  }

  public void InitializeSessionWorker(Session sessionId,
                                      TaskOptions requestTaskOptions)
  {
    Logger.BeginPropertyScope(("SessionId", sessionId));


    serviceContainerBase_.Logger.BeginPropertyScope(("SessionId", sessionId));

    if (SessionId == null || !sessionId.Equals(SessionId))
    {
      if (SessionId == null)
      {
        SessionId = sessionId;
        serviceContainerBase_.ConfigureSession(SessionId,
                                               requestTaskOptions);
        OnSessionEnter(sessionId);
      }
      else
      {
        OnSessionLeave();
        SessionId = sessionId;
        serviceContainerBase_.ConfigureSession(SessionId,
                                               requestTaskOptions);
        OnSessionEnter(sessionId);
      }
    }
  }

  public byte[] Execute(ITaskHandler taskHandler)
  {
    TaskId = new TaskId
    {
      Task = taskHandler.TaskId,
    };

    Logger.BeginPropertyScope(("TaskId", TaskId));


    serviceContainerBase_.Logger.BeginPropertyScope(("TaskId", TaskId.Task));

    var taskContext = new TaskContext
    {
      TaskId = TaskId.Task,
      TaskInput = taskHandler.Payload,
      SessionId = taskHandler.SessionId,
      DependenciesTaskIds = taskHandler.DataDependencies.Select(t => t.Key),
      DataDependencies = taskHandler.DataDependencies,
      TaskOptions = taskHandler.TaskOptions,
    };

    serviceContainerBase_.ConfigureSessionService(taskHandler);
    serviceContainerBase_.TaskId = TaskId;
    Logger.LogInformation("Check Enrich with taskId");
    var clientPayload = serviceContainerBase_.OnInvoke(sessionContext_,
                                                       taskContext);

    // Return to user the taskId, could be any other information
    return clientPayload;
  }


  public void SessionFinalize()
    => OnSessionLeave();

  public void DestroyService()
    => OnDestroyService();

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public void Dispose()
    => OnDestroyService();

  public void OnCreateService()
  {
    using (AssemblyLoadContext.EnterContextualReflection(serviceContainerBase_.GetType()
                                                                              .Assembly))
    {
      serviceContainerBase_.OnCreateService(serviceContext_);
    }
  }

  /// <summary>
  ///   The internal function onSessionEnter to openSession for clientService under GridWorker
  /// </summary>
  /// <param name="session"></param>
  public void OnSessionEnter(Session session)
  {
    sessionContext_ = new SessionContext
    {
      ClientLibVersion = GridAppVersion,
      SessionId = session.Id,
    };
    SessionId = session;

    if (serviceContainerBase_.SessionId == null || string.IsNullOrEmpty(serviceContainerBase_.SessionId.Id))
    {
      serviceContainerBase_.SessionId = session;
    }

    serviceContainerBase_.SessionId = session;

    serviceContainerBase_.OnSessionEnter(sessionContext_);
  }

  public void OnSessionLeave()
  {
    if (sessionContext_ != null)
    {
      serviceContainerBase_.OnSessionLeave(sessionContext_);
      SessionId = null;
      sessionContext_ = null;
    }
  }

  public void OnDestroyService()
  {
    OnSessionLeave();

    if (serviceContext_ != null)
    {
      serviceContainerBase_.OnDestroyService(serviceContext_);
      serviceContext_ = null;
      SessionId = null;
    }
  }
}
