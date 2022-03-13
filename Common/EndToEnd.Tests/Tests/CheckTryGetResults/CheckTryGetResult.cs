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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.SymphonyApi;
using ArmoniK.DevelopmentKit.SymphonyApi.api;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckTryGetResults
{
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

    private string Job_of_N_Tasks(byte[] payload, int nbTasks)
    {
      Logger.LogInformation($"Executing {nbTasks} Subtasks with ExpM1 compute");

      var payloads = new List<byte[]>(nbTasks);
      for (var i = 0; i < nbTasks; i++)
        payloads.Add(payload);

      var sw      = Stopwatch.StartNew();
      var taskIds = SubmitTasks(payloads);
      var newPayload = new ClientPayload()
      {
        Type = ClientPayload.TaskType.Aggregation,
      };

      var aggTaskId = this.SubmitTaskWithDependencies(newPayload.Serialize(),
                                                      taskIds.ToList());

      var elapsedMilliseconds = sw.ElapsedMilliseconds;
      Logger.LogInformation($"Server called {nbTasks} tasks in {elapsedMilliseconds} ms");

      return aggTaskId;
    }

    private static double ExpM1(double x)
    {
      return ((((((((((((((15.0 + x) * x + 210.0) * x + 2730.0) * x + 32760.0) * x + 360360.0) * x + 3603600.0) * x + 32432400.0) * x + 259459200.0) * x +
                   1816214400.0) *
                  x +
                  10897286400.0) *
                 x +
                 54486432000.0) *
                x +
                217945728000.0) *
               x +
               653837184000.0) *
              x +
              1307674368000.0) *
             x *
             7.6471637318198164759011319857881e-13;
    }

    public override byte[] OnInvoke(SessionContext sessionContext, TaskContext taskContext)
    {
      var clientPayload = ClientPayload.Deserialize(taskContext.TaskInput);


      switch (clientPayload.Type)
      {
        case ClientPayload.TaskType.Sleep:
          Logger.LogInformation($"Empty task, sessionId : {sessionContext.SessionId}, taskId : {taskContext.TaskId}, sessionId from task : {taskContext.SessionId}");
          Thread.Sleep(clientPayload.Sleep * 1000);
          break;
        case ClientPayload.TaskType.Expm1:
        {
          Logger.LogInformation($"ExpM1 task, sessionId : {sessionContext.SessionId}, taskId : {taskContext.TaskId}, sessionId from task : {taskContext.SessionId}");
          var result = 0.0;

          for (var idx = 2; idx > 0; idx--)
          {
            result += ExpM1(idx);
          }

          return new ClientPayload()
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

          var aggTaskId = Job_of_N_Tasks(bytePayload,
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
}
