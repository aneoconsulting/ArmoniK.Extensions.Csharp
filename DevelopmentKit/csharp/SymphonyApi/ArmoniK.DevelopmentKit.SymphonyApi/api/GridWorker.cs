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
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Google.Protobuf.Collections;

using Microsoft.Extensions.Configuration;


namespace ArmoniK.DevelopmentKit.SymphonyApi
{
  public class GridWorker : IGridWorker
  {
    private IServiceContainer serviceContainer;
    private SessionContext    sessionContext;
    private ServiceContext    serviceContext;

    public void Configure(IConfiguration configuration, IDictionary<string, string> taskOptions, AppsLoader appsLoader)
    {
      Configuration = configuration;

      GridAppName      = taskOptions[AppsOptions.GridAppNameKey];
      GridAppVersion   = taskOptions[AppsOptions.GridAppVersionKey];
      GridAppNamespace = taskOptions[AppsOptions.GridAppNamespaceKey];


      sessionContext                  = new SessionContext();
      serviceContext                  = new ServiceContext();
      serviceContext.ApplicationName  = GridAppName;
      sessionContext.clientLibVersion = GridAppVersion;
      serviceContext.ServiceName      = $"{GridAppName}-{GridAppVersion}-Service";

      serviceContainer = appsLoader.getServiceContainerInstance<IServiceContainer>(GridAppNamespace,
                                                                                   "ServiceContainer");

      serviceContainer.ClientService = new ArmonikSymphonyClient(configuration);


      OnCreateService();
    }

    public string GridAppNamespace { get; set; }

    public string GridAppVersion { get; set; }

    public string GridAppName { get; set; }

    public IConfiguration Configuration { get; set; }


    public string SessionId { get; set; }
    
    public string TaskId { get; set; }

public void InitializeSessionWorker(string sessionId)
    {
    }

    public void OnCreateService()
    {
      serviceContainer.OnCreateService(serviceContext);
    }

    /// <summary>
    /// The internal function onSessionEnter to openSession for clientService under GridWorker
    /// </summary>
    /// <param name="session"></param>
    public void OnSessionEnter(string session)
    {
      sessionContext.SessionId = session;

      if (serviceContainer.SessionId == null || string.IsNullOrEmpty(serviceContainer.SessionId.Session))
      {
        serviceContainer.SessionId = session?.UnPackId();
        serviceContainer.ClientService.OpenSession(session);
      }

      serviceContainer.OnSessionEnter(sessionContext);
    }

    public byte[] Execute(string session, ComputeRequest request)
    {
      if (String.IsNullOrEmpty(SessionId) || !session.Equals(SessionId))
      {
        if (String.IsNullOrEmpty(SessionId))
        {
          OnSessionEnter(session);
        }
        else
        {
          OnSessionLeave();
          OnSessionEnter(session);
        }
      }

      
      TaskId = request.TaskId;

      SessionId = session;

      TaskContext taskContext = new TaskContext();
      taskContext.TaskId    = request.TaskId;
      taskContext.TaskInput = request.Payload.ToByteArray();
      taskContext.SessionId = session;
      taskContext.ParentIds = request.Dependencies;

      serviceContainer.TaskId = request.TaskId;

      byte[] clientPayload = serviceContainer.OnInvoke(sessionContext,
                                                       taskContext);

      // Return to user the taskId, could be any other information
      return clientPayload;
    }


    public void SessionFinilize()
    {
      OnSessionLeave();
    }

    public void OnSessionLeave()
    {
      if (sessionContext != null)
      {
        serviceContainer.OnSessionLeave(sessionContext);
        SessionId      = null;
        sessionContext = null;
      }
    }

    public void OnExit()
    {
      OnSessionLeave();

      if (serviceContext != null)
      {
        serviceContainer.OnDestroyService(serviceContext);
        serviceContext = null;
        SessionId      = null;
      }
    }
  }
}