// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
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

using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Symphony;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests;

internal class SymphonyTestHelper : UnitTestHelperBase
{
  public SymphonyTestHelper(string applicationNamespace,
                            string applicationService)
    : base(EngineType.Symphony,
           applicationNamespace,
           applicationService)
    => InitService();

  public SessionService SessionService { get; private set; }

  public void InitService()
  {
    var client = new ArmonikSymphonyClient(Configuration,
                                           LoggerFactory);

    SessionService = client.CreateSession(TaskOptions);
    Log.LogInformation($"New session created : {SessionService.SessionId}");
  }

  /// <summary>
  ///   Simple function to wait and get the result from subTasking and result delegation
  ///   to a subTask
  /// </summary>
  /// <param name="taskId">The task which is waiting for</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public byte[] WaitForTaskResult(string            taskId,
                                  CancellationToken cancellationToken = default)
  {
    var taskResult = SessionService.GetResult(taskId,
                                              cancellationToken);

    return taskResult;
  }

  /// <summary>
  ///   Wait and get the results from subTasking and result delegation
  ///   to a subTask
  /// </summary>
  /// <param name="taskIds">The tasks which we are waiting for</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public IEnumerable<Tuple<string, byte[]>> WaitForTaskResults(IEnumerable<string> taskIds,
                                                               CancellationToken   cancellationToken = default)
  {
    var taskResults = SessionService.GetResults(taskIds,
                                                cancellationToken);

    return taskResults;
  }

  /// <summary>
  ///   Wait and get the results from subTasking and result delegation
  ///   to all subTasks
  /// </summary>
  /// <param name="taskIds">The tasks which are waiting for</param>
  /// <returns></returns>
  public IEnumerable<Tuple<string, byte[]>> WaitForTasksResult(IEnumerable<string> taskIds)
  {
    var ids     = taskIds.ToList();
    var missing = ids;
    var results = new List<Tuple<string, byte[]>>();

    try
    {
      while (missing.Count != 0)
      {
        var partialResults = SessionService.TryGetResults(missing);

        var listPartialResults = partialResults.ToList();

        if (listPartialResults.Count() != 0)
        {
          results.AddRange(listPartialResults);
        }

        missing = missing.Where(x => listPartialResults.ToList()
                                                       .All(rId => rId.Item1 != x))
                         .ToList();

        if (missing.Count != 0)
        {
          Log.LogInformation($"------  Still missing {missing.Count()} result(s)  -------");
        }

        Thread.Sleep(1000);
      }
    }
    catch (ClientResultsException ex)
    {
      Log.LogError(ex.Message);
      Log.LogError("------ Adding Failed results as null in the list");
      results.AddRange(ex.TaskIds.Select(x => new Tuple<string, byte[]>(x,
                                                                        null)));
    }

    return results;
  }
}
