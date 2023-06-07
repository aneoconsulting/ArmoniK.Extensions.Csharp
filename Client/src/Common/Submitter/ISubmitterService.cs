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

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Common Interface for Submitter services.
/// </summary>
public interface ISubmitterService
{
  /// <summary>
  ///   The Id of the current session
  /// </summary>
  string SessionId { get; }

  /// <summary>
  ///   The method submit will execute task asynchronously on the server
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of objects that can be passed in parameters of the function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <returns>Return the taskId string</returns>
  string Submit(string                    methodName,
                object[]                  arguments,
                IServiceInvocationHandler handler);

  /// <summary>
  ///   The method submits a list of task with a list of arguments for each task which will be serialized into a byte[] for
  ///   each call
  ///   MethodName(byte[] argument)
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of parameters that can be passed in parameters of the each call of function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <returns>Return the list of created taskIds</returns>
  IEnumerable<string> Submit(string                    methodName,
                             IEnumerable<object[]>     arguments,
                             IServiceInvocationHandler handler);
}
