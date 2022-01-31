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
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.SymphonyApi;
using ArmoniK.DevelopmentKit.SymphonyApi.api;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckSessionUniqCallback
{
  public class ServiceContainer : ServiceContainerBase
  {
    private readonly IConfiguration configuration_;
    public           int            countCall;
    public static    string         resultMessage;

    public ServiceContainer()
    {
      countCall     = 1000000;
      resultMessage = $"new ServiceContainer Instance : {this.GetHashCode()}\n";
    }

    private string PrintStates(int resultCalls)
    {
      // service * 1000000 + session * 100000 + SessionEnter * 1000 + onInvoke * 1)


      int subResult = (resultCalls / 1000);

      var nbInvoke = resultCalls - subResult * 1000;

      // service * 1000 + session * 100 + SessionEnter * 1)
      int nbOnSessionEnter = subResult - (subResult / 100) * 100;

      int createService = (resultCalls - 1000000 - nbOnSessionEnter * 1000 - nbInvoke) / 100000;


      return $"Call State :\n\t{createService} createService(s)\n\t{nbOnSessionEnter} sessionEnter(s)\n\t{nbInvoke} nbInvoke(s)";

    }

    public override void OnCreateService(ServiceContext serviceContext)
    {
      //END USER PLEASE FIXME
      countCall += 100000;
      Log.LogInformation($"Call OnCreateService on service [InstanceID : {this.GetHashCode()}]");
      resultMessage = $"{resultMessage}\nCall OnCreateService on service [InstanceID : {this.GetHashCode()}]";
    }

    public override void OnSessionEnter(SessionContext sessionContext)
    {
      //END USER PLEASE FIXME
      countCall += 1000;
      Log.LogInformation($"Call OnSessionEnter on service [InstanceID : {this.GetHashCode()}]");
      resultMessage = $"{resultMessage}\nCall OnSessionEnter on service [InstanceID : {this.GetHashCode()}]";
      
    }


    public override byte[] OnInvoke(SessionContext sessionContext, TaskContext taskContext)
    {
      countCall += 1;
      Log.LogInformation($"Call OnInvoke on service [InstanceID : {this.GetHashCode()}]");
      resultMessage = $"{resultMessage}\nCall OnInvoke on service [InstanceID : {this.GetHashCode()}]";
      var clientPayload = ClientPayload.Deserialize(taskContext.TaskInput);
      resultMessage = $"{resultMessage}\n{PrintStates(countCall)}";

      return new ClientPayload
        {
          Type   = ClientPayload.TaskType.Result,
          Result = countCall,
          Message = resultMessage,
        }
        .Serialize(); //nothing to do
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