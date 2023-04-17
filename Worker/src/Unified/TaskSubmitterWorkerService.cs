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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Worker.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ArmoniK.DevelopmentKit.Worker.Unified;

/// <summary>
///   This is an abstract class that have to be implemented
///   by each class who needs tasks submission on worker side
///   See an example in the project ArmoniK.Samples in the sub project
///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/UnifiedAPI
///   ArmoniK.Samples.Worker/Services/ServiceApps.cs
/// </summary>
[PublicAPI]
[MarkDownDoc]
public abstract class TaskSubmitterWorkerService : ITaskSubmitterWorkerServiceConfiguration, ITaskOptionsConfiguration, ILoggerConfiguration
{
  /// <summary>
  /// </summary>
  public TaskSubmitterWorkerService()
  {
    Configuration = WorkerHelpers.GetDefaultConfiguration();
    Logger = WorkerHelpers.GetDefaultLoggerFactory(Configuration)
                          .CreateLogger(GetType()
                                          .Name);
  }

  /// <summary>
  /// </summary>
  /// <param name="loggerFactory">The factory logger to create logger</param>
  public TaskSubmitterWorkerService(ILoggerFactory loggerFactory)
  {
    LoggerFactory = loggerFactory;

    Logger = loggerFactory.CreateLogger(GetType()
                                          .Name);
  }

  /// <summary>
  ///   Get access to Logger with Logger.LoggingScope.
  /// </summary>
  public ILogger Logger { get; set; }

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
  ///   Get or Set Configuration
  /// </summary>
  public IConfiguration Configuration { get; set; }

  /// <summary>
  ///   The logger factory to create new Logger in sub class caller
  /// </summary>
  public ILoggerFactory LoggerFactory { get; set; }

  /// <summary>
  ///   The configure method is an internal call to prepare the ServiceContainer.
  ///   Its holds several configuration coming from the Client call
  /// </summary>
  /// <param name="configuration">The appSettings.json configuration prepared during the deployment</param>
  public void ConfigureLogger(IConfiguration configuration)
  {
    Configuration = configuration;

    var logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration)
                                          .WriteTo.Console(new CompactJsonFormatter())
                                          .Enrich.FromLogContext()
                                          .CreateLogger();
    LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(logger));
    Logger = LoggerFactory.CreateLogger(GetType()
                                          .Name);
    Logger.LogInformation("Configuring ServiceContainerBase");
  }

  /// <summary>
  ///   The configure method is an internal call to prepare the ServiceContainer.
  ///   Its holds TaskOptions coming from the Client call
  /// </summary>
  /// <param name="clientOptions">All data coming from Client within TaskOptions </param>
  public void ConfigureTaskOptions(TaskOptions clientOptions)
    => TaskOptions = clientOptions;

  /// <summary>
  ///   Provides the context for the task that is bound to the given service invocation
  /// </summary>
  public TaskContext TaskContext { get; set; }

  /// <summary>
  ///   Configure Service for actual session. Connect the worker to the current pollingAgent
  /// </summary>
  /// <param name="taskHandler">Low level object used for tasks submission by <see cref="SessionPollingService" /> </param>
  public void ConfigureSessionService(ITaskHandler taskHandler)
    => SessionService = new SessionPollingService(LoggerFactory,
                                                  taskHandler);

  /// <summary>
  ///   User method to submit task from the service
  /// </summary>
  /// <param name="payloads">
  ///   The user payload list to execute. Generally used for subTasking.
  /// </param>
  public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    => SessionService.SubmitTasks(payloads);


  /// <summary>
  ///   The method to submit several tasks with dependencies tasks. This task will wait
  ///   until all dependencies are completed successfully before starting
  /// </summary>
  /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
  /// <param name="resultForParent">Transmit the result to parent task if true</param>
  /// <returns>return a list of taskIds of the created tasks </returns>
  public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies,
                                                         bool                                      resultForParent = false)
    => SessionService.SubmitTasksWithDependencies(payloadWithDependencies,
                                                  resultForParent);


  /// <summary>
  ///   User method to submit task from the service
  /// </summary>
  /// <param name="payload">
  ///   The user payload to execute. Generally used for subtasking.
  /// </param>
  public string SubmitTask(byte[] payload)
    => SessionService.SubmitTasks(new[]
                                  {
                                    payload,
                                  })
                     .Single();


  /// <summary>
  ///   User method to submit task from the service
  /// </summary>
  /// <param name="methodName">
  ///   The name of the method you want to call
  ///   <remarks>It is case sensitive</remarks>
  /// </param>
  /// <param name="arguments">The arguments of the method to call </param>
  public string SubmitTask(string   methodName,
                           object[] arguments)
  {
    var protoSerializer = new ProtoSerializer();
    ArmonikPayload armonikPayload = new()
                                    {
                                      MethodName          = methodName,
                                      ClientPayload       = protoSerializer.SerializeMessageObjectArray(arguments),
                                      SerializedArguments = false,
                                    };
    return SessionService.SubmitTasks(new[]
                                      {
                                        armonikPayload.Serialize(),
                                      })
                         .Single();
  }


  /// <summary>
  ///   The method to submit One task with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payload">The payload to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <param name="resultForParent"></param>
  /// <returns>return the taskId of the created task </returns>
  public string SubmitTaskWithDependencie(byte[]        payload,
                                          IList<string> dependencies,
                                          bool          resultForParent = false)
    => SessionService.SubmitTasksWithDependencies(new[]
                                                  {
                                                    Tuple.Create(payload,
                                                                 dependencies),
                                                  },
                                                  resultForParent)
                     .Single();

  /// <summary>
  ///   The method to submit One task with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="methodName"></param>
  /// <param name="arguments">The arguments to submit</param>
  /// <param name="dependencies">A list of task Id in dependence of this created task</param>
  /// <param name="resultForParent"></param>
  /// <returns>return the taskId of the created task </returns>
  public string SubmitTaskWithDependencies(string        methodName,
                                           object[]      arguments,
                                           IList<string> dependencies,
                                           bool          resultForParent = false)
  {
    var protoSerializer = new ProtoSerializer();
    ArmonikPayload armonikPayload = new()
                                    {
                                      MethodName          = methodName,
                                      ClientPayload       = protoSerializer.SerializeMessageObjectArray(arguments),
                                      SerializedArguments = false,
                                    };
    return SessionService.SubmitTasksWithDependencies(new[]
                                                      {
                                                        Tuple.Create(armonikPayload.Serialize(),
                                                                     dependencies),
                                                      },
                                                      resultForParent)
                         .Single();
  }

  /// <summary>
  ///   Get Result from compute reply
  /// </summary>
  /// <param name="taskId">The task Id to get the result</param>
  /// <returns>return the customer payload</returns>
  public byte[] GetDependenciesResult(string taskId)
    => SessionService.GetDependenciesResult(taskId);

  /// <summary>
  ///   Prepare Session and create SessionService with the specific session
  /// </summary>
  /// <param name="sessionId">The ID of the current session</param>
  /// <param name="requestTaskOptions">The default <see cref="TaskOptions" /> used by tasks in the current session</param>
  public void ConfigureSession(Session     sessionId,
                               TaskOptions requestTaskOptions)
  {
    SessionId = sessionId;

    //Append or overwrite Dictionary Options in TaskOptions with one coming from client
    TaskOptions = requestTaskOptions;
  }
}
