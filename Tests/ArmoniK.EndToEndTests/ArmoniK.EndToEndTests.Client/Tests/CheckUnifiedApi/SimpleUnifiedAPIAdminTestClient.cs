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
using System.Linq;
using System.Threading;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckUnifiedApi;

public class SimpleUnifiedApiAdminTestClient : ClientBaseTest<SimpleUnifiedApiTestClient>, IServiceInvocationHandler
{
  private ILoggerFactory loggerFactory_;

  public SimpleUnifiedApiAdminTestClient(IConfiguration configuration,
                                         ILoggerFactory loggerFactory)
    : base(configuration,
           loggerFactory)
    => loggerFactory_ = loggerFactory;

  /// <summary>
  ///   The callBack method which has to be implemented to retrieve error or exception
  /// </summary>
  /// <param name="e">The exception sent to the client from the control plane</param>
  /// <param name="taskId">The task identifier which has invoke the error callBack</param>
  public void HandleError(ServiceInvocationException e,
                          string                     taskId)
  {
    if (e.StatusCode == ArmonikStatusCode.TaskCancelled)
    {
      Log.LogWarning($"Task canceled : {taskId}. Status {e.StatusCode.ToString()} Message : {e.Message}\nDetails : {e.OutputDetails}");
    }
    else
    {
      Log.LogError($"Fail to get result from {taskId}. Status {e.StatusCode.ToString()} Message : {e.Message}\nDetails : {e.OutputDetails}");

      throw new ApplicationException($"Error from {taskId}",
                                     e);
    }
  }

  /// <summary>
  ///   The callBack method which has to be implemented to retrieve response from the server
  /// </summary>
  /// <param name="response">The object receive from the server as result the method called by the client</param>
  /// <param name="taskId">The task identifier which has invoke the response callBack</param>
  public void HandleResponse(object response,
                             string taskId)
  {
    switch (response)
    {
      case null:
        break;
      case double value:

        break;
      case double[] doubles:

        break;
      case byte[] values:

        break;
    }
  }


  [EntryPoint]
  public override void EntryPoint()
  {
    Log.LogInformation("Configure taskOptions");
    var taskOptions = InitializeTaskOptions();
    OverrideTaskOptions(taskOptions);

    var props = new Properties(taskOptions,
                               Configuration.GetSection("Grpc")["EndPoint"],
                               5001);

    //var resourceId = ServiceAdmin.CreateInstance(Configuration, LoggerFactory,props).UploadResource("filePath");

    using var cs = ServiceFactory.CreateService(props,
                                                LoggerFactory);

    using var csa = ServiceFactory.GetServiceAdmin(props,
                                                   LoggerFactory);

    Log.LogInformation($"New session created : {cs.SessionId}");


    Log.LogInformation("Submit Batch of 100 tasks in one submit call and Cancel the session");
    RunningAndCancelSession(cs,
                            csa);
  }

  private static void OverrideTaskOptions(TaskOptions taskOptions)
  {
    taskOptions.EngineType         = EngineType.Unified.ToString();
    taskOptions.ApplicationService = "CheckUnifiedApiWorker";
  }


  private static object[] ParamsHelper(params object[] elements)
    => elements;

  /// <summary>
  ///   The first test developed to validate the Session cancellation
  /// </summary>
  /// <param name="sessionService"></param>
  /// <param name="serviceAdmin"></param>
  private void RunningAndCancelSession(Service      sessionService,
                                       ServiceAdmin serviceAdmin)
  {
    var numbers = new List<double>
                  {
                    1.0,
                    2.0,
                    3.0,
                    3.0,
                    3.0,
                    3.0,
                    3.0,
                    3.0,
                  }.ToArray();
    const int wantedCount = 100;
    var tasks = sessionService.Submit("ComputeBasicArrayCube",
                                      Enumerable.Range(1,
                                                       wantedCount)
                                                .Select(_ => ParamsHelper(numbers)),
                                      this);
    if (tasks.Count() is var count && count != wantedCount)
    {
      throw new ApplicationException($"Expected {wantedCount} submitted tasks, got {count}");
    }

    //Get the count of running tasks after 15 s
    Thread.Sleep(15000);

    var countRunningTasks = serviceAdmin.AdminMonitoringService.CountTaskBySession(sessionService.SessionId,
                                                                                   TaskStatus.Completed);

    Log.LogInformation($"Number of completed tasks after 15 seconds is {countRunningTasks}");

    //Cancel all the session
    Log.LogInformation("Cancel the whole session");
    serviceAdmin.AdminMonitoringService.CancelSession(sessionService.SessionId);

    //Get the count of running tasks after 10 s
    Thread.Sleep(10000);
    //Cancel all the session
    var countCancelTasks = serviceAdmin.AdminMonitoringService.CountTaskBySession(sessionService.SessionId,
                                                                                  TaskStatus.Cancelled,
                                                                                  TaskStatus.Cancelling);

    Log.LogInformation($"Number of canceled tasks after Session cancel is {countCancelTasks}");


    countRunningTasks = serviceAdmin.AdminMonitoringService.CountTaskBySession(sessionService.SessionId,
                                                                               TaskStatus.Completed);

    Log.LogInformation($"Number of running tasks after Session cancel is {countRunningTasks}");


    var countErrorTasks = serviceAdmin.AdminMonitoringService.CountTaskBySession(sessionService.SessionId,
                                                                                 TaskStatus.Error,
                                                                                 TaskStatus.Timeout);

    Log.LogInformation($"Number of error tasks after Session cancel is {countErrorTasks}");
  }
}
