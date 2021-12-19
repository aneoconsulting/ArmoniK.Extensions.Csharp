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
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.SymphonyApi
{
  public abstract class ServiceContainerBase
  {
    public SessionId SessionId { get; set; }

    /// <summary>
    /// The middleware triggers the invocation of this handler just after a Service Instance is started.
    /// The application developer must put any service initialization into this handler. Default implementation does nothing.
    /// </summary>
    /// <param name="serviceContext">
    /// Holds all information on the state of the service at the start of the execution.
    /// </param>
    public abstract void OnCreateService(ServiceContext serviceContext);


    /// <summary>
    /// This handler is executed once after the callback OnCreateService and before the OnInvoke
    /// </summary>
    /// <param name="sessionContext">
    /// Holds all information on the state of the session at the start of the execution.
    /// </param>
    public abstract void OnSessionEnter(SessionContext sessionContext);


    /// <summary>
    /// The middleware triggers the invocation of this handler every time a task input is sent to the service to be processed.
    /// The actual service logic should be implemented in this method. This is the only method that is mandatory for the application developer to implement.
    /// </summary>
    /// <param name="sessionContext">
    /// Holds all information on the state of the session at the start of the execution such as session ID.
    /// </param>
    /// <param name="taskContext">
    /// Holds all information on the state of the task such as the task ID and the paykload.
    /// </param>
    public abstract byte[] OnInvoke(SessionContext sessionContext, TaskContext taskContext);


    /// <summary>
    /// The middleware triggers the invocation of this handler to unbind the Service Instance from its owning Session.
    /// This handler should do any cleanup for any resources that were used in the onSessionEnter() method.
    /// </summary>
    /// <param name="sessionContext">
    /// Holds all information on the state of the session at the start of the execution such as session ID.
    /// </param>
    public abstract void OnSessionLeave(SessionContext sessionContext);


    /// <summary>
    /// The middleware triggers the invocation of this handler just before a Service Instance is destroyed.
    /// This handler should do any cleanup for any resources that were used in the onCreateService() method.
    /// </summary>
    /// <param name="serviceContext">
    /// Holds all information on the state of the service at the start of the execution.
    /// </param>
    public abstract void OnDestroyService(ServiceContext serviceContext);

    /// <summary>
    /// User method to submit task from the service
    /// </summary>
    /// <param name="sessionId">
    /// The session id to attach the new task.
    /// </param>
    /// <param name="payload">
    /// The user payload to execute. Generaly used for subtasking.
    /// </param>
    public string SubmitTask(byte[] payload)
    {
      return ClientService.SubmitSubTasks(SessionId.PackSessionId(),
                                          TaskId,
                                          new[] { payload }
                          )
                          .Single();
    }

    /// <summary>
    /// User method to submit task from the service
    /// </summary>
    /// <param name="sessionId">
    /// The session id to attach the new task.
    /// </param>
    /// <param name="payloads">
    /// The user payload list to execute. Generaly used for subtasking.
    /// </param>
    public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    {
      return ClientService.SubmitSubTasks(SessionId.PackSessionId(),
                                          TaskId,
                                          payloads);
    }

    /// <summary>
    /// User method to submit task from the service
    /// </summary>
    /// <param name="sessionId">
    /// The session id to attach the new task.
    /// </param>
    /// <param name="payload">
    /// The user payload to execute. Generaly used for subtasking.
    /// </param>
    /// <param name="parentId">With one Parent task Id</param>
    public string SubmitSubTask(byte[] payload, string parentId)
    {
      return ClientService.SubmitSubTasks(SessionId.PackSessionId(),
                                          parentId,
                                          new[] { payload }).Single();
    }

    /// <summary>
    /// User method to submit task from the service
    /// </summary>
    /// <param name="sessionId">
    /// The session id to attach the new task.
    /// </param>
    /// <param name="payloads">
    /// The user payload list to execute. Generaly used for subtasking.
    /// </param>
    public IEnumerable<string> SubmitSubTasks(IEnumerable<byte[]> payloads, string parentTaskIds)
    {
      return ClientService.SubmitSubTasks(SessionId.PackSessionId(),
                                          parentTaskIds,
                                          payloads);
    }

    public string SubmitTaskWithDependencies(byte[] payload, IList<string> dependencies)
    {
      return ClientService.SubmitSubtasksWithDependencies(SessionId.PackSessionId(),
                                                          TaskId,
                                                          new[]
                                                          {
                                                            Tuple.Create(payload,
                                                                         dependencies),
                                                          }).Single();
    }

    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      return ClientService.SubmitSubtasksWithDependencies(SessionId.PackSessionId(),
                                                          TaskId,
                                                          payloadWithDependencies);
    }


    public string SubmitSubtaskWithDependencies(string parentId, byte[] payload, IList<string> dependencies)
    {
      return SubmitSubtasksWithDependencies(parentId,
                                            new[]
                                            {
                                              Tuple.Create(payload,
                                                           dependencies),
                                            }).Single();
    }

    public IEnumerable<string> SubmitSubtasksWithDependencies(string parentId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      return ClientService.SubmitSubtasksWithDependencies(SessionId.PackSessionId(),
                                                          parentId,
                                                          payloadWithDependencies);
    }

    /// <summary>
    /// User method to wait for tasks from the client
    /// </summary>
    /// <param name="taskID">
    /// The task id of the task
    /// </param>
    public void WaitForCompletion(string taskId)
    {
      ClientService.OpenSession(SessionId);
      ClientService.WaitCompletion(taskId);
    }

    /// <summary>
    /// User method to wait for tasks from the client
    /// </summary>
    /// <param name="taskID">
    /// The task id of the task
    /// </param>
    public void WaitForSubTasksCompletion(string taskId)
    {
      ClientService.OpenSession(SessionId);
      ClientService.WaitSubtasksCompletion(taskId);
    }

    /// <summary>
        /// Get Result from compute reply
        /// </summary>
        /// <param name="taskId">The task Id to get the result</param>
        /// <returns>return the customer payload</returns>
    public byte[] GetResult(string taskId)
    {
      ClientService.OpenSession(SessionId);

      return ClientService.GetResult(taskId);
    }


    private ArmonikSymphonyClient ClientService { get; set; }
    public string TaskId { get; set; }

    public void Configure(IConfiguration configuration)
    {
      Configuration = configuration;
      ClientService = new ArmonikSymphonyClient(configuration);
    }

    public IConfiguration Configuration { get; set; }
  }

  public static class ServiceContainerBaseExt
  {
    /// <summary>
    /// User method to submit task from the service
    /// </summary>
    /// <param name="sessionId">
    /// The session id to attach the new task.
    /// </param>
    /// <param name="serviceContainer"></param>
    /// <param name="payload">
    /// The user payload to execute. Generaly used for subtasking.
    /// </param>
    /// <param name="parentId">With one parent task Id</param>
    public static IEnumerable<string> SubmitSubTasks(this ServiceContainerBase serviceContainerBase, IEnumerable<byte[]> payload, string parentId)
    {
      return serviceContainerBase.SubmitSubTasks(payload,
                                             parentId);
    }
  }
}