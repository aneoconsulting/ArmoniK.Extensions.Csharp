using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace ArmoniK.DevelopmentKit.SymphonyApi
{
    public abstract class IServiceContainer
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
        public abstract void OnInvoke(SessionContext sessionContext, TaskContext taskContext);



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
        /// User call to insert customer result data from task (Server side)
        /// </summary>
        /// <param name="key">
        /// The user key that can be retrieved later from client side.
        /// </param>
        /// <param name="value">
        /// The data value to put in the database.
        /// </param>
        public void writeTaskOutput(string key, byte[] value)
        {
            ClientService.StoreData(key, value);
        }

        /// <summary>
        /// User call to get customer data from task (Server side)
        /// </summary>
        /// <param name="key">
        /// The user key that can be retrieved later from client side.
        /// </param>
        /// <param name="value">
        /// The data value to put in the database.
        /// </param>
        public byte[] GetData(string key)
        {
            return ClientService.GetData(key);
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
            return ClientService.SubmitTasks(payloads);
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
        /// <param name="parentsIds">
        /// Parent Task ids
        /// </param>
        public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads, IEnumerable<string> parentsIds)
        {
          return ClientService.SubmitTasks(payloads, parentsIds);
        }

        public string SubmitTaskWithDependencies(string session, byte[] payload, IList<string> dependencies)
        {
            return ClientService.SubmitTaskWithDependencies(session,
                                              new[]
                                              {
                                                Tuple.Create(payload,
                                                             dependencies),
                                              }).Single();
        }

        public IEnumerable<string> SubmitTaskWithDependencies(string session, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
        {
          return ClientService.SubmitTaskWithDependencies(session,
                                                          payloadWithDependencies);
        }


        public string SubmitSubtaskWithDependencies(string session, string parentId, byte[] payload, IList<string> dependencies)
        {
            return ClientService.SubmitSubtaskWithDependencies(session,
                                                               parentId,
                                                               new[]
                                                               {
                                                                 Tuple.Create(payload,
                                                                              dependencies),
                                                               }).Single();
        }

        public IEnumerable<string> SubmitSubtaskWithDependencies(string session, string parentId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
        {
          return ClientService.SubmitSubtaskWithDependencies(session,
                                                             parentId,
                                                             payloadWithDependencies);
        }

        /// <summary>
        /// User method to wait for tasks from the client
        /// </summary>
        /// <param name="taskID">
        /// The task id of the task
        /// </param>
        public void WaitCompletion(string taskId)
        {
            ClientService.WaitCompletion(taskId);
        }

        public ArmonikSymphonyClient ClientService { get; set; }
    }

    public static class ServiceContainerExt
    {
        /// <summary>
        /// User method to submit task from the service
        /// </summary>
        /// <param name="sessionId">
        /// The session id to attach the new task.
        /// </param>
        /// <param name="payload">
        /// The user payload to execute. Generaly used for subtasking.
        /// </param>
        public static string SubmitTask(this IServiceContainer serviceContainer, byte[] payload)
        {
            return serviceContainer.SubmitTasks(new[] { payload })
                                   .Single();
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
        public static string SubmitSubTask(this IServiceContainer serviceContainer, byte[] payload, string parentId)
        {
          return serviceContainer.SubmitTasks(new[] { payload }, new [] { parentId })
                                 .Single();
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
        /// <param name="parentIds">With Multiple Parents task Ids</param>
        public static string SubmitSubTask(this IServiceContainer serviceContainer, byte[] payload, IEnumerable<string> parentIds)
        {
          return serviceContainer.SubmitTasks(new[] { payload }, parentIds)
                                 .Single();
        }

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
        public static IEnumerable<string> SubmitSubTasks(this IServiceContainer serviceContainer, IEnumerable<byte[]> payload, string parentId)
        {
          return serviceContainer.SubmitTasks( payload, new [] { parentId });
        }
    }
}