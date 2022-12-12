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

using ArmoniK.DevelopmentKit.Client.Symphony;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckSessionUniqCallback;

[Disabled]
public class CheckSessionUniqCallbackClient : ClientBaseTest<CheckSessionUniqCallbackClient>
{
  public CheckSessionUniqCallbackClient(IConfiguration configuration,
                                        ILoggerFactory loggerFactory)
    : base(configuration,
           loggerFactory)
  {
  }

  [EntryPoint]
  public override void EntryPoint()
  {
    var client = new ArmonikSymphonyClient(Configuration,
                                           LoggerFactory);

    var countTask    = 0;
    var countSession = 0;

    var taskOptions = InitializeTaskOptions();


    var sessionService = client.CreateSession(taskOptions);
    Log.LogInformation($"INFO CLIENT : New session created {sessionService} num : {++countSession}");

    var payload = new ClientPayload
                  {
                    IsRootTask = true,
                    Type       = ClientPayload.TaskType.None,
                  };
    Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
    var taskId = sessionService.SubmitTask(payload.Serialize());

    Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
    _ = WaitForTaskResult(sessionService,
                          taskId);

    Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
    taskId = sessionService.SubmitTask(payload.Serialize());

    Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
    var taskResult = WaitForTaskResult(sessionService,
                                       taskId);

    var result = ClientPayload.Deserialize(taskResult);

    Log.LogInformation($"\tINFO CLIENT stage of call after 2 submits in 1 session : {PrintStates(result.Result)}");
    Log.LogInformation($"\tINFO SERVER                                            :\n\t{string.Join("\n\t", result.Message.Split('\n').Select(x => $"|\t{x}"))}");

    var storeInitialNbCall = result.Result - 1000000 - 100000 - 1000 - 2;

    sessionService = client.CreateSession(taskOptions);

    Log.LogInformation($"INFO CLIENT : New session created : {sessionService}");


    Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
    taskId = sessionService.SubmitTask(payload.Serialize());

    Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
    WaitForTaskResult(sessionService,
                      taskId);

    Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
    taskId = sessionService.SubmitTask(payload.Serialize());

    Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
    WaitForTaskResult(sessionService,
                      taskId);

    Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
    taskId = sessionService.SubmitTask(payload.Serialize());

    Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
    WaitForTaskResult(sessionService,
                      taskId);

    Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
    taskId = sessionService.SubmitTask(payload.Serialize());

    Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
    taskResult = WaitForTaskResult(sessionService,
                                   taskId);

    result = ClientPayload.Deserialize(taskResult);

    Log.LogInformation($"\tINFO CLIENT stage of call after 6 submits in 2 sessions : {PrintStates(result.Result)}");
    Log.LogInformation($"\tINFO SERVER                                             :\n\t{string.Join("\n\t", result.Message.Split('\n').Select(x => $"|\t{x}"))}");
    Assert.AreEqual(storeInitialNbCall + 1000000 + 100000 + 2 * 1000 + 6,
                    result.Result);
  }

  private string PrintStates(int resultCalls)
  {
    // service * 1000000 + session * 100000 + SessionEnter * 1000 + onInvoke * 1)


    var subResult = resultCalls / 1000;

    var nbInvoke = resultCalls - subResult * 1000;

    // service * 1000 + session * 100 + SessionEnter * 1)
    var nbOnSessionEnter = subResult - subResult / 100 * 100;

    var createService = (resultCalls - 1000000 - nbOnSessionEnter * 1000 - nbInvoke) / 100000;


    return $"\n\t{createService} createService(s)\n\t{nbOnSessionEnter} sessionEnter(s)\n\t{nbInvoke} nbInvoke(s)";
  }

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
}

