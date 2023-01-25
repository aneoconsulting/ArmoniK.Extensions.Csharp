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

using System.Collections.Generic;
using System.Linq;

using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Symphony;
using ArmoniK.EndToEndTests.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.SimpleComputeNSubtasking;

[PublicAPI]
public class ServiceContainer : ServiceContainerBase
{
  public override void OnCreateService(ServiceContext serviceContext)
  {
    //END USER PLEASE FIXME
  }

  public override void OnSessionEnter(SessionContext sessionContext)
  {
    //END USER PLEASE FIXME
  }

  public byte[]? ComputeSquare(TaskContext   taskContext,
                               ClientPayload clientPayload)
  {
    Logger?.LogInformation($"Enter in function : ComputeSquare with taskId {taskContext.TaskId}");

    if (clientPayload?.Numbers?.Count == 0)
    {
      return new ClientPayload
             {
               Type   = ClientPayload.TaskType.Result,
               Result = 0,
             }.Serialize(); // Nothing to do
    }

    if (clientPayload?.Numbers?.Count == 1)
    {
      var value = clientPayload.Numbers[0] * clientPayload.Numbers[0];
      Logger?.LogInformation($"Compute {value}             with taskId {taskContext.TaskId}");

      return new ClientPayload
             {
               Type   = ClientPayload.TaskType.Result,
               Result = value,
             }.Serialize();
    }
    else // if (clientPayload.numbers.Count > 1)
    {
      var value  = clientPayload!.Numbers?[0] ?? -1;
      var square = value * value;

      var subTaskPayload = new ClientPayload();
      clientPayload?.Numbers?.RemoveAt(0);
      subTaskPayload.Numbers = clientPayload?.Numbers;
      subTaskPayload.Type    = clientPayload!.Type;
      Logger?.LogInformation($"Compute {value} in                 {taskContext.TaskId}");

      Logger?.LogInformation($"Submitting subTask from task          : {taskContext.TaskId} from Session {SessionId}");
      var subTaskId = this.SubmitTask(subTaskPayload.Serialize());
      Logger?.LogInformation($"Submitted  subTask                    : {subTaskId} with ParentTask {TaskId}");

      ClientPayload aggPayload = new()
                                 {
                                   Type   = ClientPayload.TaskType.Aggregation,
                                   Result = square,
                                 };

      Logger?.LogInformation("Submitting aggregate task             : {taskId} from Session {sessionId}",
                             taskContext.TaskId,
                             SessionId);

      var aggTaskId = this.SubmitTaskWithDependencies(aggPayload.Serialize(),
                                                      new[]
                                                      {
                                                        subTaskId,
                                                      },
                                                      true);

      Logger?.LogInformation("Submitted  SubmitTaskWithDependencies : {aggTaskId} with task dependencies      {subTaskId}",
                             aggTaskId,
                             subTaskId);

      return null;
    }
  }

  public override byte[]? OnInvoke(SessionContext sessionContext,
                                   TaskContext    taskContext)
  {
    var clientPayload = ClientPayload.Deserialize(taskContext.Payload);

    if (clientPayload.Type == ClientPayload.TaskType.ComputeSquare)
    {
      return ComputeSquare(taskContext,
                           clientPayload);
    }

    if (clientPayload.Type == ClientPayload.TaskType.Aggregation)
    {
      return AggregateValues(taskContext,
                             clientPayload);
    }

    Logger?.LogInformation($"Task type is unManaged {clientPayload.Type}");
    throw new WorkerApiException($"Task type is unManaged {clientPayload.Type}");
  }

  private byte[]? AggregateValues(TaskContext   taskContext,
                                  ClientPayload clientPayload)
  {
    Logger?.LogInformation($"Aggregate Task {taskContext.TaskId} request result from Dependencies TaskIds : [{string.Join(", ", taskContext?.DependenciesTaskIds ?? new List<string>())}]");
    var parentResult = taskContext?.DataDependencies?.Single()
                                  .Value;

    if (parentResult == null || parentResult.Length == 0)
    {
      throw new WorkerApiException($"Cannot retrieve Result from taskId {taskContext?.DependenciesTaskIds?.Single()}");
    }

    var parentResultPayload = ClientPayload.Deserialize(parentResult);
    //if (parentResultPayload.SubTaskId != null)
    //{
    //  //parentResult        = GetResult(parentResultPayload.SubTaskId);
    //  //parentResultPayload = ClientPayload.Deserialize(parentResult);
    //  throw new WorkerApiException($"There should received a new SubTask here");
    //}

    var value = clientPayload.Result + parentResultPayload.Result;

    ClientPayload childResult = new()
                                {
                                  Type   = ClientPayload.TaskType.Result,
                                  Result = value,
                                };

    return childResult.Serialize();
  }

  public override void OnSessionLeave(SessionContext sessionContext)
  {
    //END USER PLEASE FIXME
  }

  public override void OnDestroyService(ServiceContext serviceContext)
  {
    //END USER PLEASE FIXME
  }
}
