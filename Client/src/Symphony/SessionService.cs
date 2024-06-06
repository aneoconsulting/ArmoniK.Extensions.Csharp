// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2024. All rights reserved.
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
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.Utils;

using Google.Protobuf.WellKnownTypes;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Symphony;

/// <summary>
///   The class SessionService will be create each time the function CreateSession or OpenSession will
///   be called by client or by the worker.
/// </summary>
[MarkDownDoc]
public class SessionService : BaseClientSubmitter<SessionService>
{
  /// <summary>
  ///   Ctor to instantiate a new SessionService
  ///   This is an object to send task or get Results from a session
  /// </summary>
  public SessionService(Properties     properties,
                        ILoggerFactory loggerFactory,
                        TaskOptions?   taskOptions = null,
                        Session?       session     = null)
    : base(properties,
           loggerFactory,
           taskOptions ?? InitializeDefaultTaskOptions(),
           session)
  {
  }

  /// <summary>Returns a string that represents the current object.</summary>
  /// <returns>A string that represents the current object.</returns>
  public override string ToString()
    => SessionId.Id ?? "Session_Not_ready";

  /// <summary>
  ///   Default task options
  /// </summary>
  /// <returns></returns>
  [PublicAPI]
  public static TaskOptions InitializeDefaultTaskOptions()
    => new()
       {
         MaxDuration = new Duration
                       {
                         Seconds = 300,
                       },
         MaxRetries           = 3,
         Priority             = 1,
         EngineType           = EngineType.Symphony.ToString(),
         ApplicationName      = "ArmoniK.Samples.SymphonyPackage",
         ApplicationVersion   = "1.X.X",
         ApplicationNamespace = "ArmoniK.Samples.Symphony.Packages",
       };

  /// <summary>
  ///   User method to submit task from the client
  ///   Need a client Service. In case of ServiceContainer
  ///   channel can be null until the OpenSession is called
  /// </summary>
  /// <param name="payloads">
  ///   The user payload list to execute. General used for subTasking.
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="cancellationToken"></param>
  [PublicAPI]
  public IAsyncEnumerable<string> SubmitTasksAsync(IEnumerable<byte[]> payloads,
                                                   int                 maxRetries        = 5,
                                                   TaskOptions?        taskOptions       = null,
                                                   CancellationToken   cancellationToken = default)
    => SubmitTasksWithDependenciesAsync(payloads.Select(payload => new Tuple<byte[], IList<string>>(payload,
                                                                                                    Array.Empty<string>())),
                                        maxRetries,
                                        taskOptions,
                                        cancellationToken);

  /// <summary>
  ///   User method to submit task from the client
  ///   Need a client Service. In case of ServiceContainer
  ///   channel can be null until the OpenSession is called
  /// </summary>
  /// <param name="payloads">
  ///   The user payload list to execute. General used for subTasking.
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  [PublicAPI]
  public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads,
                                         int                 maxRetries  = 5,
                                         TaskOptions?        taskOptions = null)
    => SubmitTasksAsync(payloads,
                        maxRetries,
                        taskOptions)
      .ToEnumerable();

  /// <summary>
  ///   User method to submit task from the client
  /// </summary>
  /// <param name="payload">
  ///   The user payload to execute.
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="cancellationToken"></param>
  [PublicAPI]
  public ValueTask<string> SubmitTaskAsync(byte[]            payload,
                                           int               maxRetries        = 5,
                                           TaskOptions?      taskOptions       = null,
                                           CancellationToken cancellationToken = default)
    => SubmitTasksAsync(new[]
                        {
                          payload,
                        },
                        maxRetries,
                        taskOptions,
                        cancellationToken)
      .SingleAsync(cancellationToken);

  /// <summary>
  ///   User method to submit task from the client
  /// </summary>
  /// <param name="payload">
  ///   The user payload to execute.
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  [PublicAPI]
  public string SubmitTask(byte[]       payload,
                           int          maxRetries  = 5,
                           TaskOptions? taskOptions = null)
    => SubmitTaskAsync(payload,
                       maxRetries,
                       taskOptions)
      .WaitSync();


  /// <summary>
  ///   The method to submit One task with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payload">The payload to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="cancellationToken"></param>
  /// <returns>return the taskId of the created task </returns>
  [PublicAPI]
  public ValueTask<string> SubmitTaskWithDependenciesAsync(byte[]            payload,
                                                           IList<string>     dependencies,
                                                           int               maxRetries        = 5,
                                                           TaskOptions?      taskOptions       = null,
                                                           CancellationToken cancellationToken = default)
    => SubmitTasksWithDependenciesAsync(new[]
                                        {
                                          Tuple.Create(payload,
                                                       dependencies),
                                        },
                                        maxRetries,
                                        taskOptions,
                                        cancellationToken)
      .SingleAsync(cancellationToken);

  /// <summary>
  ///   The method to submit One task with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payload">The payload to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>return the taskId of the created task </returns>
  [PublicAPI]
  public string SubmitTaskWithDependencies(byte[]        payload,
                                           IList<string> dependencies,
                                           int           maxRetries  = 5,
                                           TaskOptions?  taskOptions = null)
    => SubmitTaskWithDependenciesAsync(payload,
                                       dependencies,
                                       maxRetries,
                                       taskOptions)
      .WaitSync();
}
