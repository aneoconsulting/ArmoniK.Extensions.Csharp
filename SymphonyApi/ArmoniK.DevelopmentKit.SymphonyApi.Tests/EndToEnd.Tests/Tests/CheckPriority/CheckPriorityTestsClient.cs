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
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
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

      for (int idx = 1; idx <= 10; idx++)
      {
        ClientStartup(client,
                      idx,
                      idx);
      }
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to all subTasks
    /// </summary>
    /// <param name="client">The client API to connect to the Control plane Service</param>
    /// <param name="taskIds">The tasks which are waiting for</param>
    /// <returns></returns>
    private static IEnumerable<Tuple<string, byte[]>> WaitForTasksResult(ArmonikSymphonyClient client, IEnumerable<string> taskIds)
    {
      var ids = taskIds.ToList();

      client.WaitListCompletion(ids);
      var taskResult = client.GetResults(ids);
      var result = taskResult.Result.Select(x =>
      {
        var data = ClientPayload.Deserialize(x.Item2);
        if (!string.IsNullOrEmpty(data.SubTaskId))
        {
          client.WaitSubtasksCompletion(data.SubTaskId);
          return new Tuple<string, byte[]>(x.Item1,
                                           client.GetResult(data.SubTaskId));
        }

        return x;
      });

      return result;
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="client"></param>
    public Task ClientStartup(ArmonikSymphonyClient client, int numSession, int priority)
    {
      var clientPaylaod = new ClientPayload
      {
        IsRootTask = true,
        SingleInput = 10, // 10 Subtasks
        Type       = ClientPayload.TaskType.JobOfNTasks,
      };

      Log.LogInformation($"Configure taskOptions for Session {numSession} with lowPriority");

      var taskOptions = InitializeTaskOptions();

      taskOptions.Priority = priority;

      var sessionId = client.CreateSession(taskOptions);

      Log.LogInformation($"New session created : {sessionId}");

      Log.LogInformation($"Running 100 tasks End to End test with priority {priority}");

      var taskIds = client.SubmitTasks(new List<short>(100)
                                         .Select(x => clientPaylaod.Serialize()));

      var taskResult = new Task(() =>
      {
        var taskResult = WaitForTasksResult(client,
                                            taskIds);
        var result = ClientPayload.Deserialize(taskResult.First().Item2);

        Log.LogInformation($"Session {numSession} has finished output result : {result.Result}");
      });

      return taskResult;
    }
  }
}