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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ArmoniK.Api.Common.Utils;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Symphony;
using ArmoniK.EndToEndTests.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckRandomException;

[PublicAPI]
public class ServiceContainer : ServiceContainerBase
{
  private readonly Random rd = new();

  public override void OnCreateService(ServiceContext serviceContext)
  {
    //END USER PLEASE FIXME
  }

  public override void OnSessionEnter(SessionContext sessionContext)
  {
    //END USER PLEASE FIXME
  }

  private string Job_of_N_Tasks(byte[] payload,
                                int    nbTasks)
  {
    Logger.LogInformation($"Executing {nbTasks} Subtasks with ExpM1 compute");

    var payloads = new List<byte[]>(nbTasks);
    for (var i = 0; i < nbTasks; i++)
    {
      payloads.Add(payload);
    }

    var sw      = Stopwatch.StartNew();
    var taskIds = SubmitTasks(payloads);
    var newPayload = new ClientPayload
                     {
                       Type = ClientPayload.TaskType.Aggregation,
                     };

    var aggTaskId = this.SubmitTaskWithDependencies(newPayload.Serialize(),
                                                    taskIds.ToList());

    var elapsedMilliseconds = sw.ElapsedMilliseconds;
    Logger.LogInformation($"Server called {nbTasks} tasks in {elapsedMilliseconds} ms");

    return aggTaskId;
  }

  private double ExpM1(double x)
  {
    var percentageOfFailure = 5.0;

    var randNum = rd.NextDouble();
    if (randNum < percentageOfFailure / 100)
    {
      throw new MyCustomWorkerException("An expected failure in this random call");
    }

    return ((((((((((((((15.0 + x) * x + 210.0) * x + 2730.0) * x + 32760.0) * x + 360360.0) * x + 3603600.0) * x + 32432400.0) * x + 259459200.0) * x + 1816214400.0) *
                x + 10897286400.0) * x + 54486432000.0) * x + 217945728000.0) * x + 653837184000.0) * x + 1307674368000.0) * x * 7.6471637318198164759011319857881e-13;
  }

  public override byte[] OnInvoke(SessionContext sessionContext,
                                  TaskContext    taskContext)
  {
    var clientPayload = ClientPayload.Deserialize(taskContext.TaskInput);
    using var _ = Logger.BeginPropertyScope(("SessionId", sessionContext.SessionId),
                                            ("TaskId", taskContext.TaskId),
                                            ("ClientPayload", clientPayload.Type));

    Logger.LogInformation("{ClientPayload} task, sessionId : {SessionId}, taskId : {TaskId}",
                          clientPayload.Type,
                          sessionContext.SessionId,
                          taskContext.TaskId);

    switch (clientPayload.Type)
    {
      case ClientPayload.TaskType.Sleep:
        Thread.Sleep(clientPayload.Sleep * 1000);
        break;
      case ClientPayload.TaskType.Expm1:
      {
        var result = 0.0;

        for (var idx = 2; idx > 0; idx--)
        {
          result += ExpM1(idx);
        }

        return new ClientPayload
               {
                 Type   = ClientPayload.TaskType.Result,
                 Result = (int)result,
               }.Serialize();
      }
      case ClientPayload.TaskType.Aggregation:
        Logger.LogInformation($"!!!! All subtask Finished sessionId : {sessionContext.SessionId}\n\n");
        break;
      case ClientPayload.TaskType.JobOfNTasks:
      {
        var newPayload = new ClientPayload
                         {
                           Type = ClientPayload.TaskType.Expm1,
                         };

        var bytePayload = newPayload.Serialize();

        Job_of_N_Tasks(bytePayload,
                       clientPayload.SingleInput);

        return null;
      }
      default:
        Logger.LogInformation($"Task type is unManaged {clientPayload.Type}");
        throw new WorkerApiException($"Task type is unManaged {clientPayload.Type}");
    }

    return null; //nothing to do
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

public class MyCustomWorkerException : Exception
{
  public MyCustomWorkerException(string message)
    : base(message)
  {
  }
}

