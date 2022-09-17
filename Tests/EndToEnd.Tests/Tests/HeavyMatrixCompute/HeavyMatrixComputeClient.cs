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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Exceptions;
using ArmoniK.DevelopmentKit.Client.Factory;
using ArmoniK.DevelopmentKit.Client.Services;
using ArmoniK.DevelopmentKit.Client.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.HeavyMatrixCompute;

public class HeavyMatrixComputeClient : ClientBaseTest<HeavyMatrixComputeClient>, IServiceInvocationHandler
{
  private int nbTask_;

  public HeavyMatrixComputeClient(IConfiguration configuration,
                                  ILoggerFactory loggerFactory)
    : base(configuration,
           loggerFactory)
  {
  }


  /// <summary>
  ///   The callBack method which has to be implemented to retrieve error or exception
  /// </summary>
  /// <param name="e">The exception sent to the client from the control plane</param>
  /// <param name="taskId">The task identifier which has invoke the error callBack</param>
  public void HandleError(ServiceInvocationException e,
                          string                     taskId)
  {
    Log.LogError($"Error from {taskId} : " + e.Message);
    throw new ApplicationException($"Error from {taskId}",
                                   e);
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
        Log.LogInformation("Task finished but nothing returned in Result");
        break;
      case double value:
        nbTask_++;
        //Log.LogInformation($"Task {nbTask_++} finished with result {value}");
        break;
      case double[] doubles:
        //Log.LogInformation("Result is " + string.Join(", ",
        //                                              doubles));
        break;
      case byte[] values:
        Log.LogInformation("Result is " + string.Join(", ",
                                                      CheckUnifiedApi.SelectExtensions.ConvertToArray(values)));
        break;
    }
  }


  [EntryPoint]
  public override void EntryPoint()
  {
    Log.LogInformation("Configure taskOptions");
    var taskOptions = InitializeTaskOptions();
    OverrideTaskOptions(taskOptions);

    taskOptions.ApplicationService = "HeavyMatrixCompute";

    var props = new Properties(taskOptions,
                               Configuration.GetSection("Grpc")["EndPoint"],
                               5001);


    //using var cs = ServiceFactory.CreateService(props, LoggerFactory);
    using var cs = ServiceFactory.CreateService(props);

    Log.LogInformation($"New session created : {cs.SessionId}");

    Log.LogInformation("Running End to End test to compute heavy matrix in sequential");
    ComputeMatrix(cs);
  }

  private static void OverrideTaskOptions(TaskOptions taskOptions)
    => taskOptions.EngineType = EngineType.Unified.ToString();


  private static object[] ParamsHelper(params object[] elements)
    => elements;

  private static void PeriodicInfo(Action            action,
                                   int               seconds,
                                   CancellationToken token = default)
  {
    if (action == null)
    {
      return;
    }

    Task.Run(async () =>
             {
               while (!token.IsCancellationRequested)
               {
                 action();
                 await Task.Delay(TimeSpan.FromSeconds(seconds),
                                  token);
               }
             },
             token);
  }

  /// <summary>
  ///   The first test developed to validate dependencies subTasking
  /// </summary>
  /// <param name="sessionService"></param>
  private void ComputeMatrix(Service sessionService)
  {
    var index_task = 0;
    var prev_index = 0;
    var nb_doubles_elt = 76800; // 600 Ko of payload
    var nb_tasks = 20000;

    var cts      = new CancellationTokenSource();
    var numbers = Enumerable.Range(0,
                                   nb_doubles_elt)
                            .Select(x => (double)x)
                            .ToArray();
    Log.LogInformation($"Running from {nb_tasks} tasks with payload by task {nb_doubles_elt * 8 / 1024} Ko...");
    
    PeriodicInfo(() =>
                   {
                     Log.LogInformation($"Got {nbTask_} results. Check Submission perf : Payload {2 * (index_task - prev_index) * nb_doubles_elt * 8.0 / 1024.0 / 20.0} Ko/s, {2 * (index_task - prev_index) / 20} tasks/s");
                     prev_index = index_task;
                   },
                   20,
                   cts.Token);

    
    var sw    = Stopwatch.StartNew();

    for (index_task = 0; index_task < nb_tasks / 2; index_task++)
    {
      Log.LogInformation($"{index_task}/{nb_tasks} Task Time avg to submit {index_task * 2.0 / (sw.ElapsedMilliseconds / 1000.0):0.00} Task/s");
      sessionService.Submit("ComputeBasicArrayCube",
                            ParamsHelper(numbers),
                            this);

      sessionService.Submit("ComputeReduceCube",
                            ParamsHelper(numbers),
                            this);
    }

    Log.LogInformation($"{nb_tasks} tasks executed in : {sw.ElapsedMilliseconds / 1000} secs");
    cts.Cancel();

    Thread.Sleep(20000); // let some time to get last results. Need to  implement a wait in ResultHandler ??
  }
}
