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

using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.GridServer;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SessionService = ArmoniK.DevelopmentKit.Client.Symphony.SessionService;

namespace ArmoniK.EndToEndTests.Tests.CheckRandomException;

public class RandomExceptionClient : ClientBaseTest<RandomExceptionClient>, IServiceInvocationHandler
{
  public RandomExceptionClient(IConfiguration configuration,
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
    => Log.LogError($"Expected Error from {taskId} : " + e.Message);

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
        // Log.LogInformation("Task finished but nothing returned in Result");
        break;
      case double value:
        //Log.LogInformation($"Task finished with result {value}");
        break;
      case double[] doubles:
        //Log.LogInformation("Result is " +
        //                   string.Join(", ",
        //                               doubles));
        break;
      case byte[] values:
        //Log.LogInformation("Result is " +
        //                   string.Join(", ",
        //                               values.ConvertToArray()));
        break;
    }
  }


  [EntryPoint]
  public override void EntryPoint()
  {
    Log.LogInformation("Configure taskOptions");
    var taskOptions = InitializeTaskOptions();
    OverrideTaskOptions(taskOptions);

    taskOptions.ApplicationService = "SimpleServiceContainer";

    //var props = new Properties(Configuration,
    //                           taskOptions);
    var props = new Properties(taskOptions,
                               Configuration.GetSection("Grpc")["EndPoint"],
                               5001);

    //var resourceId = ServiceAdmin.CreateInstance(Configuration, LoggerFactory,props).UploadResource("filePath");


    using var cs = ServiceFactory.GetInstance()
                                 .CreateService(taskOptions.ApplicationName,
                                                props);


    Log.LogInformation($"New session created : {cs.SessionId}");

    Log.LogInformation("Running End to End test to compute Square value with SubTasking");
    ClientStartup1(cs);
  }

  private static void OverrideTaskOptions(TaskOptions taskOptions)
    => taskOptions.EngineType = EngineType.DataSynapse.ToString();

  /// <summary>
  ///   Simple function to wait and get the result from subTasking and result delegation
  ///   to a subTask
  /// </summary>
  /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
  /// <param name="taskId">The task which is waiting for</param>
  /// <returns></returns>
  private static byte[] WaitForTaskResult(SessionService sessionService,
                                          string         taskId)
  {
    var taskResult = sessionService.GetResult(taskId);

    return taskResult;
  }

  private static object[] ParamsHelper(params object[] elements)
    => elements;

  /// <summary>
  ///   The first test developed to validate dependencies subTasking
  /// </summary>
  /// <param name="sessionService"></param>
  private void ClientStartup1(Service sessionService)
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

    for (var taskId = 0; taskId < 10; taskId++)
    {
      sessionService.Submit("ComputeBasicArrayCube",
                            ParamsHelper(numbers,
                                         0.3),
                            this);
    }
  }
}
