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

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Unified;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckSubtaskingTreeUnifiedApi;

public class SubtaskingTreeUnifiedApiWorker : TaskWorkerService
{
  public void CheckPayload(ClientPayload payload)
  {
    if (payload == null)
    {
      throw new ArgumentException("CheckPayload: payload must not be null");
    }

    if (payload.Type != ClientPayload.TaskType.Aggregation)
    {
      if (payload.Numbers == null)
      {
        throw new ArgumentException("CheckPayload: payload numbers must not be null");
      }

      if (payload.NbSubTasks <= 1)
      {
        throw new ArgumentException($"CheckPayload: payload.NbSubTasks:{payload.NbSubTasks} must be >= 2");
      }
    }
  }

  private static IEnumerable<T[]> SplitList<T>(List<T> listToSplit,
                                               int     nbSplit = 2)
    => listToSplit.Chunk((int)Math.Ceiling(listToSplit.Count / (float)nbSplit));

  public byte[] ComputeSubTaskingTreeSum(byte[] clientPayload)
  {
    Logger.LogInformation("Enter in function : SplitAndSum taskID : {TaskId}",
                          TaskContext.TaskId);

    var payload = ClientPayload.Deserialize(clientPayload);
    CheckPayload(payload);

    if (payload.Numbers.Count == 0)
    {
      return new ClientPayload
             {
               Type   = ClientPayload.TaskType.Result,
               Result = 0,
             }.Serialize(); // Nothing to do
    }

    if (payload.Numbers.Count == 1)
    {
      var value = payload.Numbers[0];
      Logger.LogInformation("final tree {value}",
                            value);

      return new ClientPayload
             {
               Type   = ClientPayload.TaskType.Result,
               Result = value,
             }.Serialize();
    }

    // if (clientPayload.numbers.Count > 1)
    var splittedLists = SplitList(payload.Numbers,
                                  payload.NbSubTasks);

    var subTaskIds = new List<string>(payload.NbSubTasks);
    foreach (var splittedList in splittedLists)
    {
      var childPayload = new ClientPayload
                         {
                           Type       = payload.Type,
                           Numbers    = splittedList.ToList(),
                           NbSubTasks = payload.NbSubTasks,
                           Result     = 0,
                         };
      if (splittedList.Count() <= 100)
      {
        Logger.LogInformation("Submitting subTask, numbers : [{Numbers}] payload type : {PayloadType}",
                              string.Join(';',
                                          splittedList),
                              payload.Type);
      }
      else
      {
        Logger.LogInformation("Submitting subTask, numbers : [{NumbersFrom};...;{NumbersTo}]",
                              string.Join(';',
                                          splittedList.Take(50)),
                              string.Join(';',
                                          splittedList.TakeLast(50)));
      }

      var subTaskId = SubmitTask("ComputeSubTaskingTreeSum",
                                 ParamsHelper(childPayload.Serialize()));
      subTaskIds.Add(subTaskId);
    }

    ClientPayload aggPayload = new()
                               {
                                 Type   = ClientPayload.TaskType.Aggregation,
                                 Result = 0,
                               };

    Logger.LogInformation("Submitting aggregation task");
    var aggTaskId = SubmitTaskWithDependencies(nameof(AggregateValues),
                                               ParamsHelper(aggPayload.Serialize()),
                                               subTaskIds,
                                               true);

    Logger.LogInformation("Submitted  SubmitTaskWithDependencies : {aggTaskId} with task dependencies {subtaskIds}...",
                          aggTaskId,
                          string.Join(';',
                                      subTaskIds.Take(10)));

    return null; //nothing to do
  }

  private static object[] ParamsHelper(params object[] elements)
    => elements;


  public byte[] AggregateValues(byte[] serializedClientPayload)
  {
    var clientPayload = ClientPayload.Deserialize(serializedClientPayload);

    Logger.LogInformation("Aggregate Task. Request result from Dependencies TaskIds : {DependenciesTaskIds}",
                          string.Join(", ",
                                      TaskContext.DependenciesTaskIds.Take(10)));
    var aggregatedValuesSum = 0;
    var dependencyValues    = new List<int>();
    foreach (var taskDependency in TaskContext.DataDependencies)
    {
      if (taskDependency.Value == null || taskDependency.Value.Length == 0)
      {
        throw new WorkerApiException($"Cannot retrieve result from taskId {TaskContext.DependenciesTaskIds?.Single()}");
      }

      var deprot                  = ProtoSerializer.Deserialize<object[]>(taskDependency.Value);
      var dependencyResultPayload = ClientPayload.Deserialize(deprot[0] as byte[]);
      dependencyValues.Add(dependencyResultPayload.Result);
      aggregatedValuesSum += dependencyResultPayload.Result;
    }

    Logger.LogInformation("Aggregation has summed parents data dependencies {dependencyValues}, result = {aggregatedValuesSum}",
                          string.Join("+",
                                      dependencyValues),
                          aggregatedValuesSum);
    aggregatedValuesSum += clientPayload.Result;

    ClientPayload aggregationResults = new()
                                       {
                                         Type   = ClientPayload.TaskType.Result,
                                         Result = aggregatedValuesSum,
                                       };

    return aggregationResults.Serialize();
  }
}
