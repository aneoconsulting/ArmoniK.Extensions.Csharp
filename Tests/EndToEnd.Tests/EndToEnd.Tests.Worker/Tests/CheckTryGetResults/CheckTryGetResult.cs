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

using ArmoniK.DevelopmentKit.Worker.Symphony;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckTryGetResults;

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

  private static double ExpM1(double x)
    => ((((((((((((((15.0 + x) * x + 210.0) * x + 2730.0) * x + 32760.0) * x + 360360.0) * x + 3603600.0) * x + 32432400.0) * x + 259459200.0) * x + 1816214400.0) * x +
            10897286400.0) * x + 54486432000.0) * x + 217945728000.0) * x + 653837184000.0) * x + 1307674368000.0) * x * 7.6471637318198164759011319857881e-13;

  public override byte[] OnInvoke(SessionContext sessionContext,
                                  TaskContext    taskContext)
  {
    var clientPayload = ClientPayload.Deserialize(taskContext.TaskInput);

    switch (clientPayload.Type)
    {
      case ClientPayload.TaskType.Expm1:
      {
        Logger.LogInformation($"ExpM1 task, sessionId : {sessionContext.SessionId}, taskId : {taskContext.TaskId}, sessionId from task : {taskContext.SessionId}");
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
      default:
        Logger.LogInformation($"Task type is unManaged {clientPayload.Type}");
        throw new WorkerApiException($"Task type is unManaged {clientPayload.Type}");
    }
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
