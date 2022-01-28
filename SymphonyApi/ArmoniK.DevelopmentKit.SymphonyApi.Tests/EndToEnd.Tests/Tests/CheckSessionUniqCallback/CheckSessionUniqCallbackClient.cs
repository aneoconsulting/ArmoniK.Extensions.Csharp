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

using System;
using System.Collections.Generic;

using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

      Log.LogInformation("Configure taskOptions");
      var taskOptions = InitializeTaskOptions();

      var sessionId = client.CreateSession(taskOptions);

      Log.LogInformation($"New session created : {sessionId}");

      Log.LogInformation("Running End to End test to compute Square value with SubTasking");

      var payload = new ClientPayload
      {
        IsRootTask = true,
        Type       = ClientPayload.TaskType.None,
      };
      var taskId = client.SubmitTask(payload.Serialize());

      var taskResult = WaitForSubTaskResult(client,
                                            taskId);

      taskId = client.SubmitTask(payload.Serialize());

      taskResult = WaitForSubTaskResult(client,
                                            taskId);

      var result = ClientPayload.Deserialize(taskResult);

      Log.LogInformation($"output result : {result.Result}");
      var storeInitialNbCall = result.Result - 1000000 - 100000 - 1000 - 2;

      sessionId = client.CreateSession(taskOptions);

      Log.LogInformation($"New session created : {sessionId}");

      Log.LogInformation("Running End to End test to compute Square value with SubTasking");

      
      taskId = client.SubmitTask(payload.Serialize());

      taskResult = WaitForSubTaskResult(client,
                                            taskId);

      taskId = client.SubmitTask(payload.Serialize());

      taskResult = WaitForSubTaskResult(client,
                                        taskId);
      taskId = client.SubmitTask(payload.Serialize());

      taskResult = WaitForSubTaskResult(client,
                                        taskId);
      taskId = client.SubmitTask(payload.Serialize());

      taskResult = WaitForSubTaskResult(client,
                                        taskId);

      result = ClientPayload.Deserialize(taskResult);

      Log.LogInformation($"output result : {result.Result}");
      Assert.AreEqual(storeInitialNbCall + (1000000 + 100000 + 2 * 1000 + 6),
                      result.Result);
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="client">The client API to connect to the Control plane Service</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private static byte[] WaitForSubTaskResult(ArmonikSymphonyClient client, string taskId)
    {
      client.WaitSubtasksCompletion(taskId);
      var taskResult = client.GetResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      if (!string.IsNullOrEmpty(result.SubTaskId))
      {
        client.WaitSubtasksCompletion(result.SubTaskId);
        taskResult = client.GetResult(result.SubTaskId);
      }

      return taskResult;
    }
    
  }
}