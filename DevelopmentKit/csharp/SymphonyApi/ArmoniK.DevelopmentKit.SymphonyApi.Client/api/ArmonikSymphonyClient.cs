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

using ArmoniK.Core.gRPC.V1;

using Google.Protobuf;

using Grpc.Net.Client;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client
{
  /// <summary>
  /// The main object to communicate with the control Plane from the client side
  /// The class will connect to the control plane to createSession, SubmitTask,
  /// Wait for result and get the result.
  /// See an example in the project ArmoniK.Samples in the sub project
  /// https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/SymphonyLike
  /// Samples.ArmoniK.Sample.SymphonyClient
  /// </summary>
  public class ArmonikSymphonyClient
  {
    private readonly  IConfiguration                 configuration_;
    private readonly  IConfigurationSection          controlPlanAddress_;
    internal readonly ILogger<ArmonikSymphonyClient> logger_;
    private ClientService.ClientServiceClient ControlPlaneService { get; set; }

    /// <summary>
    /// Returns the section key Grpc from appSettings.json
    /// </summary>
    public string SectionControlPlan { get; set; } = "Grpc";

    /// <summary>
    /// Set or Get TaskOptions with inside MaxDuration, Priority, AppName, VersionName and AppNamespace
    /// </summary>
    public TaskOptions TaskOptions { get; set; }

    /// <summary>
    /// Only used for internal DO NOT USED IT
    /// Get or Set SessionId object stored during the call of SubmitTask, SubmitSubTask,
    /// SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public SessionId SessionId { get; private set; }

    /// <summary>
    /// Only used for internal DO NOT USED IT
    /// Get or Set SubSessionId object stored during the call of SubmitTask, SubmitSubTask,
    /// SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public SessionId SubSessionId { get; set; }

    public ArmonikSymphonyClient(IConfiguration configuration, TaskOptions taskOptions = null)
    {
      configuration_      = configuration;
      controlPlanAddress_ = configuration_.GetSection(SectionControlPlan);

      Log.Logger = new LoggerConfiguration()
                   .MinimumLevel.Override("Microsoft",
                                          LogEventLevel.Information)
                   .Enrich.FromLogContext()
                   .WriteTo.Console()
                   .CreateBootstrapLogger();

      var factory = new LoggerFactory().AddSerilog();

      logger_ = factory.CreateLogger<ArmonikSymphonyClient>();

      taskOptions ??= InitializeDefaultTaskOptions();
      TaskOptions =   taskOptions;
    }

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="taskOptions">Optional parameter to set TaskOptions during the Session creation</param>
    /// <returns></returns>
    public string CreateSession(TaskOptions taskOptions = null)
    {
      if (taskOptions != null) TaskOptions = taskOptions;

      ControlPlaneConnection();

      var sessionOptions = new SessionOptions
      {
        DefaultTaskOption = TaskOptions,
      };
      logger_.LogDebug($"Creating Session... ");

      SessionId = ControlPlaneService.CreateSession(sessionOptions);
      logger_.LogDebug($"Session Created {SessionId.PackSessionId()}");


      return SessionId.PackSessionId();
    }

    private void ControlPlaneConnection()
    {
      var channel = GrpcChannel.ForAddress(controlPlanAddress_["Endpoint"]);
      ControlPlaneService ??= new(channel);
    }

    /// <summary>
    /// Create the SubSession to submit task
    /// </summary>
    /// <param name="parentSession"></param>
    /// <param name="sessionOptions"></param>
    /// <returns></returns>
    private void CreateSubSession(string parentId)
    {
      logger_.LogDebug($"Creating SubSession from Session : {SessionId.PackSessionId()}");
      lock (this)
      {
        ControlPlaneConnection();
        if (SubSessionId is null)
        {
          string taskId = parentId.CanUnPackTaskId() ? parentId.UnPackTaskId().Task : parentId;
          var sessionOptions = new SessionOptions
          {
            DefaultTaskOption = TaskOptions,
            ParentTask = new()
                         {
              Session    = SessionId.Session,
              SubSession = SessionId.SubSession,
              Task       = taskId
            },
          };
          logger_.LogDebug($"Creating SubSession from Session :   {SessionId?.PackSessionId()}");
          SubSessionId = ControlPlaneService.CreateSession(sessionOptions);
          logger_.LogDebug(
            $"Created  SubSession from Session {SessionId?.PackSessionId()}" +
            $" new SubSession {SubSessionId?.PackSessionId()} with ParentTask {sessionOptions.ParentTask.PackTaskId()}");
        }
      }
    }

    /// <summary>
    /// Set connection to an already opened Session
    /// </summary>
    /// <param name="session">SessionId previously opened</param>
    public void OpenSession(SessionId session)
    {
      ControlPlaneConnection();

      if (SessionId == null) logger_.LogDebug($"Open Session {session.PackSessionId()}");
      SessionId ??= session;
    }

    private static TaskOptions InitializeDefaultTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new()
                      {
          Seconds = 300,
        },
        MaxRetries = 5,
        Priority   = 1,
        IdTag      = "ArmonikTag",
      };
      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.Samples.SymphonyPackage");
      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.0.0");
      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.Samples.Symphony.Packages");

      return taskOptions;
    }

    /// <summary>
    /// Wait for the taskIds and all its dependencies taskIds
    /// </summary>
    /// <param name="parentTaskId">The taskIds to </param>
    public void WaitSubtasksCompletion(string parentTaskId)
    {
      var    taskFilter = new TaskFilter();
      string taskId     = parentTaskId.CanUnPackTaskId() ? parentTaskId.UnPackTaskId().Task : parentTaskId;
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = parentTaskId.UnPackTaskId().SubSession;
      logger_.LogDebug(
        $"Wait for subTask {parentTaskId} coming from Session {SessionId.Session} " +
        $"and subSession {SessionId.SubSession} with Task SubSession {parentTaskId.UnPackTaskId().SubSession}");

      ControlPlaneService.WaitForSubTasksCompletion(new()
                                                    {
                                                      Filter = taskFilter, 
                                                      ThrowOnTaskCancellation = true, 
                                                      ThrowOnTaskError = true,
                                                    });
    }

    /// <summary>
    /// User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskId">
    /// The task id of the task to wait for
    /// </param>
    public void WaitCompletion(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId.UnPackTaskId().Task);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskId.UnPackTaskId().SubSession;

      logger_.LogDebug(
        $"Wait for task {taskId} coming from Session {SessionId.Session} " +
        $"and subSession {SessionId.SubSession} with Task SubSession {taskId.UnPackTaskId().SubSession}");

      ControlPlaneService.WaitForCompletion(new()
                                            {
                                              Filter                  = taskFilter,
                                              ThrowOnTaskCancellation = true,
                                              ThrowOnTaskError        = true,
                                            });
    }

    /// <summary>
    /// Method to GetResults when the result is returned by a task
    /// The method WaitForCompletion should called before these method
    /// </summary>
    /// <param name="taskIds">The Task Ids list of the tasks which the result is expected</param>
    /// <returns>return a dictionary with key taskId and payload</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public Task<Dictionary<string, byte[]>> GetResults(IEnumerable<string> taskIds)
    {
      if (taskIds == null) throw new ArgumentNullException(nameof(taskIds));
      if (!taskIds.Any()) throw new ArgumentException(nameof(taskIds));

      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskIds.Select(id => id.UnPackTaskId().Task));
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskIds.Select(id => id.UnPackTaskId().SubSession).First();

      logger_.LogDebug(
        $"GetResults for tasks [{string.Join(", ", taskIds.Select(id => id.UnPackTaskId().Task))}] coming from Session {SessionId.Session} " +
        $"and subSession {SessionId.SubSession} with Task SubSession {taskIds.Select(id => id.UnPackTaskId().SubSession).First()}");

      var                        result  = ControlPlaneService.TryGetResult(taskFilter);
      Dictionary<string, byte[]> results = new();

      foreach (var reply in result.Payloads)
      {
        results[reply.TaskId.Task] = reply.Data.Data.ToByteArray();
      }

      return Task.FromResult(results);
    }


    /// <summary>
    /// User method to submit task from the client
    ///  Need a client Service. In case of ServiceContainer
    /// controlPlaneService can be null until the OpenSession is called
    /// </summary>
    /// <param name="payloads">
    /// The user payload list to execute. General used for subtasking.
    /// </param>
    public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    {
      CreateTaskRequest requests = new();

      requests.SessionId = SessionId;


      foreach (var payload in payloads)
      {
        var taskRequest = new TaskRequest
        {
          Payload = new()
                    {
            Data = ByteString.CopyFrom(payload),
          },
        };

        requests.TaskRequests.Add(taskRequest);
      }

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitTasks with CreateTask");

      requests.TaskOptions = TaskOptions;
      var requestReply = ControlPlaneService.CreateTask(requests);

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
        $"Called  SubmitTasks return [{string.Join(", ", requestReply.TaskIds.Select(id => id.PackTaskId()))}]");

      return requestReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    /// The method to submit sub task inside a parent task
    /// Use this method only on server side developpement
    /// </summary>
    /// <param name="session">The session Id to attached the task </param>
    /// <param name="parentTaskId">The task Id of a parent task</param>
    /// <param name="payloads">A lists of payloads creating a list of subTask</param>
    /// <returns>Return a list of taskId</returns>
    public IEnumerable<string> SubmitSubTasks(string session, string parentTaskId, IEnumerable<byte[]> payloads)
    {
      OpenSession(session.UnPackSessionId());
      if (SubSessionId is null)
        CreateSubSession(parentTaskId);
      var taskRequests = payloads.Select(p => new TaskRequest { Payload = new()
                                                                          { Data = ByteString.CopyFrom(p) } });

      var createTaskRequest = new CreateTaskRequest { SessionId = SubSessionId };
      createTaskRequest.TaskRequests.Add(taskRequests);
      createTaskRequest.TaskOptions = TaskOptions;

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitSubTasks with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
        $"Called  SubmitSubTasks return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.PackTaskId()))}]");
      return createTaskReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    /// The method to submit several tasks with dependencies tasks. This task will wait for
    /// to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the task will be attached</param>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    public IEnumerable<string> SubmitTasksWithDependencies(string session, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      OpenSession(session.UnPackSessionId());
      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new()
                                                 { Data = ByteString.CopyFrom(p.Item1) } };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest { SessionId = session.UnPackSessionId() };
      createTaskRequest.TaskRequests.Add(taskRequests);

      createTaskRequest.TaskOptions = TaskOptions;

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitTasksWithDependencies with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
        $"Called  SubmitTasksWithDependencies return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.PackTaskId()))}] " +
        $" with dependencies : {string.Join("\n\t", payloadWithDependencies.Select(p => $"[{string.Join(", ", p.Item2)}]"))}");

      return createTaskReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    /// The method to submit One Subtask with dependencies tasks. This task will wait for
    /// to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the task will be attached</param>
    /// <param name="parentId">The parent Task who want to create the SubTask</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created SubTask</param>
    /// <returns>return the taskId of the created SubTask </returns>
    public string SubmitSubtaskWithDependencies(string session, string parentId, byte[] payload, IList<string> dependencies)
    {
      return SubmitSubtasksWithDependencies(session,
                                            parentId,
                                            new[]
                                            {
                                              Tuple.Create(payload,
                                                           dependencies),
                                            }).Single();
    }

    /// <summary>
    /// The method to submit several tasks with dependencies tasks. This task will wait for
    /// to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the Subtask will be attached</param>
    /// <param name="parentTaskId">The parent Task who want to create the SubTasks</param>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created Subtasks</param>
    /// <returns>return a list of taskIds of the created Subtasks </returns>
    public IEnumerable<string> SubmitSubtasksWithDependencies(string session, string parentTaskId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      OpenSession(session.UnPackSessionId());
      if (SubSessionId is null)
        CreateSubSession(parentTaskId);
      else
      {
        if (SubSessionId.SubSession != parentTaskId.UnPackTaskId().Task)
          throw new WorkerApiException($"Dependencies issues with Parent TaskId and SubSession {SubSessionId.SubSession} != {parentTaskId.UnPackTaskId().Task}");
      }

      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new()
                                                 { Data = ByteString.CopyFrom(p.Item1) } };
        output.DependenciesTaskIds.Add(p.Item2.Select(t => t.UnPackTaskId().Task));
        return output;
      });
      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = SubSessionId
      };
      createTaskRequest.TaskRequests.Add(taskRequests);
      createTaskRequest.TaskOptions = TaskOptions;


      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitTasksWithDependencies with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
        $"Called  SubmitSubtasksWithDependencies return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.PackTaskId()))}] " +
        $" with dependencies : {string.Join("\n\t", payloadWithDependencies.Select(p => $"[{string.Join(", ", p.Item2)}]"))}");

      return createTaskReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    /// Try to find the result of One task. If there no result, the function return byte[0] 
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] TryGetResult(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId.UnPackTaskId().Task);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskId.UnPackTaskId().SubSession;
      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
        $"Calling TryGetResult for {taskId}");

      var response = ControlPlaneService.TryGetResult(taskFilter);
      logger_.LogDebug(
        $"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
        $"Called TryGetResult for  {taskId}");

      return response.Payloads.Single().Data.ToByteArray();
    }
  }

  /// <summary>
  /// The ArmonikSymphonyClient Extension to single task creation
  /// </summary>
  public static class ArmonikSymphonyClientExt
  {
    /// <summary>
    /// User method to submit task from the client
    /// </summary>
    /// <param name="payload">
    /// The user payload to execute.
    /// </param>
    public static string SubmitTask(this ArmonikSymphonyClient client, byte[] payload)
    {
      return client.SubmitTasks(new[] { payload })
                   .Single();
    }

    /// <summary>
    /// The method to submit sub task coming from a parent task
    /// Use this method only on server side development
    /// </summary>
    /// <param name="parentTaskId">The task Id of a parent task</param>
    /// <param name="payloads">A lists of payloads creating a list of subTask</param>
    /// <returns>Return a list of taskId</returns>
    public static IEnumerable<string> SubmitSubTask(this ArmonikSymphonyClient client, string parentTaskId, byte[] payloads)
    {
      return client.SubmitSubTasks(client.SessionId.PackSessionId(),
                                   parentTaskId,
                                   new[] { payloads });
    }

    /// <summary>
    /// The method to submit One task with dependencies tasks. This task will wait for
    /// to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the task will be attached</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created task</param>
    /// <returns>return the taskId of the created task </returns>
    public static string SubmitTaskWithDependencies(this ArmonikSymphonyClient client, byte[] payload, IList<string> dependencies)
    {
      return client.SubmitTasksWithDependencies(client.SessionId.PackSessionId(),
                                                new[]
                                                {
                                                  Tuple.Create(payload,
                                                               dependencies),
                                                }).Single();
    }

    /// <summary>
    /// Get the result of One task. If there no result, the function return byte[0] 
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public static byte[] GetResult(this ArmonikSymphonyClient client, string taskId)
    {
      var results = client.GetResults(new List<string>() { taskId });
      Task.WaitAll(results);
      client.logger_.LogDebug(
        $"{client.SessionId?.PackSessionId()} {client.SubSessionId?.PackSessionId()}: " +
        $"Called GetResult for  {taskId}");
      return results.Result[taskId.UnPackTaskId().Task];
    }
  }
}