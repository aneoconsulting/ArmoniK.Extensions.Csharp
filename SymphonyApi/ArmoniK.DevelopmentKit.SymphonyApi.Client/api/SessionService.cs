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

using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client.api;

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
  public SessionService(ChannelPool                channelPool,
                        [CanBeNull] ILoggerFactory loggerFactory = null,
                        [CanBeNull] TaskOptions    taskOptions   = null,
                        [CanBeNull] Session        session       = null)
    : base(channelPool,
           loggerFactory)
  {
    TaskOptions = taskOptions ?? InitializeDefaultTaskOptions();

    Logger?.LogDebug("Creating Session... ");

    SessionId = session ?? CreateSession(new List<string>
                                         {
                                           TaskOptions.PartitionId,
                                         });

    Logger?.LogDebug($"Session Created {SessionId}");
  }

  /// <summary>Returns a string that represents the current object.</summary>
  /// <returns>A string that represents the current object.</returns>
  public override string ToString()
    => SessionId?.Id ?? "Session_Not_ready";

  /// <summary>
  ///   Default task options
  /// </summary>
  /// <returns></returns>
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

  private Session CreateSession(IEnumerable<string> partitionIds)
  {
    using var _ = Logger?.LogFunction();
    var createSessionRequest = new CreateSessionRequest
                               {
                                 DefaultTaskOption = TaskOptions,
                                 PartitionIds =
                                 {
                                   partitionIds,
                                 },
                               };
    var session = channelPool_.WithChannel(channel => new Submitter.SubmitterClient(channel).CreateSession(createSessionRequest));

    return new Session
           {
             Id = session.SessionId,
           };
  }

  /// <summary>
  ///   Set connection to an already opened Session
  /// </summary>
  /// <param name="session">SessionId previously opened</param>
  public void OpenSession(Session session)
  {
    if (SessionId == null)
    {
      Logger?.LogDebug($"Open Session {session.Id}");
    }

    SessionId = session;
  }

  /// <summary>
  ///   User method to submit task from the client
  ///   Need a client Service. In case of ServiceContainer
  ///   channel can be null until the OpenSession is called
  /// </summary>
  /// <param name="payloads">
  ///   The user payload list to execute. General used for subTasking.
  /// </param>
  public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    => SubmitTasksWithDependencies(payloads.Select(payload => new Tuple<byte[], IList<string>>(payload,
                                                                                               null)));

  /// <summary>
  ///   User method to submit task from the client
  /// </summary>
  /// <param name="payload">
  ///   The user payload to execute.
  /// </param>
  public string SubmitTask(byte[] payload)
    => SubmitTasks(new[]
                   {
                     payload,
                   })
      .Single();


  /// <summary>
  ///   The method to submit One task with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payload">The payload to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <returns>return the taskId of the created task </returns>
  public string SubmitTaskWithDependencies(byte[]        payload,
                                           IList<string> dependencies)
    => SubmitTasksWithDependencies(new[]
                                   {
                                     Tuple.Create(payload,
                                                  dependencies),
                                   })
      .Single();
}
