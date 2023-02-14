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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services;

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
  public SessionService(ChannelPool     channelPool,
                        ILoggerFactory? loggerFactory = null,
                        TaskOptions?    taskOptions   = null,
                        Session?        session       = null)
    : base(channelPool,
           loggerFactory)
  {
    TaskOptions = taskOptions ?? InitializeDefaultTaskOptions();

    Logger.LogDebug("Creating Session... ");

    SessionId = session ?? CreateSession(taskOptions != null
                                           ? new List<string>
                                             {
                                               taskOptions.PartitionId,
                                             }
                                           : Array.Empty<string>());

    Logger.LogDebug($"Session Created {SessionId}");
  }

  /// <summary>Returns a string that represents the current object.</summary>
  /// <returns>A string that represents the current object.</returns>
  [PublicAPI]
  public override string ToString()
    => SessionId.Id;

  /// <summary>
  ///   Supply a default TaskOptions
  /// </summary>
  /// <returns>A default TaskOptions object</returns>
  [PublicAPI]
  public static TaskOptions InitializeDefaultTaskOptions()
  {
    TaskOptions taskOptions = new()
                              {
                                MaxDuration = new Duration
                                              {
                                                Seconds = 40,
                                              },
                                MaxRetries           = 2,
                                Priority             = 1,
                                EngineType           = EngineType.Unified.ToString(),
                                ApplicationName      = "ArmoniK.DevelopmentKit.Worker.Unified",
                                ApplicationVersion   = "1.X.X",
                                ApplicationNamespace = "ArmoniK.DevelopmentKit.Worker.Unified",
                                ApplicationService   = "FallBackServerAdder",
                              };

    return taskOptions;
  }

  private Session CreateSession(IEnumerable<string> partitionIds)
  {
    using var _ = Logger.LogFunction();
    var createSessionRequest = new CreateSessionRequest
                               {
                                 DefaultTaskOption = TaskOptions,
                                 PartitionIds =
                                 {
                                   partitionIds,
                                 },
                               };
    var session = ChannelPool.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CreateSession(createSessionRequest));

    return new Session
           {
             Id = session.SessionId,
           };
  }

  /// <summary>
  ///   Set connection to an already opened Session
  /// </summary>
  /// <param name="session">SessionId previously opened</param>
  [PublicAPI]
  public void OpenSession(Session session)
  {
    if (SessionId.Id == "")
    {
      Logger.LogDebug($"Open Session {session.Id}");
    }

    SessionId = session;
  }

  /// <summary>
  ///   User method to submit task from the client
  ///   Need a client Service. In case of ServiceContainer
  ///   submitterService can be null until the OpenSession is called
  /// </summary>
  /// <param name="payloads">
  ///   The user payload list to execute. General used for subTasking.
  /// </param>
  [PublicAPI]
  public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    => SubmitTasksWithDependencies(payloads.Select(payload => new Tuple<byte[], IList<string>>(payload,
                                                                                               Array.Empty<string>())));

  /// <summary>
  ///   User method to submit task from the client
  /// </summary>
  /// <param name="payload">
  ///   The user payload to execute.
  /// </param>
  /// <param name="waitTimeBeforeNextSubmit">The time to wait before 2 single submitTask</param>
  [PublicAPI]
  public string SubmitTask(byte[] payload,
                           int    waitTimeBeforeNextSubmit = 2)
  {
    Thread.Sleep(waitTimeBeforeNextSubmit); // Twice the keep alive
    return SubmitTasks(new[]
                       {
                         payload,
                       })
      .Single();
  }


  /// <summary>
  ///   The method to submit One task with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payload">The payload to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <returns>return the taskId of the created task </returns>
  [PublicAPI]
  public string SubmitTaskWithDependencies(byte[]        payload,
                                           IList<string> dependencies)
    => SubmitTasksWithDependencies(new[]
                                   {
                                     Tuple.Create(payload,
                                                  dependencies),
                                   })
      .Single();
}
