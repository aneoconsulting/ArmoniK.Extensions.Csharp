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
      Random rnd = new Random();

      var client = new ArmonikSymphonyClient(Configuration,
                                             LoggerFactory);
      Log.LogInformation($"------   Start 9 Session with Rand Priority with 10 tasks each with 1 Subtask    -------");
      IEnumerable<Task> payloadsTasks = Enumerable.Range(1,
                                                         9)
                                                  .Select(idx => new Task(() => ClientStartup(client,
                                                                                              idx,
                                                                                              rnd.Next(1,
                                                                                                       10))));
      var tasks = payloadsTasks.ToList();
      tasks.AsParallel().ForAll(t => t.Start());
      tasks.AsParallel().ForAll(t => t.Wait());
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
      return sessionService.GetResults(taskIds);
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="client"></param>
    public void ClientStartup(ArmonikSymphonyClient client, int numSession, int priority)
    {
      var clientPaylaod = new ClientPayload
      {
        IsRootTask  = true,
        SingleInput = 1, // 10 Subtasks
        Type        = ClientPayload.TaskType.JobOfNTasks,
      };

      Log.LogInformation($"Configure taskOptions for Session {numSession} with Priority {priority}");

      var taskOptions = InitializeTaskOptions();

      taskOptions.Priority = priority;

      var sessionService = client.CreateSession(taskOptions);


      //Logger.LogInformation($"Running 1 fat tasks End to End test with priority {priority}");
      IEnumerable<byte[]> payloads = Enumerable.Repeat(0,
                                                       10)
                                               .Select(x => clientPaylaod.Serialize());
      var taskIds = sessionService.SubmitTasks(payloads);


      Log.LogInformation($"Session {numSession} [ {sessionService} ]is waiting for output result..");

      var taskResult = WaitForTasksResult(sessionService,
                                          taskIds);

      var result = ClientPayload.Deserialize(taskResult.First().Item2);

      Log.LogInformation($"Session {numSession} with Priority {priority} has finished output result : {result.Result}");
    }
  }
}