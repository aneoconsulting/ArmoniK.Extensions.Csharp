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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Client.Symphony;
using ArmoniK.EndToEndTests.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckTryGetResults;

[UsedImplicitly]
public class CheckTryGetResultsClient : ClientBaseTest<CheckTryGetResultsClient>
{
  public CheckTryGetResultsClient(IConfiguration configuration,
                                  ILoggerFactory loggerFactory)
    : base(configuration,
           loggerFactory)
  {
  }

  [EntryPoint]
  public override void EntryPoint()
  {
    var rnd = new Random();

    var client = new ArmonikSymphonyClient(Configuration,
                                           LoggerFactory);
    Log.LogInformation("------   Start 2 Sessions  with 100 tasks  -------");
    var payloadsTasks = Enumerable.Range(1,
                                         2)
                                  .Select(idx => new Task(() => ClientStartup(client,
                                                                              idx)));
    var tasks = payloadsTasks.ToList();
    tasks.AsParallel()
         .ForAll(t => t.Start());
    tasks.AsParallel()
         .ForAll(t => t.Wait());
  }

  /// <summary>
  ///   Simple function to wait and get the result from subTasking and result delegation
  ///   to all subTasks
  /// </summary>
  /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
  /// <param name="taskIds">The tasks which are waiting for</param>
  /// <returns></returns>
  private IEnumerable<Tuple<string, byte[]>> WaitForTasksResult(SessionService      sessionService,
                                                                IEnumerable<string> taskIds)
  {
    var ids     = taskIds.ToList();
    var missing = ids;
    var results = new List<Tuple<string, byte[]>>();

    while (missing.Count != 0)
    {
      var partialResults = sessionService.TryGetResults(missing);

      var listPartialResults = partialResults.ToList();

      if (listPartialResults.Count() != 0)
      {
        results.AddRange(listPartialResults);
        Log.LogInformation($"------  Session {sessionService.SessionId.Id}  Get {listPartialResults.Count()} result(s)  -------");
      }

      missing = missing.Where(x => listPartialResults.ToList()
                                                     .All(rId => rId.Item1 != x))
                       .ToList();

      if (missing.Count != 0)
      {
        Log.LogInformation($"------  Session {sessionService.SessionId.Id} Still missing {missing.Count()} result(s)  -------");
      }

      Thread.Sleep(1000);
    }

    return results;
  }

  /// <summary>
  ///   The first test developed to validate dependencies subTasking
  /// </summary>
  /// <param name="client"></param>
  /// <param name="numSession"></param>
  private void ClientStartup(ArmonikSymphonyClient client,
                             int                   numSession)
  {
    var clientPayload = new ClientPayload
                        {
                          Type = ClientPayload.TaskType.Expm1,
                        };

    Log.LogInformation($"Configure taskOptions for Session {numSession}");

    var taskOptions = InitializeTaskOptions();

    taskOptions.Priority = 1;

    var sessionService = client.CreateSession(taskOptions);


    var payloads = Enumerable.Repeat(0,
                                     100)
                             .Select(_ => clientPayload.Serialize());
    var taskIds = sessionService.SubmitTasks(payloads);


    Log.LogInformation($"Session {numSession} [ {sessionService} ]is waiting for output result..");

    var taskResults = WaitForTasksResult(sessionService,
                                         taskIds);

    var result = taskResults.Select(x => ClientPayload.Deserialize(x.Item2)
                                                      .Result)
                            .Sum();

    Log.LogInformation($"Session {numSession} has finished output result : {result}");
  }
}
