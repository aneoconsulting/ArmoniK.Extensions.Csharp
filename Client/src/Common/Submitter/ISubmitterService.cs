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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Common Interface for Submitter services.
/// </summary>
[PublicAPI]
public interface ISubmitterService : IDisposable
{
  /// <summary>
  ///   The Id of the current session
  /// </summary>
  string SessionId { get; }

  /// <summary>
  ///   The method submit will execute task asynchronously on the server and will serialize object[] for Service method
  ///   MethodName(Object[] arguments)
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of object that can be passed in parameters of the function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Return the taskId string</returns>
  string Submit(string                    methodName,
                object[]                  arguments,
                IServiceInvocationHandler handler,
                int                       maxRetries  = 5,
                TaskOptions?              taskOptions = null);

  /// <summary>
  ///   The method submits a list of task with a list of arguments for each task which will be serialized into a byte[] for
  ///   each call
  ///   MethodName(byte[] argument)
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of parameters that can be passed in parameters of the each call of function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Return the list of created taskIds</returns>
  IEnumerable<string> Submit(string                    methodName,
                             IEnumerable<object[]>     arguments,
                             IServiceInvocationHandler handler,
                             int                       maxRetries  = 5,
                             TaskOptions?              taskOptions = null);


  /// <summary>
  ///   The method submits a list of task with a list of arguments for each task which will be serialized into a byte[] for
  ///   each call
  ///   MethodName(byte[] argument)
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">List of serialized arguments that will already serialize for MethodName.</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Return the list of created taskIds</returns>
  IEnumerable<string> Submit(string                    methodName,
                             IEnumerable<byte[]>       arguments,
                             IServiceInvocationHandler handler,
                             int                       maxRetries  = 5,
                             TaskOptions?              taskOptions = null);

  /// <summary>
  ///   The method submit with one serialized argument that will send as byte[] MethodName(byte[]   argument).
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="argument">One serialized argument that will already serialize for MethodName.</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="token">The cancellation token to set to cancel the async task</param>
  /// <returns>Return the taskId string</returns>
  public Task<string> SubmitAsync(string                    methodName,
                                  object[]                  argument,
                                  IServiceInvocationHandler handler,
                                  int                       maxRetries  = 5,
                                  TaskOptions?              taskOptions = null,
                                  CancellationToken         token       = default);

  /// <summary>
  ///   The method submit with one serialized argument that will send as byte[] MethodName(byte[]   argument).
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="argument">One serialized argument that will already serialize for MethodName.</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="token">The cancellation token to set to cancel the async task</param>
  /// <returns>Return the taskId string</returns>
  public Task<string> SubmitAsync(string                    methodName,
                                  byte[]                    argument,
                                  IServiceInvocationHandler handler,
                                  int                       maxRetries  = 5,
                                  TaskOptions?              taskOptions = null,
                                  CancellationToken         token       = default);
}

/// <summary>
///   Provide extension methods for ISubmitterService
/// </summary>
[PublicAPI]
public static class SubmitterServiceExt
{
  /// <summary>
  ///   The method submit will execute task asynchronously on the server
  /// </summary>
  /// <param name="service">the ISubmitterService extended</param>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">A list of objects that can be passed in parameters of the function</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Return the taskId string</returns>
  public static string Submit(this ISubmitterService    service,
                              string                    methodName,
                              object[]                  arguments,
                              IServiceInvocationHandler handler,
                              int                       maxRetries  = 5,
                              TaskOptions?              taskOptions = null)
    => service.Submit(methodName,
                      new List<object[]>
                      {
                        arguments,
                      },
                      handler,
                      maxRetries,
                      taskOptions)
              .Single();

  /// <summary>
  ///   The method submit will execute task asynchronously on the server
  /// </summary>
  /// <param name="service">the ISubmitterService extended</param>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">List of serialized arguments that will already serialize for MethodName.</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Return the taskId string</returns>
  public static string Submit(this ISubmitterService    service,
                              string                    methodName,
                              byte[]                    arguments,
                              IServiceInvocationHandler handler,
                              int                       maxRetries  = 5,
                              TaskOptions?              taskOptions = null)
    => service.Submit(methodName,
                      new List<byte[]>
                      {
                        arguments,
                      },
                      handler,
                      maxRetries,
                      taskOptions)
              .Single();
}
