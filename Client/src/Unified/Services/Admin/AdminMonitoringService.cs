using System;
using System.Collections.Generic;
using System.Linq;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
  /// <param name="channelPool">The entry point to the control plane</param>
  /// <param name="loggerFactory">The factory logger to create logger</param>
  public AdminMonitoringService(ChannelPool     channelPool,
                                ILoggerFactory? loggerFactory = null)
  {
    Logger       = loggerFactory?.CreateLogger<AdminMonitoringService>() ?? NullLogger<AdminMonitoringService>.Instance;
    channelPool_ = channelPool;
  }

  private ILogger Logger { get; }


  /// <summary>
  ///   Returns the service configuration
  /// </summary>
  [PublicAPI]
  public void GetServiceConfiguration()
  {
    var configuration = channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).GetServiceConfiguration(new Empty()));

    Logger.LogInformation($"This configuration will be update in the nex version [ {configuration} ]");
  }

  /// <summary>
  ///   This method can mark the session in status Cancelled and
  ///   mark all tasks in cancel status
  /// </summary>
  /// <param name="sessionId">the sessionId of the session to cancel</param>
  [PublicAPI]
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
  [PublicAPI]
  public IEnumerable<string> ListTasks(TaskFilter taskFilter)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListTasks(taskFilter)
                                                                                                       .TaskIds);

  /// <summary>
  ///   Return the whole list of task of a session
  /// </summary>
  /// <returns>The list of filtered task </returns>
  [PublicAPI]
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
  [PublicAPI]
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
  [PublicAPI]
  public IEnumerable<string> ListAllSessions()
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListSessions(new SessionFilter())
                                                                                                       .SessionIds);

  /// <summary>
  ///   The method is to get a filtered list of session
  /// </summary>
  /// <param name="sessionFilter">The filter to apply on the request</param>
  /// <returns>returns a list of session filtered</returns>
  [PublicAPI]
  public IEnumerable<string> ListSessions(SessionFilter sessionFilter)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).ListSessions(sessionFilter)
                                                                                                       .SessionIds);

  /// <summary>
  ///   The method is to get a filtered list of running session
  /// </summary>
  /// <returns>returns a list of session filtered</returns>
  [PublicAPI]
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
  [PublicAPI]
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
  [PublicAPI]
  public int CountTasks(TaskFilter taskFilter)
    => channelPool_.WithChannel(channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel).CountTasks(taskFilter)
                                                                                                       .Values.Count);

  /// <summary>
  ///   The method is to get the number of all task in a session
  /// </summary>
  /// <returns>return the number of task</returns>
  [PublicAPI]
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
  [PublicAPI]
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
  [PublicAPI]
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
  [PublicAPI]
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
  [PublicAPI]
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
  [PublicAPI]
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
}
