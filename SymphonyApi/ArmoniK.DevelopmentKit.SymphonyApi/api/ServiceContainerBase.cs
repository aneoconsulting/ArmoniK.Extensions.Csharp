// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

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
    public SessionId SessionId { get; set; }


    internal ArmonikSymphonyClient ClientService { get; set; }

    /// <summary>
    ///   Get or set the taskId (ONLY INTERNAL USED)
    /// </summary>
    public string TaskId { get; set; }

    /// <summary>
    ///   Get or Set Configuration
    /// </summary>
    public IConfiguration Configuration { get; set; }

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
      => ClientService.SubmitSubTasks(SessionId.PackSessionId(),
                                      TaskId,
                                      payloads);


    /// <summary>
    ///   User method to submit task from the service
    /// </summary>
    /// <param name="payloads">
    ///   The user payload list to execute. Generally used for subTasking.
    /// </param>
    /// <param name="parentTaskIds">The parent task Id attaching the subTask</param>
    [Obsolete]
    public IEnumerable<string> SubmitSubTasks(IEnumerable<byte[]> payloads, string parentTaskIds)
      => ClientService.SubmitSubTasks(SessionId.PackSessionId(),
                                      parentTaskIds,
                                      payloads);

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
      => ClientService.SubmitSubtasksWithDependencies(SessionId.PackSessionId(),
                                                      TaskId,
                                                      payloadWithDependencies);

    /// <summary>
    ///   The method to submit one subtask with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="parentId">The parent Task who want to create the SubTask</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created SubTask</param>
    /// <returns>return the taskId of the created SubTask </returns>
    [Obsolete]
    public string SubmitSubtaskWithDependencies(string parentId, byte[] payload, IList<string> dependencies)
      => SubmitSubtasksWithDependencies(parentId,
                                        new[]
                                        {
                                          Tuple.Create(payload,
                                                       dependencies),
                                        }).Single();

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created Subtasks</param>
    /// <returns>return a list of taskIds of the created subtasks </returns>
    [Obsolete]
    public IEnumerable<string> SubmitSubtasksWithDependencies(string parentId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
      => ClientService.SubmitSubtasksWithDependencies(SessionId.PackSessionId(),
                                                      parentId,
                                                      payloadWithDependencies);

    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskId">
    ///   The task id of the task to wait for
    /// </param>
    public void WaitForCompletion(string taskId)
    {
      ClientService.OpenSession(SessionId);
      ClientService.WaitCompletion(taskId);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="taskIds"></param>
    public void WaitListCompletion(IEnumerable<string> taskIds)
    {
      ClientService.OpenSession(SessionId);
      ClientService.WaitListCompletion(taskIds);
    }

    /// <summary>
    ///   User method to wait for SubTasks from the client
    /// </summary>
    /// <param name="taskId">
    ///   The task id of the Subtask
    /// </param>
    public void WaitForSubTasksCompletion(string taskId)
    {
      ClientService.OpenSession(SessionId);
      ClientService.WaitSubtasksCompletion(taskId);
    }

    /// <summary>
    ///   Get Result from compute reply
    /// </summary>
    /// <param name="taskId">The task Id to get the result</param>
    /// <returns>return the customer payload</returns>
    public byte[] GetResult(string taskId)
    {
      ClientService.OpenSession(SessionId);

      return ClientService.GetResult(taskId);
    }

    /// <summary>
    ///   The configure method is an internal call to prepare the ServiceContainer.
    ///   Its holds several configuration coming from the Client call
    /// </summary>
    /// <param name="configuration">The appSettings.json configuration prepared during the deployment</param>
    /// <param name="clientOptions">All data coming from Client within TaskOptions.Options </param>
    public void Configure(IConfiguration configuration, IDictionary<string, string> clientOptions)
    {
      Configuration = configuration;

      var factory = new LoggerFactory(new[]
      {
        new SerilogLoggerProvider(new LoggerConfiguration()
                                  .ReadFrom
                                  .Configuration(Configuration)
                                  .CreateLogger())
      });

      ClientService = new(configuration,
                          factory);

      //Append or overwrite Dictionary Options in TaskOptions with one coming from client
      clientOptions.ToList()
                   .ForEach(pair => ClientService.TaskOptions.Options[pair.Key] = pair.Value);


      Log = factory.CreateLogger<ServiceContainerBase>();
      Log.LogInformation("Configuring ServiceContainerBase");
    }

    /// <summary>
    /// Get access to Logger with Log.Lo.
    /// </summary>
    public ILogger<ServiceContainerBase> Log { get; set; }
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
      return serviceContainerBase.ClientService.SubmitSubTasks(serviceContainerBase.SessionId.PackSessionId(),
                                                               serviceContainerBase.TaskId,
                                                               new[] { payload }).Single();
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
    /// <returns>return the taskId of the created task </returns>
    public static string SubmitTaskWithDependencies(this ServiceContainerBase serviceContainerBase, byte[] payload, IList<string> dependencies)
    {
      return serviceContainerBase.SubmitTasksWithDependencies(new[]
      {
        Tuple.Create(payload,
                     dependencies),
      }).Single();
    }
  }
}