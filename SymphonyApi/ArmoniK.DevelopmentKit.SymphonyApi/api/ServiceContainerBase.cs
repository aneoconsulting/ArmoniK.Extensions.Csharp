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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.Extensions.Common.StreamWrapper.Worker;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.SymphonyApi.api
{
  /// <summary>
  ///   The Class ServiceContainerBase (Old name was IServiceContainer) is an abstract class
  ///   that have to be implemented by each class wanted to be loaded as new Application
  ///   See an example in the project ArmoniK.Samples in the sub project
  ///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/SymphonyLike
  ///   Samples.ArmoniK.Sample.SymphonyPackages
  /// </summary>
  [PublicAPI]
  [MarkDownDoc]
  public abstract class ServiceContainerBase
  {
    /// <summary>
    ///   Get or Set SubSessionId object stored during the call of SubmitTask, SubmitSubTask,
    ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public Session SessionId { get; set; }

    /// <summary>
    /// Property to retrieve the sessionService previously created
    /// </summary>
    internal SessionPollingService SessionService { get; set; }

    //internal ITaskHandler TaskHandler { get; set; }

    /// <summary>
    /// Return TaskOption.Options coming from Client side
    /// 
    /// </summary>
    public IDictionary<string, string> ClientOptions { get; set; } = new Dictionary<string, string>();

    /// <summary>
    ///   Get or set the taskId (ONLY INTERNAL USED)
    /// </summary>
    public TaskId TaskId { get; set; }

    /// <summary>
    ///   Get or Set Configuration
    /// </summary>
    public IConfiguration Configuration { get; set; }

    /// <summary>
    /// The logger factory to create new Logger in sub class caller
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
    public abstract void OnCreateService(ServiceContext serviceContext);


    /// <summary>
    ///   This handler is executed once after the callback OnCreateService and before the OnInvoke
    /// </summary>
    /// <param name="sessionContext">
    ///   Holds all information on the state of the session at the start of the execution.
    /// </param>
    public abstract void OnSessionEnter(SessionContext sessionContext);


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
    public abstract byte[] OnInvoke(SessionContext sessionContext, TaskContext taskContext);


    /// <summary>
    ///   The middleware triggers the invocation of this handler to unbind the Service Instance from its owning Session.
    ///   This handler should do any cleanup for any resources that were used in the onSessionEnter() method.
    /// </summary>
    /// <param name="sessionContext">
    ///   Holds all information on the state of the session at the start of the execution such as session ID.
    /// </param>
    public abstract void OnSessionLeave(SessionContext sessionContext);


    /// <summary>
    ///   The middleware triggers the invocation of this handler just before a Service Instance is destroyed.
    ///   This handler should do any cleanup for any resources that were used in the onCreateService() method.
    /// </summary>
    /// <param name="serviceContext">
    ///   Holds all information on the state of the service at the start of the execution.
    /// </param>
    public abstract void OnDestroyService(ServiceContext serviceContext);

    /// <summary>
    ///   User method to submit task from the service
    /// </summary>
    /// <param name="payloads">
    ///   The user payload list to execute. Generally used for subTasking.
    /// </param>
    public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
      => SessionService.SubmitTasks(payloads);


    /// <summary>
    ///   User method to submit task from the service
    /// </summary>
    /// <param name="payloads">
    ///   The user payload list to execute. Generally used for subTasking.
    /// </param>
    /// <param name="parentTaskIds">The parent task Id attaching the subTask</param>
    [Obsolete]
    public IEnumerable<string> SubmitSubTasks(IEnumerable<byte[]> payloads, string parentTaskIds)
      => SessionService.SubmitSubTasks(parentTaskIds,
                                       payloads);

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <param name="resultForParent">Up result to parent task</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies, bool resultForParent = false)
      => SessionService.SubmitTasksWithDependencies(payloadWithDependencies,
                                                    resultForParent);

    /// <summary>
    ///   The method to submit one subtask with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created SubTask</param>
    /// <returns>return the taskId of the created SubTask </returns>
    [Obsolete]
    public string SubmitSubtaskWithDependencies(byte[] payload, IList<string> dependencies)
      => SubmitSubtasksWithDependencies(new[]
      {
        Tuple.Create(payload,
                     dependencies),
      }).Single();

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created Subtasks</param>
    /// <returns>return a list of taskIds of the created subtasks </returns>
    [Obsolete]
    public IEnumerable<string> SubmitSubtasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
      => SessionService.SubmitTasksWithDependencies(payloadWithDependencies);

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
    /// 
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
    /// 
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
    {
      return SessionService.GetDependenciesResult(taskId);
    }

    /// <summary>
    ///   The configure method is an internal call to prepare the ServiceContainer.
    ///   Its holds several configuration coming from the Client call
    /// </summary>
    /// <param name="configuration">The appSettings.json configuration prepared during the deployment</param>
    /// <param name="clientOptions">All data coming from Client within TaskOptions.Options </param>
    public void Configure(IConfiguration configuration, IReadOnlyDictionary<string, string> clientOptions)
    {
      Configuration = configuration;

      //Append or overwrite Dictionary Options in TaskOptions with one coming from client
      clientOptions.ToList()
                   .ForEach(pair => ClientOptions[pair.Key] = pair.Value);

      var logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration)
                                            .WriteTo.Console(new CompactJsonFormatter())
                                            .Enrich.FromLogContext()
                                            .CreateLogger();
      LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(logger));
      Logger        = LoggerFactory.CreateLogger<ServiceContainerBase>();
      Logger.LogInformation("Configuring ServiceContainerBase");
    }

    /// <summary>
    /// Prepare Session and create SessionService with the specific session
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="requestTaskOptions"></param>
    public void ConfigureSession(Session sessionId, IReadOnlyDictionary<string, string> requestTaskOptions)
    {
      SessionId = sessionId;

      //Append or overwrite Dictionary Options in TaskOptions with one coming from client
      requestTaskOptions.ToList()
                        .ForEach(pair => ClientOptions[pair.Key] = pair.Value);
    }

    /// <summary>
    /// Configure Service for actual session. Connect the worker to the current pollingAgent
    /// </summary>
    /// <param name="taskHandler"></param>
    public void ConfigureSessionService(ITaskHandler taskHandler)
    {
      SessionService = new SessionPollingService(LoggerFactory, taskHandler);
    }

    /// <summary>ginScope
    /// Get access to Logger with Logger.Lo.
    /// </summary>
    public ILogger<ServiceContainerBase> Logger { get; set; }
  }

  /// <summary>
  ///   This is the ServiceContainerBase extensions used to extend SubmitSubTasks
  /// </summary>
  public static class ServiceContainerBaseExt
  {
    /// <summary>
    ///   User method to submit task from the service
    /// </summary>
    /// <param name="serviceContainerBase"></param>
    /// <param name="payload">
    ///   The user payload to execute. Generally used for subtasking.
    /// </param>
    public static string SubmitTask(this ServiceContainerBase serviceContainerBase, byte[] payload)
    {
      return serviceContainerBase.SessionService.SubmitTasks(new[] { payload }).Single();
    }

    /// <summary>
    ///   User method to submit task from the service
    /// </summary>
    /// <param name="serviceContainerBase"></param>
    /// <param name="payload">
    ///   The user payload to execute. Generally used for subtasking.
    /// </param>
    /// <param name="parentId">With one Parent task Id</param>
    [Obsolete]
    public static string SubmitSubTask(this ServiceContainerBase serviceContainerBase, byte[] payload, string parentId)
    {
      return serviceContainerBase.SubmitSubTasks(new[] { payload },
                                                 parentId).Single();
    }

    /// <summary>
    ///   The method to submit One task with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="serviceContainerBase"></param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created task</param>
    /// <param name="resultForParent"></param>
    /// <returns>return the taskId of the created task </returns>
    public static string SubmitTaskWithDependencies(this ServiceContainerBase serviceContainerBase, byte[] payload, IList<string> dependencies, bool resultForParent = false)
    {
      return serviceContainerBase.SubmitTasksWithDependencies(new[]
      {
        Tuple.Create(payload,
                     dependencies),
      }, resultForParent).Single();
    }


    private static void SubmitDelegateTaskWithDependencies(this ServiceContainerBase                     serviceContainerBase,
                                                           IEnumerable<string>                           taskIds,
                                                           Func<IEnumerable<Tuple<string, byte[]>>, int> func)
    {
      throw new NotImplementedException();
    }
  }
}