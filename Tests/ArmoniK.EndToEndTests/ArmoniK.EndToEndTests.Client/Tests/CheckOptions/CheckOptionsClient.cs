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

using ArmoniK.DevelopmentKit.Client.Symphony;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckOptions;

public class CheckOptionsClient : ClientBaseTest<CheckOptionsClient>
{
  public CheckOptionsClient(IConfiguration  configuration,
                            ILoggerFactory? loggerFactory)
    : base(configuration,
           loggerFactory)
  {
  }

  [EntryPoint]
  public override void EntryPoint()
  {
    var client = new ArmonikSymphonyClient(Configuration,
                                           LoggerFactory);

    Log?.LogInformation("Configure taskOptions");
    var taskOptions = InitializeTaskOptions();
    taskOptions.Options["key1"] = "value1";
    taskOptions.Options["key2"] = "value2";
    taskOptions.Options["key3"] = "value3";

    var sessionService = client.CreateSession(taskOptions);

    Log?.LogInformation($"New session created : {sessionService}");

    Log?.LogInformation("Running End to End test to compute Square value with SubTasking");
    ClientStartup1(sessionService);
  }

  /// <summary>
  ///   Simple function to wait and get the result from subTasking and result delegation
  ///   to a subTask
  /// </summary>
  /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
  /// <param name="taskId">The task which is waiting for</param>
  /// <returns></returns>
  private static byte[]? WaitForTaskResult(SessionService sessionService,
                                           string         taskId)
  {
    var taskResult = sessionService.GetResult(taskId);

    return taskResult;
  }

  /// <summary>
  ///   The first test developed to validate dependencies subTasking
  /// </summary>
  /// <param name="sessionService"></param>
  public void ClientStartup1(SessionService sessionService)
  {
    var numbers = new List<int>
                  {
                    1,
                    2,
                    3,
                  };
    var clientPayload = new ClientPayload
                        {
                          IsRootTask = true,
                          Numbers    = numbers,
                          Type       = ClientPayload.TaskType.ComputeSquare,
                        };
    var serialized = clientPayload.Serialize();
    var payloads   = new List<byte[]>();
    for (var i = 0; i < 100; i++)
    {
      payloads.Add(serialized);
    }

    var taskId = sessionService.SubmitTasks(payloads);

    foreach (var t in taskId)
    {
      var taskResult = WaitForTaskResult(sessionService,
                                         t);
      var result = ClientPayload.Deserialize(taskResult);

      Log?.LogInformation($"output result : {result.Result}");
    }
  }
}
