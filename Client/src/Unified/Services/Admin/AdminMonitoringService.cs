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
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

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
  public AdminMonitoringService(ChannelPool                channelPool,
                                [CanBeNull] ILoggerFactory loggerFactory = null)
  {
    Logger       = loggerFactory?.CreateLogger<AdminMonitoringService>();
    channelPool_ = channelPool;
  }

  [CanBeNull]
  private ILogger Logger { get; }


  /// <summary>
  ///   Returns the service configuration
  /// </summary>
  public void GetServiceConfiguration()
  {
    var configuration = channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).GetServiceConfiguration(new Empty()));

    Logger?.LogInformation($"This configuration will be update in the nex version [ {configuration} ]");
  }

  /// <summary>
  ///   This method can mark the session in status Cancelled and
  ///   mark all tasks in cancel status
  /// </summary>
  /// <param name="sessionId">the sessionId of the session to cancel</param>
  public void CancelSession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CancelSession(new Session
                                                                                                                      {
                                                                                                                        Id = sessionId,
                                                                                                                      }));

  /// <summary>
  ///   Return the filtered list of task of a session
  /// </summary>
  /// <param name="taskFilter">The filter to apply on list of task</param>
  /// <returns>The list of filtered task </returns>
  public IEnumerable<string> ListTasks(TaskFilter taskFilter)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(taskFilter)
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the whole list of task of a session
  /// </summary>
  /// <returns>The list of filtered task </returns>
  public IEnumerable<string> ListAllTasksBySession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(new TaskFilter
                                                                                                                  {
                                                                                                                    Session = new TaskFilter.Types.IdsRequest
                                                                                                                              {
                                                                                                                                Ids =
                                                                                                                                {
                                                                                                                                  sessionId,
                                                                                                                                },
                                                                                                                              },
                                                                                                                  })
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the list of task of a session filtered by status
  /// </summary>
  /// <returns>The list of filtered task </returns>
  public IEnumerable<string> ListTasksBySession(string              sessionId,
                                                params TaskStatus[] taskStatus)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(new TaskFilter
                                                                                                                  {
                                                                                                                    Session = new TaskFilter.Types.IdsRequest
                                                                                                                              {
                                                                                                                                Ids =
                                                                                                                                {
                                                                                                                                  sessionId,
                                                                                                                                },
                                                                                                                              },
                                                                                                                    Included = new TaskFilter.Types.StatusesRequest
                                                                                                                               {
                                                                                                                                 Statuses =
                                                                                                                                 {
                                                                                                                                   taskStatus,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                  })
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the list of running tasks of a session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns>The list of filtered task </returns>
  [Obsolete]
  public IEnumerable<string> ListRunningTasks(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(new TaskFilter
                                                                                                                  {
                                                                                                                    Session = new TaskFilter.Types.IdsRequest
                                                                                                                              {
                                                                                                                                Ids =
                                                                                                                                {
                                                                                                                                  sessionId,
                                                                                                                                },
                                                                                                                              },
                                                                                                                    Included = new TaskFilter.Types.StatusesRequest
                                                                                                                               {
                                                                                                                                 Statuses =
                                                                                                                                 {
                                                                                                                                   TaskStatus.Creating,
                                                                                                                                   TaskStatus.Dispatched,
                                                                                                                                   TaskStatus.Processing,
                                                                                                                                   TaskStatus.Submitted,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                  })
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the list of error tasks of a session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns>The list of filtered task </returns>
  [Obsolete]
  public IEnumerable<string> ListErrorTasks(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(new TaskFilter
                                                                                                                  {
                                                                                                                    Session = new TaskFilter.Types.IdsRequest
                                                                                                                              {
                                                                                                                                Ids =
                                                                                                                                {
                                                                                                                                  sessionId,
                                                                                                                                },
                                                                                                                              },
                                                                                                                    Included = new TaskFilter.Types.StatusesRequest
                                                                                                                               {
                                                                                                                                 Statuses =
                                                                                                                                 {
                                                                                                                                   TaskStatus.Error,
                                                                                                                                   TaskStatus.Timeout,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                  })
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the list of canceled tasks of a session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns>The list of filtered task </returns>
  [Obsolete]
  public IEnumerable<string> ListCancelledTasks(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(new TaskFilter
                                                                                                                  {
                                                                                                                    Session = new TaskFilter.Types.IdsRequest
                                                                                                                              {
                                                                                                                                Ids =
                                                                                                                                {
                                                                                                                                  sessionId,
                                                                                                                                },
                                                                                                                              },
                                                                                                                    Included = new TaskFilter.Types.StatusesRequest
                                                                                                                               {
                                                                                                                                 Statuses =
                                                                                                                                 {
                                                                                                                                   TaskStatus.Cancelled,
                                                                                                                                   TaskStatus.Cancelling,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                  })
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the list of all sessions
  /// </summary>
  /// <returns>The list of filtered session </returns>
  public IEnumerable<string> ListAllSessions()
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListSessions(new SessionFilter())
                                                                                                       .SessionIds);

  /// <summary>
  ///   The method is to get a filtered list of session
  /// </summary>
  /// <param name="sessionFilter">The filter to apply on the request</param>
  /// <returns>returns a list of session filtered</returns>
  public IEnumerable<string> ListSessions(SessionFilter sessionFilter)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListSessions(sessionFilter)
                                                                                                       .SessionIds);

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  public IEnumerable<string> ListRunningSessions()
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListSessions(new SessionFilter
                                                                                                                     {
                                                                                                                       Included = new SessionFilter.Types.StatusesRequest
                                                                                                                                  {
                                                                                                                                    Statuses =
                                                                                                                                    {
                                                                                                                                      SessionStatus.Running,
                                                                                                                                    },
                                                                                                                                  },
                                                                                                                     })
                                                                                                       .SessionIds);

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  public IEnumerable<string> ListCancelledSessions()
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListSessions(new SessionFilter
                                                                                                                     {
                                                                                                                       Included = new SessionFilter.Types.StatusesRequest
                                                                                                                                  {
                                                                                                                                    Statuses =
                                                                                                                                    {
                                                                                                                                      SessionStatus.Cancelled,
                                                                                                                                    },
                                                                                                                                  },
                                                                                                                     })
                                                                                                       .SessionIds);

  /// <summary>
  ///   The method is to get the number of filtered tasks
  /// </summary>
  /// <param name="taskFilter">the filter to apply on tasks</param>
  /// <returns>return the number of task</returns>
  public int CountTasks(TaskFilter taskFilter)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(taskFilter)
                                                                                                       .Values.Count);

  /// <summary>
  ///   The method is to get the number of all task in a session
  /// </summary>
  /// <returns>return the number of task</returns>
  public int CountAllTasksBySession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(new TaskFilter
                                                                                                                   {
                                                                                                                     Session = new TaskFilter.Types.IdsRequest
                                                                                                                               {
                                                                                                                                 Ids =
                                                                                                                                 {
                                                                                                                                   sessionId,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                   })
                                                                                                       .Values.Count);


  /// <summary>
  ///   The method is to get the number of running tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  public int CountRunningTasksBySession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(new TaskFilter
                                                                                                                   {
                                                                                                                     Session = new TaskFilter.Types.IdsRequest
                                                                                                                               {
                                                                                                                                 Ids =
                                                                                                                                 {
                                                                                                                                   sessionId,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                     Included = new TaskFilter.Types.StatusesRequest
                                                                                                                                {
                                                                                                                                  Statuses =
                                                                                                                                  {
                                                                                                                                    TaskStatus.Creating,
                                                                                                                                    TaskStatus.Dispatched,
                                                                                                                                    TaskStatus.Processing,
                                                                                                                                    TaskStatus.Submitted,
                                                                                                                                  },
                                                                                                                                },
                                                                                                                   })
                                                                                                       .Values.Count);

  /// <summary>
  ///   The method is to get the number of error tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  public int CountErrorTasksBySession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(new TaskFilter
                                                                                                                   {
                                                                                                                     Session = new TaskFilter.Types.IdsRequest
                                                                                                                               {
                                                                                                                                 Ids =
                                                                                                                                 {
                                                                                                                                   sessionId,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                     Included = new TaskFilter.Types.StatusesRequest
                                                                                                                                {
                                                                                                                                  Statuses =
                                                                                                                                  {
                                                                                                                                    TaskStatus.Error,
                                                                                                                                    TaskStatus.Timeout,
                                                                                                                                  },
                                                                                                                                },
                                                                                                                   })
                                                                                                       .Values.Count);

  /// <summary>
  ///   Count task in a session and select by status
  /// </summary>
  /// <param name="sessionId">the id of the session</param>
  /// <param name="taskStatus">a variadic list of taskStatus </param>
  /// <returns>return the number of task</returns>
  public int CountTaskBySession(string              sessionId,
                                params TaskStatus[] taskStatus)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(new TaskFilter
                                                                                                                   {
                                                                                                                     Session = new TaskFilter.Types.IdsRequest
                                                                                                                               {
                                                                                                                                 Ids =
                                                                                                                                 {
                                                                                                                                   sessionId,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                     Included = new TaskFilter.Types.StatusesRequest
                                                                                                                                {
                                                                                                                                  Statuses =
                                                                                                                                  {
                                                                                                                                    taskStatus,
                                                                                                                                  },
                                                                                                                                },
                                                                                                                   })
                                                                                                       .Values.Count);

  /// <summary>
  ///   The method is to get the number of error tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  [Obsolete]
  public int CountCancelTasksBySession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(new TaskFilter
                                                                                                                   {
                                                                                                                     Session = new TaskFilter.Types.IdsRequest
                                                                                                                               {
                                                                                                                                 Ids =
                                                                                                                                 {
                                                                                                                                   sessionId,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                     Included = new TaskFilter.Types.StatusesRequest
                                                                                                                                {
                                                                                                                                  Statuses =
                                                                                                                                  {
                                                                                                                                    TaskStatus.Cancelling,
                                                                                                                                    TaskStatus.Cancelled,
                                                                                                                                  },
                                                                                                                                },
                                                                                                                   })
                                                                                                       .Values.Count);

  /// <summary>
  ///   The method is to get the number of error tasks in the session
  /// </summary>
  /// <returns>return the number of task</returns>
  [Obsolete]
  public int CountCompletedTasksBySession(string sessionId)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(new TaskFilter
                                                                                                                   {
                                                                                                                     Session = new TaskFilter.Types.IdsRequest
                                                                                                                               {
                                                                                                                                 Ids =
                                                                                                                                 {
                                                                                                                                   sessionId,
                                                                                                                                 },
                                                                                                                               },
                                                                                                                     Included = new TaskFilter.Types.StatusesRequest
                                                                                                                                {
                                                                                                                                  Statuses =
                                                                                                                                  {
                                                                                                                                    TaskStatus.Completed,
                                                                                                                                  },
                                                                                                                                },
                                                                                                                   })
                                                                                                       .Values.Count);

  /// <summary>
  ///   Cancel a list of task in a session
  /// </summary>
  /// <param name="taskIds">the taskIds list to cancel</param>
  public void CancelTasksBySession(IEnumerable<string> taskIds)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CancelTasks(new TaskFilter
                                                                                                                    {
                                                                                                                      Task = new TaskFilter.Types.IdsRequest
                                                                                                                             {
                                                                                                                               Ids =
                                                                                                                               {
                                                                                                                                 taskIds,
                                                                                                                               },
                                                                                                                             },
                                                                                                                    }));

  /// <summary>
  ///   The method to get status of a list of tasks
  /// </summary>
  /// <param name="taskIds">The list of task</param>
  /// <returns>returns a list of pair TaskId/TaskStatus</returns>
  public IEnumerable<Tuple<string, TaskStatus>> GetTaskStatus(IEnumerable<string> taskIds)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).GetTaskStatus(new GetTaskStatusRequest
                                                                                                                      {
                                                                                                                        TaskIds =
                                                                                                                        {
                                                                                                                          taskIds,
                                                                                                                        },
                                                                                                                      })
                                                                                                       .IdStatuses.Select(idsStatus => Tuple.Create(idsStatus.TaskId,
                                                                                                                                                    idsStatus.Status)));


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
