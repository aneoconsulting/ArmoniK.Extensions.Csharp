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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Armonik.Api.Grpc.V1.SortDirection;

using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.DevelopmentKit.Client.Common.Status;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.EndToEndTests.Common;
using ArmoniK.Utils;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.AggregationPriority;

/// <summary>
///   AggregationPriorityTest is a class that tests the AggregationPriority application.
/// </summary>
public class AggregationPriorityTest
{
  /// <summary>
  ///   ApplicationNamespace is the namespace of the AggregationPriority application.
  /// </summary>
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.AggregationPriority";

  /// <summary>
  ///   ApplicationService is the name of the AggregationPriority application.
  /// </summary>
  private const string ApplicationService = "AggregationPriority";

  /// <summary>
  ///   numbers_ is an array of double values.
  /// </summary>
  private readonly double[] numbers_ = Enumerable.Range(0,
                                                        10)
                                                 .Select(i => (double)i)
                                                 .ToArray();

  /// <summary>
  ///   unifiedTestHelper_ is an instance of UnifiedTestHelper class.
  /// </summary>
  private UnifiedTestHelper unifiedTestHelper_;

  /// <summary>
  ///   Setup is a method that sets up the test.
  /// </summary>
  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
                                                  ApplicationNamespace,
                                                  ApplicationService);

  /// <summary>
  ///   Cleanup is a method that cleans up after the test.
  /// </summary>
  [TearDown]
  public void Cleanup()
  {
  }

  /// <summary>
  ///   Check_That_serialazation_is_ok is a method that tests the serialization of TaskResult.
  /// </summary>
  [Test]
  public void Check_That_serialazation_is_ok()
  {
    unifiedTestHelper_.Log.LogInformation("Test TaskResult serialization");

    var byteArray = new TaskResult
                    {
                      Result = 15,
                    }.Serialize();

    var taskResult = TaskResult.Deserialize(byteArray);
    unifiedTestHelper_.Log.LogInformation($"Result after deserialization : {taskResult.Result}");

    Assert.That(taskResult.Result,
                Is.EqualTo(15));
  }

  /// <summary>
  ///   RetrieveAllTasksStats is a method that retrieves all tasks stats.
  /// </summary>
  /// <param name="channel">The channel.</param>
  /// <param name="filter">The filter.</param>
  /// <param name="sort">The sort.</param>
  /// <returns>An IAsyncEnumerable of TaskRaw.</returns>
  private async IAsyncEnumerable<TaskRaw> RetrieveAllTasksStats(ChannelBase                   channel,
                                                                ListTasksRequest.Types.Filter filter,
                                                                ListTasksRequest.Types.Sort   sort)
  {
    var               read       = 0;
    var               page       = 0;
    var               taskClient = new Tasks.TasksClient(channel);
    ListTasksResponse res;

    while ((res = await taskClient.ListTasksAsync(new ListTasksRequest
                                                  {
                                                    Filter   = filter,
                                                    Sort     = sort,
                                                    PageSize = 50,
                                                    Page     = page,
                                                  })
                                  .ConfigureAwait(false)).Total > read)
    {
      foreach (var taskSummary in res.Tasks)
      {
        var taskRaw = taskClient.GetTask(new GetTaskRequest
                                         {
                                           TaskId = taskSummary.Id,
                                         })
                                .Task;
        read++;
        yield return taskRaw;
      }

      page++;
    }
  }

  /// <summary>
  ///   Work in progress. GetDistribution is a method that gets the repartition between scalar and agg tasks.
  /// </summary>
  /// <returns>A Task of IEnumerable of TaskRaw.</returns>
  private async Task<IEnumerable<TaskRaw>> GetDistribution(int nRows)
  {
    var service = unifiedTestHelper_.Service as Service;

    var taskRawData = new List<TaskRaw>();

    await foreach (var taskRaw in RetrieveAllTasksStats(service.GetChannel(),
                                                        new ListTasksRequest.Types.Filter
                                                        {
                                                          SessionId = service.SessionId,
                                                        },
                                                        new ListTasksRequest.Types.Sort
                                                        {
                                                          Direction = SortDirection.Asc,
                                                          Field = new TaskField
                                                                  {
                                                                    TaskSummaryField = TaskSummaryField.TaskId,
                                                                  },
                                                        })
                     .ConfigureAwait(false))
    {
      taskRawData.Add(taskRaw);
    }

    var intermediateResultType = GetIntermediateResultInfo(service.SessionId,
                                                           taskRawData);

    var orderedTasks = intermediateResultType.OrderBy(t => t.Item3.CompletedAt);

    var idealGap          = nRows; // theoretical gap between agg and scalar
    var currentGap        = 0;
    var totalGapDeviation = 0;

    foreach (var task in orderedTasks)
    {
      switch (task.Item3.ResultString)
      {
        case "scalar":
          currentGap++;
          break;
        case "agg":
        {
          var gapDeviation = currentGap - idealGap;
          totalGapDeviation += gapDeviation;

          currentGap = 0;
          break;
        }
      }
    }

    unifiedTestHelper_.Log.LogInformation("Total Gap Deviation = {0}",
                                          totalGapDeviation / nRows);


    foreach (var taskData in intermediateResultType.OrderBy(t => t.Item3.CompletedAt))
    {
      unifiedTestHelper_.Log.LogInformation("TaskId : {id} completed at {completeAt} Priority : {prio} Type of task : {type}",
                                            taskData.Item1,
                                            taskData.Item3.CompletedAt,
                                            taskData.Item3.Priority,
                                            taskData.Item3.ResultString);
    }

    return taskRawData;
  }

  /// <summary>
  ///   Waits for the results of the given taskIds in the specified sessionId.
  /// </summary>
  /// <param name="sessionId">The sessionId to retrieve the results from.</param>
  /// <param name="taskIds">The taskIds to retrieve the results for.</param>
  /// <returns>A <see cref="IEnumerable{Tuple{string, byte[]}}" /> of the results.</returns>
  private IEnumerable<Tuple<string, byte[]>> WaitForResults(string              sessionId,
                                                            IEnumerable<string> taskIds)
  {
    for (var retry = 0; retry < 10; retry++)
    {
      try
      {
        var service       = unifiedTestHelper_.Service as Service;
        var ids           = taskIds as string[] ?? taskIds.ToArray();
        var missingTaskId = new List<string>(ids);
        var completeList  = new List<ResultStatusData>();

        while (completeList.Count() < ids.Count())
        {
          var bucketResultData = missingTaskId.ToChunks(200)
                                              .SelectMany(bucket =>
                                                          {
                                                            if (bucket.Distinct()
                                                                      .Count() != bucket.Length)
                                                            {
                                                              throw new Exception("Bucket has multiple times the same taskIds");
                                                            }

                                                            var taskList = bucket.ToList();
                                                            //TODO Fix issue GetResultIds return MapTaskResult can be N result for N TaskId since parentTaskIds can be requested
                                                            var mapTaskResults = taskList.ToChunks(200)
                                                                                         .SelectMany(b => service.SessionService.GetResultIds(b))
                                                                                         .Select(mp => (mp.ResultIds, mp.TaskId))
                                                                                         .ToList();
                                                            var dic = mapTaskResults.GroupBy(taskResult => taskResult.Item1.First())
                                                                                    .ToDictionary(group => group.Key,
                                                                                                  group => group.First()
                                                                                                                .Item2);

                                                            return service.SessionService.GetResultStatus(dic.Values)
                                                                          .IdsReady;
                                                          })
                                              .ToList();
          completeList.AddRange(bucketResultData);
          missingTaskId = missingTaskId.Except(bucketResultData.Select(l => l.TaskId))
                                       .ToList();
          Thread.Sleep(100);
        }

        return Retry.WhileException(10,
                                    2000,
                                    retry =>
                                    {
                                      return completeList.ToChunks(200)
                                                         .SelectMany(bucket => service.SessionService.GetResults(bucket.Select(t => t.TaskId)));
                                    },
                                    true,
                                    typeof(IOException),
                                    typeof(RpcException));
      }
      catch (Exception e)
      {
        if (retry < 9)
        {
          unifiedTestHelper_.Log.LogWarning(e,
                                            "GetResults Method threw an error");
          Thread.Sleep(2000);
        }
        else
        {
          throw new Exception("Cannot retrieve result for taskIds");
        }
      }
    }

    throw new Exception("Cannot retrieve result for taskIds");
  }

  /// <summary>
  ///   Gets the intermediate result info for the given sessionId and taskDataIds.
  /// </summary>
  /// <param name="sessionId">The sessionId for which the intermediate result info is to be retrieved.</param>
  /// <param name="taskDataIds">The taskDataIds for which the intermediate result info is to be retrieved.</param>
  /// <returns>An IEnumerable of tuples containing the sessionId, taskRaw, and taskResult.</returns>
  private IEnumerable<(string, TaskRaw, TaskResult)> GetIntermediateResultInfo(string               sessionId,
                                                                               IEnumerable<TaskRaw> taskDataIds)
  {
    var result = WaitForResults(sessionId,
                                taskDataIds.Select(t => t.Id));

    Assert.IsNotNull(result);
    var taskResults = result.Select(tp =>
                                    {
                                      var armonikPayload = ProtoSerializer.DeSerializeMessageObjectArray(tp.Item2);
                                      return (tp.Item1, taskDataIds.First(taskData => tp.Item1 == taskData.Id), TaskResult.Deserialize(armonikPayload[0] as byte[]));
                                    });


    return taskResults;
  }

  /// <summary>
  ///   This method checks that the result of a matrix computation has the expected value.
  /// </summary>
  /// <param name="squareMatrixSize">The size of the square matrix.</param>
  [TestCase(20)]
  [Ignore("Too big")]
  public void Check_That_Result_has_expected_value(int squareMatrixSize)
  {
    unifiedTestHelper_.Log.LogInformation($"Compute square matrix with n =  {squareMatrixSize}");
    unifiedTestHelper_.Log.LogInformation($"Duplicating {squareMatrixSize} Rows with vector {string.Join(", ", Enumerable.Range(0, squareMatrixSize))}");

    var taskId = unifiedTestHelper_.Service.Submit("ComputeMatrix",
                                                   UnitTestHelperBase.ParamsHelper(squareMatrixSize),
                                                   unifiedTestHelper_);

    var result = WaitForResults(unifiedTestHelper_.Service.SessionId,
                                new List<string>
                                {
                                  taskId,
                                })
      .Single();

    Assert.IsNotNull(result);

    var deprot     = ProtoSerializer.DeSerializeMessageObjectArray(result.Item2);
    var taskResult = TaskResult.Deserialize(deprot[0] as byte[]);
    unifiedTestHelper_.Log.LogInformation($"Result of Matrix formula : {taskResult.Result}");

    var sum = Enumerable.Range(0,
                               squareMatrixSize)
                        .Aggregate(0.0,
                                   (current,
                                    scalar) => current + scalar * scalar);

    Assert.That(sum * squareMatrixSize,
                Is.EqualTo(taskResult.Result));

    var _ = GetDistribution(squareMatrixSize)
      .Result;
  }
}
