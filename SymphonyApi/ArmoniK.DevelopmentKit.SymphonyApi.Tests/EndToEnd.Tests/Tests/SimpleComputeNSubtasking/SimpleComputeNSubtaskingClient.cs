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

using System.Collections.Generic;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.SimpleComputeNSubtasking
{
  public class SimpleComputeNSubtaskingClient : ClientBaseTest<SimpleComputeNSubtaskingClient>
  {
    public SimpleComputeNSubtaskingClient(IConfiguration configuration, ILoggerFactory loggerFactory) :
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

      var sessionService = client.CreateSession(taskOptions);

      Log.LogInformation($"New session created : {sessionService}");
      
      Log.LogInformation("Running End to End test to compute Square value with SubTasking");
      ClientStartup1(sessionService);
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="sessionService">The sessionService to submit and wait for result</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private byte[] WaitForSubTaskResult(SessionService sessionService, string taskId)
    {
      Log.LogInformation($"Wait for root task to finish [task {taskId}]");
      sessionService.WaitForTaskCompletion(taskId);
      var taskResult = sessionService.GetResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      if (!string.IsNullOrEmpty(result.SubTaskId))
      {
        Log.LogInformation($"Root task wait for subtask delegation [SubTask with dependencies {result.SubTaskId}]");
        Log.LogInformation($"Wait for Sub task to finish [task {result.SubTaskId}]");
        sessionService.WaitForTaskCompletion(result.SubTaskId);
        taskResult = sessionService.GetResult(result.SubTaskId);
      }

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
      var clientPaylaod = new ClientPayload
      {
        IsRootTask = true,
        Numbers    = numbers,
        Type       = ClientPayload.TaskType.ComputeSquare
      };
      var taskId = sessionService.SubmitTask(clientPaylaod.Serialize());

      var taskResult = WaitForSubTaskResult(sessionService,
                                            taskId);
      var result = ClientPayload.Deserialize(taskResult);

      Log.LogInformation($"output result : {result.Result}");
    }
  }
}