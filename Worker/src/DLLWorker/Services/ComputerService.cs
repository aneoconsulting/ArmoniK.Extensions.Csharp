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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

using Grpc.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker.Services;

public class ComputerService : WorkerStreamWrapper
{
  public ComputerService(IConfiguration configuration,
                         GrpcChannelProvider provider,
                         ServiceRequestContext serviceRequestContext)
    : base(serviceRequestContext.LoggerFactory,
           provider)
  {
    Configuration = configuration;
    Logger = serviceRequestContext.LoggerFactory.CreateLogger<ComputerService>();
    ServiceRequestContext = serviceRequestContext;
    Logger.LogInformation("Starting worker...OK");
  }

  private ILogger<ComputerService> Logger { get; }

  public ServiceRequestContext ServiceRequestContext { get; }

  public IConfiguration Configuration { get; }

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
      if (new[]
          {
            (nameof(taskHandler.TaskOptions.ApplicationName), string.IsNullOrEmpty(taskHandler.TaskOptions.ApplicationName)),
            (nameof(taskHandler.TaskOptions.ApplicationVersion), string.IsNullOrEmpty(taskHandler.TaskOptions.ApplicationVersion)),
            (nameof(taskHandler.TaskOptions.ApplicationNamespace), string.IsNullOrEmpty(taskHandler.TaskOptions.ApplicationNamespace)),
          }.Where(x => x.Item2)
           .ToArray() is var missingKeys && missingKeys.Any())
      {
        throw new WorkerApiException($"Error in TaskOptions : One of Keys is missing [{string.Join(";", missingKeys.Select(el => $"{el.Item1} => {el.Item2}"))}]");
      }

      var fileName = $"{taskHandler.TaskOptions.ApplicationName}-v{taskHandler.TaskOptions.ApplicationVersion}.zip";
      var localDirectoryZip = $"{Configuration[AppsOptions.GridDataVolumesKey]}";

      var engineTypeName = string.IsNullOrEmpty(taskHandler.TaskOptions.EngineType)
                             ? EngineType.Symphony.ToString()
                             : taskHandler.TaskOptions.EngineType;

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

        serviceWorker.InitializeSessionWorker(ServiceRequestContext.SessionId,
                                              taskHandler.TaskOptions);
      }

      ServiceRequestContext.SessionId = sessionIdCaller;

      Logger.LogInformation("Executing task");
      var sw = Stopwatch.StartNew();
      var result = serviceWorker.Execute(taskHandler);

      if (result != null)
      {
        await taskHandler.SendResult(taskHandler.ExpectedResults.Single(),
                                     result);
      }


      Logger.BeginPropertyScope(("Elapsed", sw.ElapsedMilliseconds / 1000.0));
      Logger.LogInformation("Executed task");


      output = new Output
      {
        Ok = new Empty(),
      };
    }
    catch (WorkerApiException ex)
    {
      Logger.LogError(ex,
                      "WorkerAPIException failure while executing task");

      return new Output
             {
               Error = new Output.Types.Error
                       {
                         Details = ex.Message + Environment.NewLine + ex.StackTrace,
                       },
             };
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
    var level = 1;
    var current = e;
    List<Exception> exList = new()
                             {
                               e,
                             };

    while (current.InnerException != null)
    {
      current = current.InnerException;
      exList.Add(current);
      level++;

      if (level > 30)
      {
        break;
      }
    }

    exList.Reverse();
    var message = $"Root Exception cause : {exList[0].GetType()} | message : {exList[0].Message}" +
                  $"\n\tReversed StackTrace : \n\t{string.Join("\n\t", exList[0].StackTrace)}";

    exList.RemoveAt(0);


    foreach (var exception in exList)
    {
      message += $"\nFrom Exception : {exception.GetType()} message : {exception.Message}\n\t{string.Join("\n\t", exception.StackTrace)}";
    }

    return message;
  }
}
