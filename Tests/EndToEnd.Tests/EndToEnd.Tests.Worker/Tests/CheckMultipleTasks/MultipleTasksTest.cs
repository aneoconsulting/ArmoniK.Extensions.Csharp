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

using ArmoniK.DevelopmentKit.Worker.Symphony;
using ArmoniK.EndToEndTests.Common;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckMultipleTasks;

public class CheckMultipleTasksWorker : ServiceContainerBase
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
    var payload = ClientPayload.Deserialize(taskContext.TaskInput);

    return new ClientPayload
           {
             Type   = ClientPayload.TaskType.Result,
             Result = 8,
           }.Serialize(); //nothing to do
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
