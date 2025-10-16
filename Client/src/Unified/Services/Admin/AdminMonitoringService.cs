// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2025. All rights reserved.
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Sessions;
using ArmoniK.Api.gRPC.V1.SortDirection;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.Utils;
using ArmoniK.Utils.Pool;

using Grpc.Net.Client;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using FilterField = ArmoniK.Api.gRPC.V1.Sessions.FilterField;
using Filters = ArmoniK.Api.gRPC.V1.Tasks.Filters;
using FiltersAnd = ArmoniK.Api.gRPC.V1.Sessions.FiltersAnd;
using FilterStatus = ArmoniK.Api.gRPC.V1.Sessions.FilterStatus;
using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;

/// <summary>
///   The Administration and monitoring class to get or manage information on grid server
/// </summary>
public class AdminMonitoringService
{
  private readonly ObjectPool<GrpcChannel> channelPool_;

  /// <summary>
  ///   The constructor to instantiate this service
  /// </summary>
  /// <param name="channel">The entry point to the control plane</param>
  /// <param name="loggerFactory">The factory logger to create logger</param>
  public AdminMonitoringService(ObjectPool<GrpcChannel> channelPool,
                                ILoggerFactory?         loggerFactory = null)
  {
    Logger       = loggerFactory?.CreateLogger<AdminMonitoringService>();
    channelPool_ = channelPool;
  }

  private ILogger? Logger { get; }


  /// <summary>
  ///   Returns the service configuration
  /// </summary>
  public void GetServiceConfiguration()
  {
    using var channel       = channelPool_.Get();
    var       resultsClient = new Results.ResultsClient(channel);
    var       configuration = resultsClient.GetServiceConfiguration(new Empty());
    Logger?.LogInformation($"This configuration will be update in the nex version [ {configuration} ]");
  }

  /// <summary>
  ///   This method can mark the session in status Cancelled and
  ///   mark all tasks in cancel status
  /// </summary>
  /// <param name="sessionId">the sessionId of the session to cancel</param>
  /// <param name="cancellationToken"></param>
  [PublicAPI]
  public async ValueTask CancelSessionAsync(string            sessionId,
                                            CancellationToken cancellationToken = default)
  {
    await channelPool_.WithSessionClient(Logger)
                      .WithDefaultRetries()
                      .ExecuteAsync(client => client.CancelSessionAsync(new CancelSessionRequest
                                                                        {
                                                                          SessionId = sessionId,
                                                                        },
                                                                        cancellationToken: cancellationToken),
                                    cancellationToken)
                      .ConfigureAwait(false);

    Logger?.LogDebug("Session cancelled {sessionId}",
                     sessionId);
  }

  /// <summary>
  ///   This method can mark the session in status Cancelled and
  ///   mark all tasks in cancel status
  /// </summary>
  /// <param name="sessionId">the sessionId of the session to cancel</param>
  [PublicAPI]
  public void CancelSession(string sessionId)
    => CancelSessionAsync(sessionId)
      .WaitSync();

  /// <summary>
  ///   Return the whole list of task of a session
  /// </summary>
  /// <returns>The list of filtered task </returns>
  public IEnumerable<string> ListAllTasksBySession(string sessionId)
    => ListTasksBySession(sessionId);

  /// <summary>
  ///   Return the list of task of a session filtered by status
  /// </summary>
  /// <returns>The list of filtered task </returns>
  [PublicAPI]
  public IEnumerable<string> ListTasksBySession(string              sessionId,
                                                params TaskStatus[] taskStatus)
    => ListTasksBySessionAsync(sessionId,
                               taskStatus)
      .ToEnumerable();

  /// <summary>
  ///   Return the list of task of a session filtered by status
  /// </summary>
  /// <returns>The list of filtered task </returns>
  [PublicAPI]
  public IAsyncEnumerable<string> ListTasksBySessionAsync(string              sessionId,
                                                          CancellationToken   cancellationToken = default,
                                                          params TaskStatus[] taskStatus)
    => ListTasksBySessionAsync(sessionId,
                               taskStatus,
                               cancellationToken);

  /// <summary>
  ///   Return the list of task of a session filtered by status
  /// </summary>
  /// <returns>The list of filtered task </returns>
  private IAsyncEnumerable<string> ListTasksBySessionAsync(string            sessionId,
                                                           TaskStatus[]      taskStatus,
                                                           CancellationToken cancellationToken = default)
  {
    var filter = new Filters
                 {
                   Or =
                   {
                     taskStatus.Select(status => TasksClientExt.TaskStatusFilter(status,
                                                                                 sessionId)),
                   },
                 };
    var sort = new ListTasksRequest.Types.Sort
               {
                 Field = new TaskField
                         {
                           TaskSummaryField = new TaskSummaryField
                                              {
                                                Field = TaskSummaryEnumField.TaskId,
                                              },
                         },
                 Direction = SortDirection.Asc,
               };
    return channelPool_.ListTasksAsync(filter,
                                       sort,
                                       cancellationToken: cancellationToken)
                       .Select(static task => task.Id);
  }

  /// <summary>
  ///   Return the list of running tasks of a session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns>The list of filtered task </returns>
  [Obsolete]
  public IEnumerable<string> ListRunningTasks(string sessionId)
    => ListTasksBySession(sessionId,
                          TaskStatus.Processing);

  /// <summary>
  ///   Return the list of error tasks of a session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns>The list of filtered task </returns>
  [Obsolete]
  public IEnumerable<string> ListErrorTasks(string sessionId)
    => ListTasksBySession(sessionId,
                          TaskStatus.Error);

  /// <summary>
  ///   Return the list of canceled tasks of a session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns>The list of filtered task </returns>
  [Obsolete]
  public IEnumerable<string> ListCancelledTasks(string sessionId)
    => ListTasksBySession(sessionId,
                          TaskStatus.Cancelled);

  /// <summary>
  ///   Return the list of all sessions
  /// </summary>
  /// <returns>The list of filtered session </returns>
  [PublicAPI]
  public IEnumerable<string> ListAllSessions()
    => ListAllSessionsAsync()
      .ToEnumerable();

  /// <summary>
  ///   Return the list of all sessions
  /// </summary>
  /// <returns>The list of filtered session </returns>
  [PublicAPI]
  public async IAsyncEnumerable<string> ListAllSessionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var sessions = await channelPool_.WithSessionClient(Logger)
                                     .WithDefaultRetries()
                                     .ExecuteAsync(client => client.ListSessionsAsync(new ListSessionsRequest(),
                                                                                      cancellationToken: cancellationToken),
                                                   cancellationToken)
                                     .ConfigureAwait(false);

    foreach (var session in sessions.Sessions)
    {
      yield return session.SessionId;
    }
  }


  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  [PublicAPI]
  public IEnumerable<string> ListRunningSessions()
    => ListRunningSessionsAsync()
      .ToEnumerable();

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  [PublicAPI]
  public async IAsyncEnumerable<string> ListRunningSessionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var sessions = await channelPool_.WithSessionClient(Logger)
                                     .WithDefaultRetries()
                                     .ExecuteAsync(client =>
                                                   {
                                                     var request = new ListSessionsRequest
                                                                   {
                                                                     Filters = new Api.gRPC.V1.Sessions.Filters
                                                                               {
                                                                                 Or =
                                                                                 {
                                                                                   new FiltersAnd
                                                                                   {
                                                                                     And =
                                                                                     {
                                                                                       new FilterField
                                                                                       {
                                                                                         FilterStatus = new FilterStatus
                                                                                                        {
                                                                                                          Operator = FilterStatusOperator.Equal,
                                                                                                          Value    = SessionStatus.Running,
                                                                                                        },
                                                                                         Field = new SessionField
                                                                                                 {
                                                                                                   SessionRawField = new SessionRawField
                                                                                                                     {
                                                                                                                       Field = SessionRawEnumField.Status,
                                                                                                                     },
                                                                                                 },
                                                                                       },
                                                                                     },
                                                                                   },
                                                                                 },
                                                                               },
                                                                   };
                                                     return client.ListSessionsAsync(request,
                                                                                     cancellationToken: cancellationToken);
                                                   },
                                                   cancellationToken)
                                     .ConfigureAwait(false);

    foreach (var session in sessions.Sessions)
    {
      yield return session.SessionId;
    }
  }

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  [PublicAPI]
  public IEnumerable<string> ListCancelledSessions()
    => ListCancelledSessionsAsync()
      .ToEnumerable();

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  [PublicAPI]
  public async IAsyncEnumerable<string> ListCancelledSessionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    await using var channel = await channelPool_.GetAsync(cancellationToken)
                                                .ConfigureAwait(false);
    var sessionsClient = new Sessions.SessionsClient(channel);

    ListSessionsResponse sessions;
    try
    {
      sessions = await sessionsClient.ListSessionsAsync(new ListSessionsRequest
                                                        {
                                                          Filters = new Api.gRPC.V1.Sessions.Filters
                                                                    {
                                                                      Or =
                                                                      {
                                                                        new FiltersAnd
                                                                        {
                                                                          And =
                                                                          {
                                                                            new FilterField
                                                                            {
                                                                              FilterStatus = new FilterStatus
                                                                                             {
                                                                                               Operator = FilterStatusOperator.Equal,
                                                                                               Value    = SessionStatus.Cancelled,
                                                                                             },
                                                                              Field = new SessionField
                                                                                      {
                                                                                        SessionRawField = new SessionRawField
                                                                                                          {
                                                                                                            Field = SessionRawEnumField.Status,
                                                                                                          },
                                                                                      },
                                                                            },
                                                                          },
                                                                        },
                                                                      },
                                                                    },
                                                        },
                                                        cancellationToken: cancellationToken)
                                     .ConfigureAwait(false);
    }
    catch (Exception e)
    {
      channel.Exception = e;
      throw;
    }

    foreach (var session in sessions.Sessions)
    {
      yield return session.SessionId;
    }
  }

  /// <summary>
  ///   The method is to get the number of all task in a session
  /// </summary>
  /// <returns>return the number of task</returns>
  public int CountAllTasksBySession(string sessionId)
    => CountTaskBySession(sessionId);


  /// <summary>
  ///   The method is to get the number of running tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  public int CountRunningTasksBySession(string sessionId)
    => CountTaskBySession(sessionId,
                          TaskStatus.Processing);

  /// <summary>
  ///   The method is to get the number of error tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  public int CountErrorTasksBySession(string sessionId)
    => CountTaskBySession(sessionId,
                          TaskStatus.Error);

  /// <summary>
  ///   Count task in a session and select by status
  /// </summary>
  /// <param name="sessionId">the id of the session</param>
  /// <param name="taskStatus">a variadic list of taskStatus </param>
  /// <returns>return the number of task</returns>
  [PublicAPI]
  public int CountTaskBySession(string              sessionId,
                                params TaskStatus[] taskStatus)
    => CountTaskBySessionAsync(sessionId,
                               taskStatus)
      .WaitSync();

  /// <summary>
  ///   Count task in a session and select by status
  /// </summary>
  /// <param name="sessionId">the id of the session</param>
  /// <param name="cancellationToken"></param>
  /// <param name="taskStatus">a variadic list of taskStatus </param>
  /// <returns>return the number of task</returns>
  [PublicAPI]
  private ValueTask<int> CountTaskBySessionAsync(string              sessionId,
                                                 CancellationToken   cancellationToken = default,
                                                 params TaskStatus[] taskStatus)
    => CountTaskBySessionAsync(sessionId,
                               taskStatus,
                               cancellationToken);

  /// <summary>
  ///   Count task in a session and select by status
  /// </summary>
  /// <param name="sessionId">the id of the session</param>
  /// <param name="taskStatus">a variadic list of taskStatus </param>
  /// <param name="cancellationToken"></param>
  /// <returns>return the number of task</returns>
  private ValueTask<int> CountTaskBySessionAsync(string            sessionId,
                                                 TaskStatus[]      taskStatus,
                                                 CancellationToken cancellationToken = default)
    => channelPool_.WithTaskClient(Logger)
                   .WithDefaultRetries()
                   .ExecuteAsync(client => client.CountTasksByStatusAsync(new CountTasksByStatusRequest
                                                                          {
                                                                            Filters = new Filters
                                                                                      {
                                                                                        Or =
                                                                                        {
                                                                                          taskStatus.Select(status => TasksClientExt.TaskStatusFilter(status,
                                                                                                                                                      sessionId)),
                                                                                        },
                                                                                      },
                                                                          },
                                                                          cancellationToken: cancellationToken),
                                 cancellationToken)
                   .AndThen(static counts => counts.Status.Sum(static count => count.Count));

  /// <summary>
  ///   The method is to get the number of error tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  [Obsolete]
  public int CountCancelTasksBySession(string sessionId)
    => CountTaskBySession(sessionId,
                          TaskStatus.Cancelled);

  /// <summary>
  ///   The method is to get the number of error tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  [Obsolete]
  public int CountCompletedTasksBySession(string sessionId)
    => CountTaskBySession(sessionId,
                          TaskStatus.Completed);

  /// <summary>
  ///   Cancel a list of task in a session
  /// </summary>
  /// <param name="taskIds">the taskIds list to cancel</param>
  [PublicAPI]
  public void CancelTasksBySession(IEnumerable<string> taskIds)
    => CancelTasksBySessionAsync(taskIds)
      .WaitSync();

  /// <summary>
  ///   Cancel a list of task in a session
  /// </summary>
  /// <param name="taskIds">the taskIds list to cancel</param>
  /// <param name="cancellationToken"></param>
  [PublicAPI]
  public ValueTask CancelTasksBySessionAsync(IEnumerable<string> taskIds,
                                             CancellationToken   cancellationToken = default)
    => channelPool_.WithTaskClient(Logger)
                   .WithDefaultRetries()
                   .ExecuteAsync(client => client.CancelTasksAsync(new CancelTasksRequest
                                                                   {
                                                                     TaskIds =
                                                                     {
                                                                       taskIds,
                                                                     },
                                                                   },
                                                                   cancellationToken: cancellationToken),
                                 cancellationToken)
                   .AndThen(static _ =>
                            {
                            });

  /// <summary>
  ///   The method to get status of a list of tasks
  /// </summary>
  /// <param name="taskIds">The list of task</param>
  /// <returns>returns a list of pair TaskId/TaskStatus</returns>
  [PublicAPI]
  public IEnumerable<Tuple<string, TaskStatus>> GetTaskStatus(IEnumerable<string> taskIds)
    => GetTaskStatusAsync(taskIds)
      .ToEnumerable();

  /// <summary>
  ///   The method to get status of a list of tasks
  /// </summary>
  /// <param name="taskIds">The list of task</param>
  /// <param name="cancellationToken"></param>
  /// <returns>returns a list of pair TaskId/TaskStatus</returns>
  [PublicAPI]
  public IAsyncEnumerable<Tuple<string, TaskStatus>> GetTaskStatusAsync(IEnumerable<string> taskIds,
                                                                        CancellationToken   cancellationToken = default)
    => channelPool_.ListTasksAsync(new Filters
                                   {
                                     Or =
                                     {
                                       taskIds.Select(TasksClientExt.TaskIdFilter),
                                     },
                                   },
                                   new ListTasksRequest.Types.Sort
                                   {
                                     Direction = SortDirection.Asc,
                                     Field = new TaskField
                                             {
                                               TaskSummaryField = new TaskSummaryField
                                                                  {
                                                                    Field = TaskSummaryEnumField.TaskId,
                                                                  },
                                             },
                                   },
                                   cancellationToken: cancellationToken)
                   .Select(static task => new Tuple<string, TaskStatus>(task.Id,
                                                                        task.Status));


  private void UploadResources(string path)
    => throw new NotImplementedException();

  private void DeployResources()
    => throw new NotImplementedException();

  private void DeleteResources()
    => throw new NotImplementedException();

  private void DownloadResource(string path)
    => throw new NotImplementedException();

  private IEnumerable<string> ListResources()
    => throw new NotImplementedException();

  private void GetRegisteredServices()
    => throw new NotImplementedException();

  private void RegisterService(string name)
    => throw new NotImplementedException();

  private void UnRegisterService(string name)
    => throw new NotImplementedException();

  private void GetServiceBinding(string name)
    => throw new NotImplementedException();

  private void ResourceExists(string name)
    => throw new NotImplementedException();

  private string UploadResource(string filepath)
    => throw new NotImplementedException();
}
