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

//TODO : remove pragma

using ArmoniK.DevelopmentKit.Client.Exceptions;
using ArmoniK.DevelopmentKit.Common;

namespace ArmoniK.DevelopmentKit.Client.Services
{
  /// <summary>
  /// The interface from which the handler must inherit to be considered as a handler of service
  /// in the method LocalExecute, Execute or Submit
  /// </summary>
  [MarkDownDoc]
  public interface IServiceInvocationHandler
  {
    /// <summary>
    /// The callBack method which has to be implemented to retrieve error or exception
    /// </summary>
    /// <param name="e">The exception sent to the client from the control plane</param>
    /// <param name="taskId">The task identifier which has invoke the error callBack</param>
    void HandleError(ServiceInvocationException e, string taskId);

    /// <summary>
    /// The callBack method which has to be implemented to retrieve response from the server
    /// </summary>
    /// <param name="response">The object receive from the server as result the method called by the client</param>
    /// <param name="taskId">The task identifier which has invoke the response callBack</param>
    void HandleResponse(object response, string taskId);
  }
}