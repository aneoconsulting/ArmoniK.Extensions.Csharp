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

using System.Collections.Generic;
using System.Linq;

using ArmoniK.DevelopmentKit.Worker.Symphony;
using ArmoniK.EndToEndTests.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckTypeOfSubmission;

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

  private byte[] AggregateValues(TaskContext taskContext)
  {
    Logger.LogInformation($"Aggregate Task from Dependencies TaskIds : [{string.Join(", ", taskContext.DependenciesTaskIds)}]");

    var sum = taskContext.DataDependencies?.Select(x => ClientPayload.Deserialize(x.Value)
                                                                     .Result)
                         .Sum() ?? 0;


    ClientPayload childResult = new()
                                {
                                  Type   = ClientPayload.TaskType.Result,
                                  Result = sum,
                                };

    return childResult.Serialize();
  }

  public override byte[] OnInvoke(SessionContext sessionContext,
                                  TaskContext    taskContext)
  {
    var payload = ClientPayload.Deserialize(taskContext.TaskInput);

    switch (payload.Type)
    {
      case ClientPayload.TaskType.SubTask when payload.NbSubTasks > 0:
      {
        var subPayload = new ClientPayload
                         {
                           Type    = ClientPayload.TaskType.None,
                           Numbers = payload.Numbers,
                         }.Serialize();

        var listPayload = new List<byte[]>();

        for (var i = 0; i < payload.NbSubTasks; i++)
        {
          listPayload.Add(subPayload);
        }

        var taskIds = SubmitTasks(listPayload);

        var aggPayload = new ClientPayload
                         {
                           Type = ClientPayload.TaskType.Aggregation,
                         };

        this.SubmitTaskWithDependencies(aggPayload.Serialize(),
                                        taskIds.ToList(),
                                        true);

        return null; //Delegate to subTasks
      }
      case ClientPayload.TaskType.SubTask:
        return new ClientPayload
               {
                 Type   = ClientPayload.TaskType.Result,
                 Result = payload.Numbers.Sum(),
               }.Serialize(); //nothing to do
      case ClientPayload.TaskType.Aggregation:
        return AggregateValues(taskContext);

      default:
        return new ClientPayload
               {
                 Type   = ClientPayload.TaskType.Result,
                 Result = payload.Numbers.Sum(),
               }.Serialize(); //nothing to do
    }
    /////////////////// TO SERVER SIDE TEST HERE //////////////////////////////////////////
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
