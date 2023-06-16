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

using ArmoniK.DevelopmentKit.Worker.Symphony;
using ArmoniK.EndToEndTests.Common;

namespace ArmoniK.EndToEndTests.Tests.CheckOptions;

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


  public override byte[] OnInvoke(SessionContext sessionContext,
                                  TaskContext    taskContext)
  {
    _ = ClientPayload.Deserialize(taskContext.TaskInput);
    ConfigureSession(SessionId,
                     taskContext.TaskOptions);

    /////////////////// TO SERVER SIDE TEST HERE //////////////////////////////////////////

    return new ClientPayload
           {
             Type   = ClientPayload.TaskType.Result,
             Result = 42,
           }.Serialize(); //nothing to do
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
