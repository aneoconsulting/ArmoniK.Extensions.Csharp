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

using Grpc.Core;
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
  public class ArmonikSymphonyClient
  {
    private readonly  IConfiguration                 configuration_;
    private readonly  IConfigurationSection          controlPlanAddress_;
    internal readonly ILogger<ArmonikSymphonyClient> logger_;
    private ClientService.ClientServiceClient ControlPlaneService { get; set; }

    public string SectionControlPlan { get; set; } = "Grpc";

    public TaskOptions TaskOptions { get; set; }

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
    /// <param name="sessionOptions"></param>
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
      ControlPlaneService ??= new ClientService.ClientServiceClient(channel);
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
            ParentTask = new TaskId
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

    public SessionId SessionId { get; private set; }
    public SessionId SubSessionId { get; set; }

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
        MaxDuration = new Duration
        {
          Seconds = 300,
        },
        MaxRetries = 5,
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


    public void WaitSubtasksCompletion(string parentId)
    {
      var    taskFilter = new TaskFilter();
      string taskId     = parentId.CanUnPackTaskId() ? parentId.UnPackTaskId().Task : parentId;
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = parentId.UnPackTaskId().SubSession;
      logger_.LogDebug(
        $"Wait for subTask {parentId} coming from Session {SessionId.Session} " +
        $"and subSession {SessionId.SubSession} with Task SubSession {parentId.UnPackTaskId().SubSession}");

      ControlPlaneService.WaitForSubTasksCompletion(taskFilter);
    }

    /// <summary>
    /// User method to wait for tasks from the client
    /// </summary>
    /// <param name="taskID">
    /// The task id of the task
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

      ControlPlaneService.WaitForCompletion(taskFilter);
    }

    public Task<Dictionary<string, byte[]>> GetResults(IEnumerable<string> taskId)
    {
      if (taskId == null) throw new ArgumentNullException(nameof(taskId));
      if (!taskId.Any()) throw new ArgumentException(nameof(taskId));

      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId.Select(id => id.UnPackTaskId().Task));
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskId.Select(id => id.UnPackTaskId().SubSession).First();

      logger_.LogDebug(
        $"GetResults for tasks [{string.Join(", ", taskId.Select(id => id.UnPackTaskId().Task))}] coming from Session {SessionId.Session} " +
        $"and subSession {SessionId.SubSession} with Task SubSession {taskId.Select(id => id.UnPackTaskId().SubSession).First()}");

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
          Payload = new Payload
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

    public IEnumerable<string> SubmitSubTasks(string session, string parentTaskId, IEnumerable<byte[]> payloads)
    {
      OpenSession(session.UnPackSessionId());
      if (SubSessionId is null)
        CreateSubSession(parentTaskId);
      var taskRequests = payloads.Select(p => new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p) } });

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

    public string SubmitTaskWithDependencies(string session, byte[] payload, IList<string> dependencies)
    {
      return SubmitTasksWithDependencies(session,
                                         new[]
                                         {
                                           Tuple.Create(payload,
                                                        dependencies),
                                         }).Single();
    }

    public IEnumerable<string> SubmitTasksWithDependencies(string session, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      OpenSession(session.UnPackSessionId());
      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p.Item1) } };
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
        var output = new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p.Item1) } };
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