// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.Extensions.Common.StreamWrapper.Worker;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.WorkerApi.Services
{
  public class ComputerService : WorkerStreamWrapper
  {
    private ILogger<ComputerService> Logger { get; set; }

    public ServiceRequestContext ServiceRequestContext { get; private set; }

    public IConfiguration Configuration { get; }

    public ComputerService(IConfiguration        configuration,
                           ServiceRequestContext serviceRequestContext) : base(serviceRequestContext.LoggerFactory)
    {
      Configuration         = configuration;
      Logger                = serviceRequestContext.LoggerFactory.CreateLogger<ComputerService>();
      ServiceRequestContext = serviceRequestContext;
    }

    public override async Task<Output> Process(ITaskHandler taskHandler)
    {
      using var scopedLog = Logger.BeginNamedScope("Execute task",
                                                    ("Session", taskHandler.SessionId),
                                                    ("TaskId", taskHandler.TaskId));
      Logger.LogTrace("DataDependencies {DataDependencies}",
                       taskHandler.DataDependencies.Keys);
      Logger.LogTrace("ExpectedResults {ExpectedResults}",
                       taskHandler.ExpectedResults);

      Output output;
      try
      {
        var sessionIdCaller = new Session
        {
          Id = taskHandler.SessionId,
        };
        var taskId = new TaskId
        {
          Task = taskHandler.TaskId,
        };
        Logger.BeginPropertyScope(("TaskId", taskId.Task),
                                  ("SessionId", sessionIdCaller));

        Logger.LogInformation($"Receive new task Session        {sessionIdCaller} -> task {taskId}");
        Logger.LogInformation($"Previous Session#SubSession was {ServiceRequestContext.SessionId?.Id ?? "NOT SET"}");

        var fileName          = $"{taskHandler.TaskOptions[AppsOptions.GridAppNameKey]}-v{taskHandler.TaskOptions[AppsOptions.GridAppVersionKey]}.zip";
        var localDirectoryZip = $"{Configuration["target_data_path"]}";

        var engineTypeName = taskHandler.TaskOptions.ContainsKey(AppsOptions.EngineTypeNameKey)
          ? taskHandler.TaskOptions[AppsOptions.EngineTypeNameKey]
          : EngineType.Symphony.ToString();

        if (!taskHandler.TaskOptions.ContainsKey(AppsOptions.GridAppNamespaceKey))
        {
          throw new WorkerApiException("Cannot find namespace service in TaskOptions. Please set the namespace");
        }

        var _ = taskHandler.TaskOptions.ContainsKey(AppsOptions.GridAppNamespaceKey)
          ? taskHandler.TaskOptions[AppsOptions.GridAppNamespaceKey]
          : "UnknownNamespaceService avoid previous validation !!";

        var fileAdaptater = ServiceRequestContext.CreateOrGetFileAdaptater(Configuration,
                                                                           localDirectoryZip);


        var serviceId = ServiceRequestContext.CreateOrGetArmonikService(Configuration,
                                                                        engineTypeName,
                                                                        fileAdaptater,
                                                                        fileName,
                                                                        taskHandler.TaskOptions);

        var serviceWorker = ServiceRequestContext.GetService(serviceId);


        if (ServiceRequestContext.IsNewSessionId(sessionIdCaller))
        {
          ServiceRequestContext.SessionId = sessionIdCaller;

          serviceWorker.CloseSession();

          serviceWorker.GridWorker.InitializeSessionWorker(ServiceRequestContext.SessionId,
                                                           taskHandler.TaskOptions);
        }

        ServiceRequestContext.SessionId = sessionIdCaller;

        Logger.LogInformation($"Executing task");

        var result = serviceWorker.GridWorker.Execute(ServiceRequestContext.SessionId,
                                                      taskHandler);
        if (result != null && result.Length != 0)
        {
          await taskHandler.SendResult(taskHandler.ExpectedResults.Single(),
                                       result);
        }

        output = new Output
        {
          Ok     = new Empty(),
          Status = TaskStatus.Completed,
        };
      }
      catch (Exception ex)
      {
        Logger.LogError(ex,
                         "Error while computing task");

        output = new Output
        {
          Error = new Output.Types.Error
          {
            Details      = ex.Message + ex.StackTrace,
            KillSubTasks = true,
          },
          Status = TaskStatus.Error,
        };
      }

      return output;
    }

    private static string ExtractException(Exception e)
    {
      var             level   = 1;
      var             current = e;
      List<Exception> exList  = new();
      exList.Add(e);

      while (current.InnerException != null)
      {
        current = current.InnerException;
        exList.Add(current);
        level++;

        if (level > 30)
          break;
      }

      exList.Reverse();
      var message = $"Root Exception cause : {exList[0].GetType()} | message : {exList[0].Message}" +
                    $"\n\tReversed StackTrace : \n\t{string.Join("\n\t", exList[0].StackTrace)}";

      exList.RemoveAt(0);


      foreach (var exception in exList)
      {
        message +=
          $"\nFrom Exception : {exception.GetType()} message : {exception.Message}\n\t{string.Join("\n\t", exception.StackTrace)}";
      }

      return message;
    }
  }
}