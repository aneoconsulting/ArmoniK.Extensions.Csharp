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

using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.EndToEndTests.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckTryGetResults
{
  [UsedImplicitly]
  public class CheckTryGetResultsClient : ClientBaseTest<CheckTryGetResultsClient>
  {
    public CheckTryGetResultsClient(IConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration,
                                                                                                       loggerFactory)
    {
    }

    [EntryPoint]
    public override void EntryPoint()
    {
      var rnd = new Random();

      var client = new ArmonikSymphonyClient(Configuration,
                                             LoggerFactory);
      Log.LogInformation("------   Start 9 Session with Rand Priority with 10 tasks each with 1 Subtask    -------");
      var payloadsTasks = Enumerable.Range(1,
                                           2)
                                    .Select(idx => new Task(() => ClientStartup(client,
                                                                                idx)));
      var tasks = payloadsTasks.ToList();
      tasks.AsParallel().ForAll(t => t.Start());
      tasks.AsParallel().ForAll(t => t.Wait());
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to all subTasks
    /// </summary>
    /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
    /// <param name="taskIds">The tasks which are waiting for</param>
    /// <returns></returns>
    /// 
    private static IEnumerable<Tuple<string, byte[]>> WaitForTasksResult(SessionService sessionService, IEnumerable<string> taskIds)
    {
      var ids     = taskIds.ToList();
      var missing = ids;
      var results = new List<Tuple<string, byte[]>>();

      while (missing.Count != 0)
      {
        var partialResults = sessionService.TryGetResults(ids);

        var listPartialResults = partialResults.ToList();

        if (listPartialResults.Count() != 0)
          results.AddRange(listPartialResults);

        missing = ids.Where(x => listPartialResults.ToList().All(rId => rId.Item1 != x)).ToList();
      }

      return results;
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="client"></param>
    /// <param name="numSession"></param>
    /// <param name="priority"></param>
    private void ClientStartup(ArmonikSymphonyClient client, int numSession)
    {
      var clientPayload = new ClientPayload
      {
        Type        = ClientPayload.TaskType.Expm1,
      };

      Log.LogInformation($"Configure taskOptions for Session {numSession}");

      var taskOptions = InitializeTaskOptions();

      taskOptions.Priority = 1;

      var sessionService = client.CreateSession(taskOptions);


      var payloads = Enumerable.Repeat(0,
                                       10)
                               .Select(_ => clientPayload.Serialize());
      var taskIds = sessionService.SubmitTasks(payloads);


      Log.LogInformation($"Session {numSession} [ {sessionService} ]is waiting for output result..");

      var taskResults = WaitForTasksResult(sessionService,
                                          taskIds);

      var result = taskResults.Select(x => ClientPayload.Deserialize(x.Item2).Result).Sum();

      Log.LogInformation($"Session {numSession} has finished output result : {result}");
    }
  }
}