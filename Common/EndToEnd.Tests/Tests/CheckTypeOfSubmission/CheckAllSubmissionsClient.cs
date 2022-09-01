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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckTypeOfSubmission
{
  public static class MyExtensions
  {
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
                                                       int                 maxItems)
    {
      return items.Select((item, inx) => new
                  {
                    item,
                    inx
                  })
                  .GroupBy(x => x.inx / maxItems)
                  .Select(g => g.Select(x => x.item));
    }
  }

  public class CheckAllSubmissionsClient : ClientBaseTest<CheckAllSubmissionsClient>
  {
    private enum GetResultType
    {
      GetResult,
      TryGetResult,
    }

    private enum SubmissionType
    {
      Sequential,
      Batch,
    }


    public CheckAllSubmissionsClient(IConfiguration configuration, ILoggerFactory loggerFactory) :
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

      try
      {
        SubmissionTask(sessionService,
                       10,
                       1,
                       SubmissionType.Sequential,
                       GetResultType.GetResult);
      }
      catch (Exception e)
      {
        Log.LogError(e,
                     "Submission Error 10 Jobs with 1 subtask");
      }

      try
      {
        SubmissionTask(sessionService,
                       5000,
                       0,
                       SubmissionType.Sequential,
                       GetResultType.TryGetResult);
      }
      catch (Exception e)
      {
        Log.LogError(e,
                     "Submission Error 5000 Jobs with 0 subtask");
      }

      try
      {
        SubmissionTask(sessionService,
                       5000,
                       0,
                       SubmissionType.Batch,
                       GetResultType.TryGetResult);
      }
      catch (Exception e)
      {
        Log.LogError(e,
                     "Submission Error 1 Jobs with 5000 subtasks");
      }

      //try
      //{
      //  SubmissionTask(sessionService,
      //                 1000,
      //                 0,
      //                 SubmissionType.Batch,
      //                 GetResultType.TryGetResult);
      //}
      //catch (Exception e)
      //{
      //  Log.LogError(e,
      //               "Submission Error 1000 Jobs with 0 subtask");
      //}

      //try
      //{
      //  SubmissionTask(sessionService,
      //                 1000,
      //                 1,
      //                 SubmissionType.Batch,
      //                 GetResultType.TryGetResult);
      //}
      //catch (Exception e)
      //{
      //  Log.LogError(e,
      //               "Submission Error 1000 Jobs with 1 subtask");
      //}


      try
      {
        SubmissionTask(sessionService,
                       10000,
                       0,
                       SubmissionType.Batch,
                       GetResultType.TryGetResult);
      }
      catch (Exception e)
      {
        Log.LogError(e,
                     "Submission Error 10000 Jobs with 0 subtask");
      }

      //try
      //{
      //  SubmissionTask(sessionService,
      //                 100000,
      //                 0,
      //                 SubmissionType.Batch,
      //                 GetResultType.TryGetResult);
      //}
      //catch (Exception e)
      //{
      //  Log.LogError(e,
      //               "Submission Error 10 Jobs with 1 subtask");
      //}
    }

    private void SubmissionTask(SessionService sessionService, int nbJob, int nbSubTasks, SubmissionType submissionType, GetResultType getResultType)
    {
      Log.LogInformation($"==  Running {nbJob} Tasks with {nbSubTasks} subTasks " +
                         $" {Enum.GetName(submissionType)} submit, Result method {Enum.GetName(getResultType)} =====");
      var numbers = new List<int>
      {
        1,
        2,
        3,
      };
      var clientPayloads = new ClientPayload
      {
        IsRootTask = true,
        Numbers    = numbers,
        NbSubTasks = nbSubTasks,
        Type       = ClientPayload.TaskType.SubTask,
      };

      //Prepare List of jobs
      var listOfPayload = new List<byte[]>();

      for (var i = 0; i < nbJob; i++)
      {
        listOfPayload.Add(clientPayloads.Serialize());
      }


      //Start Submission tasks
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      IEnumerable<string> taskIds;
      if (submissionType == SubmissionType.Sequential)
      {
        taskIds = listOfPayload.Select(sessionService.SubmitTask).ToArray();
      }
      else // (submissionType == SubmissionType.Batch)
      {
        taskIds = sessionService.SubmitTasks(listOfPayload).ToArray();
      }

      stopWatch.Stop();
      var ts = stopWatch.Elapsed;
      // Format and display the TimeSpan value.
      var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
      Log.LogInformation("End of submission in " + elapsedTime);


      stopWatch.Start();
      Log.LogInformation("Starting to retrieve the result : ");
      IEnumerable<Tuple<string, byte[]>> results;

      if (getResultType == GetResultType.GetResult)
      {
        results = sessionService.GetResults(taskIds);
      }
      else
      {
        results = GetTryResults(sessionService,
                                taskIds.ToList());
      }

      var tuples = results as Tuple<string, byte[]>[] ?? results.ToArray();
      stopWatch.Stop();
      ts = stopWatch.Elapsed;
      // Format and display the TimeSpan value.
      elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
      Log.LogInformation("Finished to get Results in " + elapsedTime);


      stopWatch.Start();

      Log.LogInformation($"Starting to deserialize {tuples.Count()} results : ");

      var computeResult = tuples.Select(x => ClientPayload.Deserialize(x.Item2).Result).Sum();
      var nTasks        = (nbSubTasks > 0) ? nbSubTasks : 1;

      var expectedResult = tuples.Select(_ => numbers.Sum() * nTasks).Sum();

      stopWatch.Stop();
      ts = stopWatch.Elapsed;
      // Format and display the TimeSpan value.
      elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
      Log.LogInformation($"===== Finished to execute {nbJob} nTask with {nbSubTasks} subtask " +
                         $"with result computed {computeResult} vs expected {expectedResult} in {elapsedTime}\n");
    }

    private static void PeriodicInfo(Action action, int seconds, CancellationToken token = default)
    {
      if (action == null)
      {
        return;
      }

      Task.Run(async () =>
               {
                 while (!token.IsCancellationRequested)
                 {
                   action();
                   await Task.Delay(TimeSpan.FromSeconds(seconds),
                                    token);
                 }
               },
               token);
    }


    private IEnumerable<Tuple<string, byte[]>> GetTryResults(SessionService sessionService, IList<string> taskIds)
    {
      var ids      = taskIds.ToList();
      var missing  = ids;
      var results  = new List<Tuple<string, byte[]>>();
      var cts      = new CancellationTokenSource();
      var holdPrev = 0;
      var waitInSeconds = new List<int>
      {
        10,
        1000,
        5000,
        10000,
      };
      var idx = 0;

      PeriodicInfo(() => { Log.LogInformation($"Got {results.Count} / {ids.Count} result(s) "); },
                   20,
                   cts.Token);

      while (missing.Count != 0)
      {
        var buckets = missing.Batch(10000).ToList();

        buckets.ForEach(bucket =>
        {
          var partialResults = sessionService.TryGetResults(bucket.ToList());

          var listPartialResults = partialResults.ToList();

          if (listPartialResults.Count() != 0)
          {
            results.AddRange(listPartialResults);
          }

          missing = missing.Where(x => listPartialResults.ToList().All(rId => rId.Item1 != x)).ToList();
          Thread.Sleep(waitInSeconds[0]);
        }); 
        
        if (holdPrev == results.Count)
        {
          idx = idx >= waitInSeconds.Count - 1 ? waitInSeconds.Count - 1 : idx + 1;
        }
        else
        {
          idx      = 0;
          holdPrev = results.Count;
        }

        Thread.Sleep(waitInSeconds[idx]);
      }

      cts.Cancel();

      return results;
    }
  }
}