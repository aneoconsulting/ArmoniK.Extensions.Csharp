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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.Api.Worker.Utils;
using ArmoniK.Api.Worker.Worker;

using WorkerApiException = ArmoniK.DevelopmentKit.Common.Exceptions.WorkerApiException;
using Grpc.Core;

using LoggerExt = ArmoniK.DevelopmentKit.Common.LoggerExt;

namespace ArmoniK.DevelopmentKit.WorkerApi.Services
{
  public class ComputerService : WorkerStreamWrapper
  {
    private ILogger<ComputerService> Logger { get; set; }

    public ServiceRequestContext ServiceRequestContext { get; private set; }

    public IConfiguration Configuration { get; }

    public ComputerService(IConfiguration        configuration,
                           GrpcChannelProvider provider,
                           ServiceRequestContext serviceRequestContext) : base(serviceRequestContext.LoggerFactory, provider)
    {
      Configuration         = configuration;
      Logger                = serviceRequestContext.LoggerFactory.CreateLogger<ComputerService>();
      ServiceRequestContext = serviceRequestContext;
    }

    public override async Task<Output> Process(ITaskHandler taskHandler)
    {
      using var scopedLog = LoggerExt.BeginNamedScope(Logger,
                                                      "Execute task",
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
        LoggerExt.BeginPropertyScope(Logger,
                                     ("TaskId", taskId.Task),
                                     ("SessionId", sessionIdCaller));

        Logger.LogInformation($"Receive new task Session        {sessionIdCaller} -> task {taskId}");
        Logger.LogInformation($"Previous Session#SubSession was {ServiceRequestContext.SessionId?.Id ?? "NOT SET"}");

        var keyOkList = new[]
          {
            AppsOptions.GridAppNameKey, AppsOptions.GridAppVersionKey, AppsOptions.GridAppNamespaceKey,
          }.Select(key => (key,
                     val: taskHandler.TaskOptions.Options
                                     .ContainsKey(key)))
           .ToArray();

        if (keyOkList.Any(el => el.val == false))
        {
          throw new WorkerApiException(
            $"Error in TaskOptions.Options : One of Keys is missing [{string.Join(";", keyOkList.Where(x => x.Item2 == false).Select(el => $"{el.key} => {el.val}"))}]");
        }


        var fileName          = $"{taskHandler.TaskOptions.Options[AppsOptions.GridAppNameKey]}-v{taskHandler.TaskOptions.Options[AppsOptions.GridAppVersionKey]}.zip";
        var localDirectoryZip = $"{Configuration["target_data_path"]}";

        var engineTypeName = taskHandler.TaskOptions.Options.ContainsKey(AppsOptions.EngineTypeNameKey)
          ? taskHandler.TaskOptions.Options[AppsOptions.EngineTypeNameKey]
          : EngineType.Symphony.ToString();

        if (!taskHandler.TaskOptions.Options.ContainsKey(AppsOptions.GridAppNamespaceKey))
        {
          throw new WorkerApiException("Cannot find namespace service in TaskOptions. Please set the namespace");
        }

        var _ = taskHandler.TaskOptions.Options.ContainsKey(AppsOptions.GridAppNamespaceKey)
          ? taskHandler.TaskOptions.Options[AppsOptions.GridAppNamespaceKey]
          : "UnknownNamespaceService avoid previous validation !!";

        var fileAdaptater = ServiceRequestContext.CreateOrGetFileAdaptater(Configuration,
                                                                           localDirectoryZip);


        var serviceId = ServiceRequestContext.CreateOrGetArmonikService(Configuration,
                                                                        engineTypeName,
                                                                        fileAdaptater,
                                                                        fileName,
                                                                        taskHandler.TaskOptions.Options);

        var serviceWorker = ServiceRequestContext.GetService(serviceId);


        if (ServiceRequestContext.IsNewSessionId(sessionIdCaller))
        {
          ServiceRequestContext.SessionId = sessionIdCaller;

          serviceWorker.CloseSession();

          serviceWorker.InitializeSessionWorker(ServiceRequestContext.SessionId,
                                                taskHandler.TaskOptions.Options);
        }

        ServiceRequestContext.SessionId = sessionIdCaller;

        Logger.LogInformation($"Executing task");
        var sw     = Stopwatch.StartNew();
        var result = serviceWorker.Execute(taskHandler);

        if (result != null)
        {
          await taskHandler.SendResult(taskHandler.ExpectedResults.Single(),
                                       result);
        }


        LoggerExt.BeginPropertyScope(Logger,
                                     ("Elapsed", sw.ElapsedMilliseconds / 1000.0));
        Logger.LogInformation($"Executed task");


        output = new Output
        {
          Ok     = new Empty(),
        };
      }
      catch (WorkerApiException ex)
      {
        Logger.LogError(ex,
                        "WorkerAPIException failure while executing task");

        throw new RpcException(new Status(StatusCode.Aborted,
                                          ex.Message + Environment.NewLine + ex.StackTrace));
      }

      catch (Exception ex)
      {
        Logger.LogError(ex,
                        "Unmanaged exception while executing task");

        throw new RpcException(new Status(StatusCode.Aborted,
                                          ex.Message + Environment.NewLine + ex.StackTrace));
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