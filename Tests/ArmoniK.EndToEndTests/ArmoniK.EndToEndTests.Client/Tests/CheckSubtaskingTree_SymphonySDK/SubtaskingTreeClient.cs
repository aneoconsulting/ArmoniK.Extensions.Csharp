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

using System.Diagnostics;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Client.Symphony;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckSubtaskingTree_SymphonySDK;

public class SubtaskingTreeClient : ClientBaseTest<SubtaskingTreeClient>
{
  public SubtaskingTreeClient(IConfiguration  configuration,
                              ILoggerFactory? loggerFactory)
    : base(configuration,
           loggerFactory)
  {
  }

  [EntryPoint]
  public override void EntryPoint()
  {
    var client = new ArmonikSymphonyClient(Configuration,
                                           LoggerFactory);

    Log?.LogInformation("Configure taskOptions");
    var taskOptions = InitializeTaskOptions();

    var sessionService = client.CreateSession(taskOptions);

    Log?.LogInformation($"New session created : {sessionService}");

    Log?.LogInformation("Running End to End test to compute Sum of numbers in a subtasking tree way");
    ExecuteTreeSubtasking(sessionService,
                          256);
  }

  /// <summary>
  ///   Simple function to wait and get the result from subTasking and result delegation
  ///   to a subTask
  /// </summary>
  /// <param name="sessionService">The sessionService API to connect to the Control plane Service</param>
  /// <param name="taskId">The task which is waiting for</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  private static byte[]? WaitForTaskResult(SessionService    sessionService,
                                           string            taskId,
                                           CancellationToken cancellationToken = default)
  {
    var taskResult = sessionService.GetResult(taskId,
                                              cancellationToken);
    return taskResult;
  }

  private static void ExecuteTreeSubtasking(SessionService sessionService,
                                            int            maxNumberToSum    = 10,
                                            int            subtaskSplitCount = 2)
  {
    var numbers = Enumerable.Range(1,
                                   maxNumberToSum)
                            .ToList();
    var payload = new ClientPayload
                  {
                    IsRootTask = true,
                    Numbers    = numbers,
                    NbSubTasks = subtaskSplitCount,
                    Type       = ClientPayload.TaskType.None,
                  };

    var sw = Stopwatch.StartNew();

    var taskId = sessionService.SubmitTask(payload.Serialize());

    var taskResult = WaitForTaskResult(sessionService,
                                       taskId);
    var result = ClientPayload.Deserialize(taskResult);
    sw.Stop();
    var expectedResult = numbers.Sum(elem => (long)elem);
    Log?.LogInformation($"SplitAndSum {numbers.First()} ... {numbers.Last()}: Result is {result.Result} expected : {expectedResult} => {(result.Result == expectedResult ? "OK" : "NOT OK")} in {sw.ElapsedMilliseconds / 1000} sec.");
  }
}
