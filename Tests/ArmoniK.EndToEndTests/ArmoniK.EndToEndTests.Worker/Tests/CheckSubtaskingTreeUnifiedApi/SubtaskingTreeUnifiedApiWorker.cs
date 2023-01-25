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

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Unified;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckSubtaskingTreeUnifiedApi;

public class SubtaskingTreeUnifiedApiWorker : TaskSubmitterWorkerService
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

  public byte[]? ComputeSubTaskingTreeSum(byte[]? clientPayload)
  {
    Logger?.LogInformation("Enter in function : SplitAndSum taskID : {TaskId}",
                          TaskContext?.TaskId);

    var payload = ClientPayload.Deserialize(clientPayload);
    CheckPayload(payload);

    if (payload?.Numbers?.Count == 0)
    {
      return new ClientPayload
             {
               Type   = ClientPayload.TaskType.Result,
               Result = 0,
             }.Serialize(); // Nothing to do
    }

    if (payload?.Numbers?.Count == 1)
    {
      var value = payload.Numbers[0];
      Logger?.LogInformation("final tree {value}",
                            value);

      return new ClientPayload
             {
               Type   = ClientPayload.TaskType.Result,
               Result = value,
             }.Serialize();
    }

    // if (clientPayload.numbers.Count > 1)
    var splittedLists = SplitList(payload?.Numbers!,
                                  payload!.NbSubTasks!);

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
        Logger?.LogInformation("Submitting subTask, numbers : [{Numbers}] payload type : {PayloadType}",
                              string.Join(';',
                                          splittedList),
                              payload.Type);
      }
      else
      {
        Logger?.LogInformation("Submitting subTask, numbers : [{NumbersFrom};...;{NumbersTo}]",
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

    Logger?.LogInformation("Submitting aggregation task");
    var aggTaskId = SubmitTaskWithDependencies(nameof(AggregateValues),
                                               ParamsHelper(aggPayload.Serialize()),
                                               subTaskIds,
                                               true);

    Logger?.LogInformation("Submitted  SubmitTaskWithDependencies : {aggTaskId} with task dependencies {subtaskIds}...",
                          aggTaskId,
                          string.Join(';',
                                      subTaskIds.Take(10)));

    return null; //nothing to do
  }

  private static object?[] ParamsHelper(params object?[] elements)
    => elements;


  public byte[] AggregateValues(byte[]? serializedClientPayload)
  {
    var clientPayload = ClientPayload.Deserialize(serializedClientPayload);

    Logger?.LogInformation("Aggregate Task. Request result from Dependencies TaskIds : {DependenciesTaskIds}",
                          string.Join(", ",
                                      TaskContext?.DependenciesTaskIds?.Take(10) ?? new List<string>()));
    var aggregatedValuesSum = 0;
    var dependencyValues    = new List<int>();
    if (TaskContext?.DataDependencies != null)
    {
      foreach (var taskDependency in TaskContext?.DataDependencies!)
      {
        if (taskDependency.Value == null || taskDependency.Value.Length == 0)
        {
          throw new WorkerApiException($"Cannot retrieve result from taskId {TaskContext?.DependenciesTaskIds?.Single()}");
        }

        var deprot                  = ProtoSerializer.DeSerializeMessageObjectArray(taskDependency.Value);
        var dependencyResultPayload = ClientPayload.Deserialize(deprot?[0] as byte[]);
        dependencyValues.Add(dependencyResultPayload.Result);
        aggregatedValuesSum += dependencyResultPayload.Result;
      }
    }

    Logger?.LogInformation("Aggregation has summed parents data dependencies {dependencyValues}, result = {aggregatedValuesSum}",
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
