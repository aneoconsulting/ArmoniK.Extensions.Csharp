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

using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common;

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


    private static void PeriodicInfo(Action action, int seconds, CancellationToken token = default)
    {
      if (action == null)
        return;
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

    private static IEnumerable<Tuple<ResultIds, byte[]>> GetTryResults(SessionService sessionService, IEnumerable<TaskResultId> taskIds)
    {
      var ids      = taskIds.Select(id => new ResultIds(id)).ToList();
      var missing  = ids;
      var results  = new List<Tuple<ResultIds, byte[]>>();
      var cts      = new CancellationTokenSource();
      var holdPrev = 0;
      var waitInSeconds = new List<int>
      {
        1000,
        5000,
        10000,
        20000,
        30000
      };
      var idx = 0;

      PeriodicInfo(() => { Log.LogInformation($"Got {results.Count} / {ids.Count} result(s) "); },
                   20,
                   cts.Token);

      while (missing.Count != 0)
      {
        missing.Batch(100).ToList().ForEach(bucket =>
        {
          var partialResults = sessionService.TryGetResults(bucket);

          var listPartialResults = partialResults.ToList();

          if (listPartialResults.Count() != 0)
          {
            results.AddRange(listPartialResults);
          }

          missing = missing.Where(x => listPartialResults.ToList().All(rId => rId.Item1 != x)).ToList();


          if (holdPrev == results.Count)
          {
            idx = idx >= waitInSeconds.Count - 1 ? waitInSeconds.Count - 1 : idx + 1;
          }
          else
          {
            idx = 0;
          }

          holdPrev = results.Count;

          Thread.Sleep(waitInSeconds[idx]);
        });
      }

      cts.Cancel();

      return results;
    }


    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="sessionService">The sessionService to submit and wait for result</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private byte[] WaitForSubTaskResult(SessionService sessionService, ResultIds taskId)
    {
      Log.LogInformation($"Wait for root task to finish [task {taskId}]");

      var taskResult = sessionService.GetResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      //if (!string.IsNullOrEmpty(result.SubTaskId))
      //{
      //  Logger.LogInformation($"Root task wait for subtask delegation [SubTask with dependencies {result.SubTaskId}]");
      //  Logger.LogInformation($"Wait for Sub task to finish [task {result.SubTaskId}]");
      //  sessionService.WaitForTaskCompletion(result.SubTaskId);
      //  taskResult = sessionService.GetResult(result.SubTaskId);
      //}

      return taskResult;
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="sessionService"></param>
    private void ClientStartup1(SessionService sessionService)
    {
      var numbers = new List<int>
      {
        1,
        2,
        3,
      };
      var clientPayload = new ClientPayload
      {
        IsRootTask = true,
        Numbers    = numbers,
        Type       = ClientPayload.TaskType.ComputeSquare
      };
      var taskId = sessionService.SubmitTask(clientPayload.Serialize());

      var taskResult = WaitForSubTaskResult(sessionService,
                                            taskId);
      var result = ClientPayload.Deserialize(taskResult);

      Log.LogInformation($"output result : {result.Result}");
    }


    /// <summary>
    ///   Simple function to wait and get the Result from subTasking and Result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private static byte[] WaitForTaskResult(SessionService sessionService, ResultIds taskId)
    {
      var taskResult = sessionService.GetResult(taskId);

      return taskResult;
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="sessionService"></param>
    /// <param name="nbRun">The number of execution to produce a average time spent</param>
    /// <param name="nbElements">The number of element in the vector to compute</param>
    private static void ExecuteVectorSubtasking(SessionService sessionService, int nbRun = 1, int nbElements = 3)
    {
      Log.LogInformation("Running End to End test to compute Square value with SubTasking");

      var numbers = Enumerable.Range(1,
                                     nbElements).ToList();

      var timeSpans = new List<TimeSpan>();

      Enumerable.Range(1,
                       nbRun).ToList().ForEach(nRun =>
      {
        //Start Submission tasks
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var payload = new ClientPayload
        {
          IsRootTask = true,
          Numbers    = numbers,
          Type       = ClientPayload.TaskType.ComputeSquare,
        };
        var taskId = sessionService.SubmitTask(payload.Serialize());

        var taskResult = WaitForTaskResult(sessionService,
                                           taskId);
        var result = ClientPayload.Deserialize(taskResult);

        stopWatch.Stop();

        Log.LogInformation($"Run: {nRun} output Result : {result.Result}");
        var ts = stopWatch.Elapsed;
        timeSpans.Add(ts);
      });
      var tsm = timeSpans.Average();
      // Format and display the TimeSpan value.
      var elapsedTime = $"{tsm.Hours:00}:{tsm.Minutes:00}:{tsm.Seconds:00}.{tsm.Milliseconds / 10:00}";
      Log.LogInformation($"Time elapsed average for {nbRun} Runs " + elapsedTime);
    }
  }


  public static class TimeSpanExt
  {
    /// <summary>
    /// Calculates the average of the given timeSpans.
    /// </summary>
    public static TimeSpan Average(this IEnumerable<TimeSpan> timeSpans)
    {
      IEnumerable<long> ticksPerTimeSpan = timeSpans.Select(t => t.Ticks);
      var               averageTicks     = ticksPerTimeSpan.Average();
      var               averageTicksLong = Convert.ToInt64(averageTicks);

      var averageTimeSpan = TimeSpan.FromTicks(averageTicksLong);

      return averageTimeSpan;
    }
  }
}