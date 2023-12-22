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
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Sessions;
using ArmoniK.Api.gRPC.V1.SortDirection;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;

using Microsoft.Extensions.Logging;

using FilterField = ArmoniK.Api.gRPC.V1.Sessions.FilterField;
using Filters = ArmoniK.Api.gRPC.V1.Tasks.Filters;
using FiltersAnd = ArmoniK.Api.gRPC.V1.Sessions.FiltersAnd;
using FilterStatus = ArmoniK.Api.gRPC.V1.Sessions.FilterStatus;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;

/// <summary>
///   The Administration and monitoring class to get or manage information on grid server
/// </summary>
public class AdminMonitoringService
{
  private readonly ChannelPool channelPool_;

  /// <summary>
  ///   The constructor to instantiate this service
  /// </summary>
  /// <param name="channel">The entry point to the control plane</param>
  /// <param name="loggerFactory">The factory logger to create logger</param>
  public AdminMonitoringService(ChannelPool     channelPool,
                                ILoggerFactory? loggerFactory = null)
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
    using var channel       = channelPool_.GetChannel();
    var       resultsClient = new Results.ResultsClient(channel);
    var       configuration = resultsClient.GetServiceConfiguration(new Empty());
    Logger?.LogInformation($"This configuration will be update in the nex version [ {configuration} ]");
  }

  /// <summary>
  ///   This method can mark the session in status Cancelled and
  ///   mark all tasks in cancel status
  /// </summary>
  /// <param name="sessionId">the sessionId of the session to cancel</param>
  public void CancelSession(string sessionId)
  {
    using var channel        = channelPool_.GetChannel();
    var       sessionsClient = new Sessions.SessionsClient(channel);
    sessionsClient.CancelSession(new CancelSessionRequest
                                 {
                                   SessionId = sessionId,
                                 });
    Logger.LogDebug("Session cancelled {sessionId}",
                    sessionId);
  }

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
  public IEnumerable<string> ListTasksBySession(string              sessionId,
                                                params TaskStatus[] taskStatus)
  {
    using var channel     = channelPool_.GetChannel();
    var       tasksClient = new Tasks.TasksClient(channel);

    return tasksClient.ListTasks(new Filters
                                 {
                                   Or =
                                   {
                                     taskStatus.Select(status => TasksClientExt.TaskStatusFilter(status,
                                                                                                 sessionId)),
                                   },
                                 },
                                 new ListTasksRequest.Types.Sort
                                 {
                                   Field = new TaskField
                                           {
                                             TaskSummaryField = new TaskSummaryField
                                                                {
                                                                  Field = TaskSummaryEnumField.TaskId,
                                                                },
                                           },
                                   Direction = SortDirection.Asc,
                                 })
                      .Select(summary => summary.Id);
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
  public IEnumerable<string> ListAllSessions()
  {
    using var channel        = channelPool_.GetChannel();
    var       sessionsClient = new Sessions.SessionsClient(channel);
    return sessionsClient.ListSessions(new ListSessionsRequest())
                         .Sessions.Select(session => session.SessionId);
  }


  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  public IEnumerable<string> ListRunningSessions()
  {
    using var channel        = channelPool_.GetChannel();
    var       sessionsClient = new Sessions.SessionsClient(channel);
    return sessionsClient.ListSessions(new ListSessionsRequest
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
                                       })
                         .Sessions.Select(session => session.SessionId);
  }

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  public IEnumerable<string> ListCancelledSessions()
  {
    using var channel        = channelPool_.GetChannel();
    var       sessionsClient = new Sessions.SessionsClient(channel);
    return sessionsClient.ListSessions(new ListSessionsRequest
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
                                       })
                         .Sessions.Select(session => session.SessionId);
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
  public int CountTaskBySession(string              sessionId,
                                params TaskStatus[] taskStatus)
  {
    using var channel     = channelPool_.GetChannel();
    var       tasksClient = new Tasks.TasksClient(channel);
    return tasksClient.CountTasksByStatus(new CountTasksByStatusRequest
                                          {
                                            Filters = new Filters
                                                      {
                                                        Or =
                                                        {
                                                          taskStatus.Select(status => TasksClientExt.TaskStatusFilter(status,
                                                                                                                      sessionId)),
                                                        },
                                                      },
                                          })
                      .Status.Sum(count => count.Count);
  }

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
  public void CancelTasksBySession(IEnumerable<string> taskIds)
  {
    using var channel     = channelPool_.GetChannel();
    var       tasksClient = new Tasks.TasksClient(channel);
    tasksClient.CancelTasks(new CancelTasksRequest
                            {
                              TaskIds =
                              {
                                taskIds,
                              },
                            });
  }

  /// <summary>
  ///   The method to get status of a list of tasks
  /// </summary>
  /// <param name="taskIds">The list of task</param>
  /// <returns>returns a list of pair TaskId/TaskStatus</returns>
  public IEnumerable<Tuple<string, TaskStatus>> GetTaskStatus(IEnumerable<string> taskIds)
  {
    using var channel     = channelPool_.GetChannel();
    var       tasksClient = new Tasks.TasksClient(channel);
    return tasksClient.ListTasks(new Filters
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
                                 })
                      .Select(task => new Tuple<string, TaskStatus>(task.Id,
                                                                    task.Status));
  }


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
