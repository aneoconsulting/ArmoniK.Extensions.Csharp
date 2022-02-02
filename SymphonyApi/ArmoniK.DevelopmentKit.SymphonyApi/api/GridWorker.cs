/* GridWorker.cs is part of the Htc.Mock solution.

   Copyright (c) 2021-2021 ANEO.
     D. DUBUC (https://github.com/ddubuc)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

*/


using System;
using System.Collections.Generic;
using System.Linq;

using ArmoniK.Attributes;
using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.SymphonyApi.api;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.SymphonyApi
{
  [XmlDocIgnore]
  public class GridWorker : IGridWorker
  {
    private ILogger<GridWorker> Logger { get; set; }
    private ServiceContainerBase serviceContainerBase_;
    private ServiceContext       serviceContext_;
    private SessionContext       sessionContext_;

    public GridWorker(IConfiguration configuration, LoggerFactory factory)
    {
      Configuration = configuration;

      Logger = factory.CreateLogger<GridWorker>();
    }

    public TaskOptions TaskOptions { get; set; }

    public string GridAppNamespace { get; set; }

    public string GridAppVersion { get; set; }

    public string GridAppName { get; set; }

    public IConfiguration Configuration { get; set; }


    public string SessionId { get; set; }

    public string TaskId { get; set; }

    public void Configure(IConfiguration configuration, IDictionary<string, string> clientOptions, AppsLoader appsLoader)
    {
      GridAppName      = clientOptions[AppsOptions.GridAppNameKey];
      GridAppVersion   = clientOptions[AppsOptions.GridAppVersionKey];
      GridAppNamespace = clientOptions[AppsOptions.GridAppNamespaceKey];

      serviceContext_ = new()
      {
        ApplicationName  = GridAppName,
        ServiceName      = $"{GridAppName}-{GridAppVersion}-Service",
        ClientLibVersion = GridAppVersion,
        AppNamespace     = GridAppNamespace,
      };


      Logger.LogInformation("Loading ServiceContainer from Application package :  " +
                            $"\n\tappName   :   {GridAppName}" +
                            $"\n\tvers      :   {GridAppVersion}" +
                            $"\n\tnameSpace :   {GridAppNamespace}");

      serviceContainerBase_ = appsLoader.GetServiceContainerInstance<ServiceContainerBase>(GridAppNamespace,
                                                                                           "ServiceContainer");

      serviceContainerBase_.Configure(configuration,
                                      clientOptions);

      Logger.LogDebug("Call OnCreateService");

      OnCreateService();
    }

    public void InitializeSessionWorker(string sessionId)
    {
      if (string.IsNullOrEmpty(SessionId) || !sessionId.Equals(SessionId.UnPackSessionId().Session))
      {
        if (string.IsNullOrEmpty(SessionId))
        {
          OnSessionEnter(sessionId);
        }
        else
        {
          OnSessionLeave();
          OnSessionEnter(sessionId);
        }
      }

      SessionId                       = sessionId;
      serviceContainerBase_.SessionId = sessionId?.UnPackSessionId();
    }

    public byte[] Execute(string session, ComputeRequest request)
    {
      TaskId = new TaskId
      {
        Task       = request.TaskId,
        SubSession = request.Subsession,
      }.PackTaskId();


      var taskContext = new TaskContext
      {
        TaskId    = TaskId,
        TaskInput = request.Payload.ToByteArray(),
        SessionId = session,
        DependenciesTaskIds = request.Dependencies.Select(t =>
                                                            new TaskId
                                                            {
                                                              Task       = t,
                                                              SubSession = request.Subsession,
                                                            }.PackTaskId()),
        ClientOptions = request.TaskOptions,
      };

      serviceContainerBase_.TaskId = TaskId;

      var clientPayload = serviceContainerBase_.OnInvoke(sessionContext_,
                                                         taskContext);

      // Return to user the taskId, could be any other information
      return clientPayload;
    }


    public void SessionFinalize()
    {
      OnSessionLeave();
    }

    public void DestroyService()
    {
      OnDestroyService();
    }

    public void OnCreateService()
    {
      serviceContainerBase_.OnCreateService(serviceContext_);
    }

    /// <summary>
    ///   The internal function onSessionEnter to openSession for clientService under GridWorker
    /// </summary>
    /// <param name="session"></param>
    public void OnSessionEnter(string session)
    {
      sessionContext_ = new()
      {
        ClientLibVersion = GridAppVersion,
        SessionId        = session
      };
      SessionId = session;

      if (serviceContainerBase_.SessionId == null || string.IsNullOrEmpty(serviceContainerBase_.SessionId.Session))
        serviceContainerBase_.SessionId = session?.UnPackSessionId();

      serviceContainerBase_.SessionId = session?.UnPackSessionId();

      serviceContainerBase_.OnSessionEnter(sessionContext_);
    }

    public void OnSessionLeave()
    {
      if (sessionContext_ != null)
      {
        serviceContainerBase_.OnSessionLeave(sessionContext_);
        SessionId       = null;
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
        SessionId       = null;
      }
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
      OnDestroyService();
    }
  }
}