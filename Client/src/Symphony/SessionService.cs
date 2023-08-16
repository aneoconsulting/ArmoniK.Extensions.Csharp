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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

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
                        TaskOptions?    taskOptions   = null,
                        Session?     session       = null)
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
  // TODO: mark with [PublicApi] ?
  // ReSharper disable once MemberCanBePrivate.Global
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
  public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads,
                                         int                 maxRetries  = 5,
                                         TaskOptions?        taskOptions = null)
    => SubmitTasksWithDependencies(payloads.Select(payload => new Tuple<byte[], IList<string>>(payload,
                                                                                               Array.Empty<string>())),
                                   maxRetries,
                                   taskOptions);

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
  public string SubmitTask(byte[]       payload,
                           int          maxRetries  = 5,
                           TaskOptions? taskOptions = null)
    => SubmitTasks(new[]
                   {
                     payload,
                   },
                   maxRetries,
                   taskOptions)
      .Single();


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
  // TODO: mark with [PublicApi] ?
  // ReSharper disable once UnusedMember.Global
  public string SubmitTaskWithDependencies(byte[]        payload,
                                           IList<string> dependencies,
                                           int           maxRetries  = 5,
                                           TaskOptions?  taskOptions = null)
    => SubmitTasksWithDependencies(new[]
                                   {
                                     Tuple.Create(payload,
                                                  dependencies),
                                   },
                                   maxRetries,
                                   taskOptions)
      .Single();
}
