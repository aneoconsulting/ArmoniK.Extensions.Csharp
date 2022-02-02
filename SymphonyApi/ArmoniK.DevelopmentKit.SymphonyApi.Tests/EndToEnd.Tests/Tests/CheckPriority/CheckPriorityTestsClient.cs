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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckPriority
{
  [Disabled]
  public class CheckPriorityTestsClient : ClientBaseTest<CheckPriorityTestsClient>
  {
    public CheckPriorityTestsClient(IConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration,
                                                                                                       loggerFactory)
    {
    }

    [EntryPoint]
    public override void EntryPoint()
    {
      var client = new ArmonikSymphonyClient(Configuration,
                                             LoggerFactory);
      IEnumerable<Task> payloads = Enumerable.Range(1,
                                                    9)
                                             .Select(idx => ClientStartup(client,
                                                                          idx,
                                                                          idx));
      Task.WaitAll(payloads.ToArray());
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to all subTasks
    /// </summary>
    /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
    /// <param name="sessionId"></param>
    /// <param name="taskIds">The tasks which are waiting for</param>
    /// <returns></returns>
    private static IEnumerable<Tuple<string, byte[]>> WaitForTasksResult(SessionService sessionService, IEnumerable<string> taskIds)
    {
      IEnumerable<string> ids = taskIds.ToList();
      var results = new List<Tuple<string, byte[]>>()
      {
        Capacity = ids.Count(),
      };

      foreach (var id in ids)
      {
        sessionService.WaitCompletion(id);
        var taskResult = sessionService.GetResult(id);
        var data       = ClientPayload.Deserialize(taskResult);
        if (string.IsNullOrEmpty(data.SubTaskId))
          continue;
        sessionService.WaitSubtasksCompletion(data.SubTaskId);
        results.Add(new Tuple<string, byte[]>(id,
                                              sessionService.GetResult(data.SubTaskId)));
      }

      return results;
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="client"></param>
    public Task ClientStartup(ArmonikSymphonyClient client, int numSession, int priority)
    {
      var clientPaylaod = new ClientPayload
      {
        IsRootTask  = true,
        SingleInput = 1, // 10 Subtasks
        Type        = ClientPayload.TaskType.JobOfNTasks,
      };

      Log.LogInformation($"Configure taskOptions for Session {numSession} with lowPriority");

      var taskOptions = InitializeTaskOptions();

      taskOptions.Priority = priority;

      var sessionService = client.CreateSession(taskOptions);

      Log.LogInformation($"New session created : {sessionService}");

      Log.LogInformation($"Running 100 tasks End to End test with priority {priority}");
      IEnumerable<byte[]> payloads = Enumerable.Repeat(0,
                                                       10)
                                               .Select(x => clientPaylaod.Serialize());
      var taskIds = sessionService.SubmitTasks(payloads);

      var taskResult = Task.Run(() =>
      {
        Log.LogInformation($"Session {numSession} is waiting for output result..");

        var taskResult = WaitForTasksResult(sessionService, taskIds);

        var result     = ClientPayload.Deserialize(taskResult.First().Item2);

        Log.LogInformation($"Session {numSession} has finished output result : {result.Result}");
      });

      return taskResult;
    }
  }
}