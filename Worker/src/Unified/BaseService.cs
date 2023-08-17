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
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Worker.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

namespace ArmoniK.DevelopmentKit.Worker.Unified;

/// <summary>
///   The Class ServiceContainerBase (Old name was IServiceContainer) is an abstract class
///   that have to be implemented by each class wanted to be loaded as new Application
///   See an example in the project ArmoniK.Samples in the sub project
///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/SymphonyLike
///   Samples.ArmoniK.Sample.SymphonyPackages
/// </summary>
[PublicAPI]
[MarkDownDoc]
public abstract class BaseService<T>
{
  /// <summary>
  /// </summary>
  public BaseService()
  {
    Configuration = WorkerHelpers.GetDefaultConfiguration();
    LoggerFactory = WorkerHelpers.GetDefaultLoggerFactory(Configuration);
    Logger        = LoggerFactory.CreateLogger<T>();
  }

  /// <summary>
  /// </summary>
  /// <param name="loggerFactory"></param>
  public BaseService(ILoggerFactory loggerFactory)
  {
    LoggerFactory = loggerFactory;

    Logger = loggerFactory.CreateLogger<T>();
  }

  /// <summary>
  ///   ginScope
  ///   Get access to Logger with Logger.Lo.
  /// </summary>
  public ILogger<T> Logger { get; set; }

  /// <summary>
  ///   Get or Set SubSessionId object stored during the call of SubmitTask, SubmitSubTask,
  ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
  /// </summary>
  public Session SessionId { get; set; }

  /// <summary>
  ///   Property to retrieve the sessionService previously created
  /// </summary>
  internal SessionPollingService SessionService { get; set; }

  /// <summary>
  ///   Map between ids of task and their results id after task submission
  /// </summary>
  public Dictionary<string, string> TaskId2OutputId
    => SessionService.TaskId2OutputId;

  //internal ITaskHandler TaskHandler { get; set; }

  /// <summary>
  ///   Return TaskOption coming from Client side
  /// </summary>
  public TaskOptions TaskOptions { get; set; } = new();

  /// <summary>
  ///   Get or set the taskId (ONLY INTERNAL USED)
  /// </summary>
  public TaskId TaskId { get; set; }

  /// <summary>
  ///   Get or Set Configuration
  /// </summary>
  public IConfiguration Configuration { get; set; }

  /// <summary>
  ///   The logger factory to create new Logger in sub class caller
  /// </summary>
  public ILoggerFactory LoggerFactory { get; set; }

  /// <summary>
  ///   The middleware triggers the invocation of this handler just after a Service Instance is started.
  ///   The application developer must put any service initialization into this handler.
  ///   Default implementation does nothing.
  /// </summary>
  /// <param name="serviceContext">
  ///   Holds all information on the state of the service at the start of the execution.
  /// </param>
  public void OnCreateService(ServiceContext serviceContext)
  {
  }


  /// <summary>
  ///   This handler is executed once after the callback OnCreateService and before the OnInvoke
  /// </summary>
  /// <param name="sessionContext">
  ///   Holds all information on the state of the session at the start of the execution.
  /// </param>
  public void OnSessionEnter(SessionContext sessionContext)
  {
  }


  /// <summary>
  ///   The middleware triggers the invocation of this handler every time a task input is
  ///   sent to the service to be processed.
  ///   The actual service logic should be implemented in this method. This is the only
  ///   method that is mandatory for the application developer to implement.
  /// </summary>
  /// <param name="sessionContext">
  ///   Holds all information on the state of the session at the start of the execution such as session ID.
  /// </param>
  /// <param name="taskContext">
  ///   Holds all information on the state of the task such as the task ID and the payload.
  /// </param>
  public byte[] OnInvoke(SessionContext sessionContext,
                         TaskContext    taskContext)
    => throw new NotImplementedException("BaseService call OnInvoke won't be implemented");


  /// <summary>
  ///   The middleware triggers the invocation of this handler to unbind the Service Instance from its owning Session.
  ///   This handler should do any cleanup for any resources that were used in the onSessionEnter() method.
  /// </summary>
  /// <param name="sessionContext">
  ///   Holds all information on the state of the session at the start of the execution such as session ID.
  /// </param>
  public void OnSessionLeave(SessionContext sessionContext)
  {
  }


  /// <summary>
  ///   The middleware triggers the invocation of this handler just before a Service Instance is destroyed.
  ///   This handler should do any cleanup for any resources that were used in the onCreateService() method.
  /// </summary>
  /// <param name="serviceContext">
  ///   Holds all information on the state of the service at the start of the execution.
  /// </param>
  public void OnDestroyService(ServiceContext serviceContext)
  {
  }

  /// <summary>
  ///   User method to submit task from the service
  /// </summary>
  /// <param name="payloads">
  ///   The user payload list to execute. Generally used for subTasking.
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads,
                                         int                 maxRetries  = 5,
                                         TaskOptions?        taskOptions = null)
    => SessionService.SubmitTasks(payloads,
                                  maxRetries,
                                  taskOptions);


  /// <summary>
  ///   The method to submit several tasks with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
  /// <param name="resultForParent">Up result to parent task</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>return a list of taskIds of the created tasks </returns>
  public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies,
                                                         bool                                      resultForParent = false,
                                                         int                                       maxRetries      = 5,
                                                         TaskOptions                               taskOptions     = null)
    => SessionService.SubmitTasksWithDependencies(payloadWithDependencies,
                                                  resultForParent,
                                                  maxRetries,
                                                  taskOptions);

  /// <summary>
  ///   User method to wait for only the parent task from the client
  /// </summary>
  /// <param name="taskId">
  ///   The task id of the task to wait for
  /// </param>
  [Obsolete]
  public void WaitForTaskCompletion(string taskId)
  {
  }

  /// <summary>
  /// </summary>
  /// <param name="taskIds">List of tasks to wait for</param>
  [Obsolete]
  public void WaitForTasksCompletion(IEnumerable<string> taskIds)
  {
  }

  /// <summary>
  ///   User method to wait for SubTasks from the client
  /// </summary>
  /// <param name="taskId">
  ///   The task id of the Subtask
  /// </param>
  [Obsolete]
  public void WaitForSubTasksCompletion(string taskId)
  {
  }

  /// <summary>
  ///   Get Result from compute reply
  /// </summary>
  /// <param name="taskId">The task Id to get the result</param>
  /// <returns>return the customer payload</returns>
  public byte[] GetDependenciesResult(string taskId)
    => SessionService.GetDependenciesResult(taskId);

  /// <summary>
  ///   The configure method is an internal call to prepare the ServiceContainer.
  ///   Its holds several configuration coming from the Client call
  /// </summary>
  /// <param name="configuration">The appSettings.json configuration prepared during the deployment</param>
  /// <param name="clientOptions">All data coming from Client within TaskOptions </param>
  public void Configure(IConfiguration configuration,
                        TaskOptions    clientOptions)
  {
    Configuration = configuration;

    var logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration)
                                          .WriteTo.Console(new CompactJsonFormatter())
                                          .Enrich.FromLogContext()
                                          .CreateLogger();
    LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(logger));
    Logger        = LoggerFactory.CreateLogger<T>();
    Logger.LogInformation("Configuring ServiceContainerBase");
  }

  /// <summary>
  ///   Prepare Session and create SessionService with the specific session
  /// </summary>
  /// <param name="sessionId"></param>
  /// <param name="requestTaskOptions"></param>
  public void ConfigureSession(Session     sessionId,
                               TaskOptions requestTaskOptions)
  {
    SessionId = sessionId;

    //Append or overwrite Dictionary Options in TaskOptions with one coming from client
    TaskOptions = requestTaskOptions;
  }

  /// <summary>
  ///   Configure Service for actual session. Connect the worker to the current pollingAgent
  /// </summary>
  /// <param name="taskHandler"></param>
  public void ConfigureSessionService(ITaskHandler taskHandler)
    => SessionService = new SessionPollingService(LoggerFactory,
                                                  taskHandler);
}

/// <summary>
///   This is the ServiceContainerBase extensions used to extend SubmitSubTasks
/// </summary>
public static class BaseServiceExt
{
  /// <summary>
  ///   User method to submit task from the service
  /// </summary>
  /// <param name="serviceContainerBase"></param>
  /// <param name="payload">
  ///   The user payload to execute. Generally used for subtasking.
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  public static string SubmitTask<T>(this BaseService<T> serviceContainerBase,
                                     byte[]              payload,
                                     int                 maxRetries  = 5,
                                     TaskOptions         taskOptions = null)
    => serviceContainerBase.SessionService.SubmitTasks(new[]
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
  /// <param name="serviceContainerBase"></param>
  /// <param name="payload">The payload to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <param name="resultForParent"></param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>return the taskId of the created task </returns>
  public static string SubmitTaskWithDependencies<T>(this BaseService<T> serviceContainerBase,
                                                     byte[]              payload,
                                                     IList<string>       dependencies,
                                                     bool                resultForParent = false,
                                                     int                 maxRetries      = 5,
                                                     TaskOptions         taskOptions     = null)
    => serviceContainerBase.SubmitTasksWithDependencies(new[]
                                                        {
                                                          Tuple.Create(payload,
                                                                       dependencies),
                                                        },
                                                        resultForParent,
                                                        maxRetries,
                                                        taskOptions)
                           .Single();


  private static void SubmitDelegateTaskWithDependencies<T>(this BaseService<T>                           serviceContainerBase,
                                                            IEnumerable<string>                           taskIds,
                                                            Func<IEnumerable<Tuple<string, byte[]>>, int> func)
    => throw new NotImplementedException();
}
