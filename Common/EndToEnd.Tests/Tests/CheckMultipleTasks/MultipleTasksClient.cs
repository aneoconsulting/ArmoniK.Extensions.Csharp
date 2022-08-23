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
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckMultipleTasks
{
  [Disabled]
  public class CheckMultipleTasksClient : ClientBaseTest<CheckMultipleTasksClient>
  {
    public CheckMultipleTasksClient(IConfiguration configuration, ILoggerFactory loggerFactory) :
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
    /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static byte[] WaitForTaskResult(SessionService sessionService, ResultIds taskId, CancellationToken cancellationToken = default)
    {
      var taskResult = sessionService.GetResult(taskId,
                                                cancellationToken);

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
        Type       = ClientPayload.TaskType.None,
      };


      var listOfNbTasks = new List<int>
      {
        100,
      };
      long       sumTimeElapsed = 0;
      const long maxDuration    = 10 * 60 * 1000; // 10 min

      Log.LogInformation($"Running tests with {string.Join("; ", listOfNbTasks)} nbTasks in {maxDuration / 1000} secs");
      var cancellationToken     = new CancellationTokenSource();
      var waitCancellationToken = new CancellationTokenSource();
      var token                 = waitCancellationToken.Token;
      var testRun = Task.Run(() =>
                             {
                               foreach (var nbTasks in listOfNbTasks)
                               {
                                 sumTimeElapsed += Job_of_N_Tasks(sessionService,
                                                                  clientPayload.Serialize(),
                                                                  nbTasks,
                                                                  token);
                               }
                             },
                             cancellationToken.Token);
      var finished = testRun.Wait(TimeSpan.FromMilliseconds(maxDuration));

      if (!finished)
      {
        waitCancellationToken.Cancel();


        try
        {
          testRun.Wait(TimeSpan.FromMilliseconds(1000 * 10));
          cancellationToken.Cancel();
        }
        catch (Exception)
        {
          // ignored
        }
        finally
        {
          Log.LogWarning($"TimeElapsed more than {maxDuration / (60 * 1000)} min. Stop tests after running Jobs");
          cancellationToken.Dispose();
        }
      }
    }

    /// <summary>
    ///   The function to execute 1 job with several tasks inside
    /// </summary>
    /// <param name="sessionService">The sessionService to connect to the Control plane Service</param>
    /// <param name="payload">A default payload to execute by each task</param>
    /// <param name="nbTasks">The Number of jobs</param>
    /// <param name="cancellationToken"></param>
    private long Job_of_N_Tasks(SessionService    sessionService,
                                byte[]            payload,
                                int               nbTasks,
                                CancellationToken cancellationToken)
    {
      var payloads = new List<byte[]>(nbTasks);
      for (var i = 0; i < nbTasks; i++)
        payloads.Add(payload);

      var sw          = Stopwatch.StartNew();
      var finalResult = 0;
      var taskIds     = sessionService.SubmitTasks(payloads);
      foreach (var taskId in taskIds)
      {
        Log.LogInformation($"Client is calling {nbTasks} tasks...");
        var taskResult = WaitForTaskResult(sessionService,
                                           taskId,
                                           cancellationToken);
        var result = ClientPayload.Deserialize(taskResult);

        finalResult += result.Result;
      }

      Assert.AreEqual(nbTasks * 8,
                      finalResult);


      var elapsedMilliseconds = sw.ElapsedMilliseconds;
      Log.LogInformation($"Client called {nbTasks} tasks in {elapsedMilliseconds} ms aggregated Result = {finalResult}");

      return elapsedMilliseconds;
    }
  }
}