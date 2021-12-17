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

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client
{
  public class ArmonikSymphonyClient
  {
    private readonly IConfiguration        configuration_;
    private readonly IConfigurationSection controlPlanAddress_;

    private ClientService.ClientServiceClient ControlPlaneService { get; set; }

    public string SectionControlPlan { get; set; } = "Grpc";

    public ArmonikSymphonyClient(IConfiguration configuration)
    {
      configuration_      = configuration;
      controlPlanAddress_ = configuration_.GetSection(SectionControlPlan);
    }

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="sessionOptions"></param>
    /// <returns></returns>
    public string CreateSession()
    {
      ControlPlaneConnection();

      var sessionOptions = new SessionOptions
      {
        DefaultTaskOption = new TaskOptions
        {
          MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(20)),
          MaxRetries  = 2,
          Priority    = 1,
        },
      };
      SessionId = ControlPlaneService.CreateSession(sessionOptions);
      return SessionId.PackId();
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
      lock (this)
      {
        ControlPlaneConnection();
        if (SubSessionId is null)
        {
          var sessionOptions = new SessionOptions
          {
            DefaultTaskOption = InitializeDefaultTaskOptions(),
            ParentTask        = new TaskId { Session = SessionId.Session, SubSession = SessionId.SubSession, Task = parentId },
          };
          SubSessionId = ControlPlaneService.CreateSession(sessionOptions);
        }
      }
    }

    public SessionId SessionId { get; private set; }
    public SessionId SubSessionId { get; set; }


    //public void OpenSession(string session)
    //{
    //  var channel = GrpcChannel.ForAddress(controlPlanAddress["Address"]);
    //  controlPlaneService ??= new ClientService.ClientServiceClient(channel);

    //  var sessionOptions = new SessionOptions
    //  {
    //    DefaultTaskOption = InitializeDefaultTaskOptions(),
    //    ParentSession     = new() { Session = session?.UnPackId().Session },
    //  };
    //  _sessionId = controlPlaneService.CreateSession(sessionOptions);
    //}


    public void OpenSession(SessionId session)
    {
      ControlPlaneConnection();
      SessionId ??= session;

      //return Disposable.Create(() => { SubSessionId = null; });
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


    /// <summary>
    /// User method to wait for tasks from the client
    /// </summary>
    /// <param name="taskID">
    /// The task id of the task
    /// </param>
    public void WaitCompletion(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);
      ControlPlaneService.WaitForCompletion(taskFilter);
    }

    public Task<Dictionary<string, byte[]>> GetResults(IEnumerable<string> taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = SessionId.SubSession;

      ControlPlaneService.WaitForCompletion(taskFilter);

      var                        result  = ControlPlaneService.TryGetResult(taskFilter);
      Dictionary<string, byte[]> results = new();

      foreach (var reply in result.Payloads)
      {
        results[reply.TaskId.Task] = reply.Data.Data.ToByteArray();
      }

      return Task.FromResult(results);
    }

    //TODO : See what will be the goal in the new Control Agent
    public void WaitSubtasksCompletion(string parentId) => WaitCompletion(parentId);


    /// <summary>
    /// User method to submit task from the client
    ///  Need a client Service. In case of ServiceContainer
    /// controlPlaneService can be null until the OpenSession is called
    /// </summary>
    /// <param name="payloads">
    /// The user payload list to execute. Generaly used for subtasking.
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

      requests.TaskOptions = InitializeDefaultTaskOptions();
      var requestReply = ControlPlaneService.CreateTask(requests);
      return requestReply.TaskIds.Select(id => id.Task);
    }

    public IEnumerable<string> SubmitSubTasks(string session, string parentTaskId, IEnumerable<byte[]> payloads)
    {
      OpenSession(session.UnPackId());
      if (SubSessionId is null)
        CreateSubSession(parentTaskId);
      var taskRequests      = payloads.Select(p => new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p) } });
      var createTaskRequest = new CreateTaskRequest { SessionId = SubSessionId };
      createTaskRequest.TaskRequests.Add(taskRequests);
      createTaskRequest.TaskOptions = InitializeDefaultTaskOptions();
      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);
      return createTaskReply.TaskIds.Select(id => id.Task);
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
      OpenSession(session.UnPackId());
      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p.Item1) } };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest { SessionId = session.UnPackId() };
      createTaskRequest.TaskRequests.Add(taskRequests);

      createTaskRequest.TaskOptions = InitializeDefaultTaskOptions();

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);
      return createTaskReply.TaskIds.Select(id => id.Task);
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
      OpenSession(session.UnPackId());
      if (SubSessionId is null)
        CreateSubSession(parentTaskId);
      else
      {
        if (SubSessionId.SubSession != parentTaskId)
          throw new WorkerApiException($"Dependencies issues with Parent TaskId and SubSession {SubSessionId.SubSession} != {parentTaskId}");
      }

      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p.Item1) } };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = SubSessionId
      };
      createTaskRequest.TaskRequests.Add(taskRequests);
      createTaskRequest.TaskOptions = InitializeDefaultTaskOptions();

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);
      return createTaskReply.TaskIds.Select(id => id.Task);
    }

    public byte[] TryGetResult(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = SessionId.SubSession;

      var response = ControlPlaneService.TryGetResult(taskFilter);
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
      return results.Result[taskId];
    }
  }
}