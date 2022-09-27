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

namespace ArmoniK.EndToEndTests.Tests.CheckSessionUniqCallback;

public sealed class ServiceContainer : ServiceContainerBase
{
  private static string _resultMessage;
  private        int    countCall_;

  public ServiceContainer()
  {
    _resultMessage ??= "";

    countCall_     = 1000000;
    _resultMessage = $"new ServiceContainer Instance : {GetHashCode()}\n";
  }

  private static string PrintStates(int resultCalls)
  {
    // service * 1000000 + session * 100000 + SessionEnter * 1000 + onInvoke * 1)


    var subResult = resultCalls / 1000;

    var nbInvoke = resultCalls - subResult * 1000;

    // service * 1000 + session * 100 + SessionEnter * 1)
    var nbOnSessionEnter = subResult - subResult / 100 * 100;

    var createService = (resultCalls - 1000000 - nbOnSessionEnter * 1000 - nbInvoke) / 100000;


    return $"Call State :\n\t{createService} createService(s)\n\t{nbOnSessionEnter} sessionEnter(s)\n\t{nbInvoke} nbInvoke(s)";
  }

  public override void OnCreateService(ServiceContext serviceContext)
  {
    //END USER PLEASE FIXME
    countCall_ += 100000;
    Logger.LogInformation($"Call OnCreateService on service [InstanceID : {GetHashCode()}]");
    _resultMessage = $"{_resultMessage}\nCall OnCreateService on service [InstanceID : {GetHashCode()}]";
  }

  public override void OnSessionEnter(SessionContext sessionContext)
  {
    //END USER PLEASE FIXME
    countCall_ += 1000;
    Logger.LogInformation($"Call OnSessionEnter on service [InstanceID : {GetHashCode()}]");
    _resultMessage = $"{_resultMessage}\nCall OnSessionEnter on service [InstanceID : {GetHashCode()}]";
  }


  public override byte[] OnInvoke(SessionContext sessionContext,
                                  TaskContext    taskContext)
  {
    countCall_ += 1;
    Logger.LogInformation($"Call OnInvoke on service [InstanceID : {GetHashCode()}]");
    _resultMessage = $"{_resultMessage}\nCall OnInvoke on service [InstanceID : {GetHashCode()}]";
    _resultMessage = $"{_resultMessage}\n{PrintStates(countCall_)}";

    return new ClientPayload
           {
             Type    = ClientPayload.TaskType.Result,
             Result  = countCall_,
             Message = _resultMessage,
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
