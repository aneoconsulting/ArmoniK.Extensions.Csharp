// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
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

using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.EndToEndTests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ArmoniK.EndToEndTests.Tests.CheckSessionUniqCallback
{
  public class CheckSessionUniqCallbackClient : ClientBaseTest<CheckSessionUniqCallbackClient>
  {
    public CheckSessionUniqCallbackClient(IConfiguration configuration, ILoggerFactory loggerFactory) :
      base(configuration,
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
      _ = WaitForSubTaskResult(sessionService,
                               taskId);

      Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
      taskId = sessionService.SubmitTask(payload.Serialize());

      Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
      var taskResult = WaitForSubTaskResult(sessionService,
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
      WaitForSubTaskResult(sessionService,
                                        taskId);

      Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
      taskId = sessionService.SubmitTask(payload.Serialize());

      Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
      WaitForSubTaskResult(sessionService,
                                        taskId);

      Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
      taskId = sessionService.SubmitTask(payload.Serialize());

      Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
      WaitForSubTaskResult(sessionService,
                           taskId);

      Log.LogInformation($"\tINFO CLIENT : Submitted new task       num : {++countTask}");
      taskId = sessionService.SubmitTask(payload.Serialize());

      Log.LogInformation($"\tINFO CLIENT : Waiting taskId {taskId}  num : {countTask}");
      taskResult = WaitForSubTaskResult(sessionService,
                                        taskId);

      result = ClientPayload.Deserialize(taskResult);

      Log.LogInformation($"\tINFO CLIENT stage of call after 6 submits in 2 sessions : {PrintStates(result.Result)}");
      Log.LogInformation($"\tINFO SERVER                                             :\n\t{string.Join("\n\t", result.Message.Split('\n').Select(x => $"|\t{x}"))}");
      Assert.AreEqual(storeInitialNbCall + (1000000 + 100000 + 2 * 1000 + 6),
                      result.Result);
    }

    private string PrintStates(int resultCalls)
    {
      // service * 1000000 + session * 100000 + SessionEnter * 1000 + onInvoke * 1)


      int subResult = (resultCalls / 1000);

      var nbInvoke = resultCalls - subResult * 1000;

      // service * 1000 + session * 100 + SessionEnter * 1)
      int nbOnSessionEnter = subResult - (subResult / 100) * 100;

      int createService = (resultCalls - 1000000 - nbOnSessionEnter * 1000 - nbInvoke) / 100000;


      return $"\n\t{createService} createService(s)\n\t{nbOnSessionEnter} sessionEnter(s)\n\t{nbInvoke} nbInvoke(s)";
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private static byte[] WaitForSubTaskResult(SessionService sessionService, string taskId)
    {
      sessionService.WaitSubtasksCompletion(taskId);
      var taskResult = sessionService.GetResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      if (!string.IsNullOrEmpty(result.SubTaskId))
      {
        sessionService.WaitSubtasksCompletion(result.SubTaskId);
        taskResult = sessionService.GetResult(result.SubTaskId);
      }

      return taskResult;
    }
  }
}