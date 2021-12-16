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

using Google.Protobuf.WellKnownTypes;

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client
{
  public class ArmonikSymphonyClient
  {
    private readonly IConfiguration                    configuration_;
    private readonly IConfigurationSection             controlPlanAddress;
    private          ClientService.ClientServiceClient controlPlaneService;

    public string SectionControlPlan { get; set; } = "ControlPlan";
    public string SectionGrpcChannel { get; set; } = "GrpcChannel";

    public ArmonikSymphonyClient(IConfiguration configuration)
    {
      configuration_     = configuration;
      controlPlanAddress = configuration_.GetSection(SectionControlPlan).GetSection(SectionGrpcChannel);
    }


    /// <summary>
    /// User call to get customer data from client (Server side)
    /// </summary>
    /// <param name="key">
    /// The user key that can be retrieved later from client side.
    /// </param>
    public byte[] GetData(string key)
    {
      throw new NotImplementedException("The method [GetData] Need new gRPC connection");
    }

    public void StoreData(string key, byte[] data) =>
      throw new NotImplementedException("The method [StoreData] Need new gRPC connection");

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="sessionOptions"></param>
    /// <returns></returns>
    public string CreateSession()
    {
      var channel = GrpcChannel.ForAddress(controlPlanAddress["Address"]);
      controlPlaneService ??= new ClientService.ClientServiceClient(channel);

      var sessionOptions = new SessionOptions
      {
        DefaultTaskOption = new TaskOptions
        {
          MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(20)),
          MaxRetries  = 2,
          Priority    = 1,
        },
      };
      SessionId = controlPlaneService.CreateSession(sessionOptions);
      return SessionId.PackId();
    }

    /// <summary>
    /// Create the SubSession to submit task
    /// </summary>
    /// <param name="parentSession"></param>
    /// <param name="sessionOptions"></param>
    /// <returns></returns>
    public void CreateSubSession(string parentId)
    {
      lock (this)
      {
        if (SubSessionId is null)
        {
          var sessionOptions = new SessionOptions
          {
            DefaultTaskOption = new TaskOptions
            {
              MaxDuration = Duration.FromTimeSpan(TimeSpan.FromMinutes(20)),
              MaxRetries  = 2,
              Priority    = 1,
            },
            ParentTask = new TaskId { Session = SessionId.Session, SubSession = SessionId.SubSession, Task = parentId },
          };
          SubSessionId = controlPlaneService.CreateSession(sessionOptions);
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


    public void OpenSession(string session)
    {
      SessionId = session.UnPackId();
      //return Disposable.Create(() => { SubSessionId = null; });
    }

    private TaskOptions InitializeDefaultTaskOptions()
    {
      TaskOptions taskOptions = new();
      taskOptions.MaxDuration         = new Duration();
      taskOptions.MaxDuration.Seconds = 60;
      taskOptions.MaxRetries          = 5;
      taskOptions.IdTag               = "ddu";

      return taskOptions;
    }

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
      if (controlPlaneService == null)
      {
        throw new NullReferenceException("Client Service is required to submit task");
      }

      CreateTaskRequest requests = new();
      requests.TaskOptions = InitializeDefaultTaskOptions();
      requests.SessionId   = SessionId;
      requests.TaskOptions.Options.Add(AppsOptions.GridAppNameKey,
                                       "ArmoniK.Samples.SymphonyPackage");
      requests.TaskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                                       "1.0.0");
      requests.TaskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                                       "ArmoniK.Samples.Symphony.Packages");

      foreach (byte[] payload in payloads)
      {
        TaskRequest taskRequest = new TaskRequest();
        taskRequest.Payload      = new Payload();
        taskRequest.Payload.Data = ByteString.CopyFrom(payload);

        requests.TaskRequests.Add(taskRequest);
      }

      CreateTaskReply requestReply = controlPlaneService.CreateTask(requests);
      return requestReply.TaskIds.Select(id => id.Task);
    }

    /// <summary>
    /// User method to wait for tasks from the client
    /// </summary>
    /// <param name="taskID">
    /// The task id of the task
    /// </param>
    public void WaitCompletion(string taskId)
    {
      TaskFilter taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);
      controlPlaneService.WaitForCompletion(taskFilter);
    }

    public async Task<Dictionary<string, byte[]>> GetResults(IEnumerable<string> taskId)
    {
      TaskFilter taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);

      var                        result  = controlPlaneService.TryGetResult(taskFilter);
      Dictionary<string, byte[]> results = new();

      foreach (var reply in result.Payloads)
      {
        results[reply.TaskId.Task] = reply.Data.Data.ToByteArray();
      }

      return results;
    }

    //TODO : See what will be the goal in the new Control Agent
    public void WaitSubtasksCompletion(string parentId) => WaitCompletion(parentId);

    public IEnumerable<string> SubmitSubTasks(string session, string parentTaskId, IEnumerable<byte[]> payloads)
    {
      if (SubSessionId is null)
        CreateSubSession(parentTaskId);
      var taskRequests      = payloads.Select(p => new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p) } });
      var createTaskRequest = new CreateTaskRequest { SessionId = SubSessionId };
      createTaskRequest.TaskRequests.Add(taskRequests);
      var createTaskReply = controlPlaneService.CreateTask(createTaskRequest);
      return createTaskReply.TaskIds.Select(id => id.Task);
    }

    public string SubmitTaskWithDependencies(string session, byte[] payload, IList<string> dependencies)
    {
      return SubmitTaskWithDependencies(session,
                                        new[]
                                        {
                                          Tuple.Create(payload,
                                                       dependencies),
                                        }).Single();
    }

    public IEnumerable<string> SubmitTaskWithDependencies(string session, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p.Item1) } };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest { SessionId = session.UnPackId() };
      createTaskRequest.TaskRequests.Add(taskRequests);
      var createTaskReply = controlPlaneService.CreateTask(createTaskRequest);
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

    public IEnumerable<string> SubmitSubtasksWithDependencies(string session, string parentId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      var taskRequests = payloadWithDependencies.Select(p =>
      {
        var output = new TaskRequest { Payload = new Payload { Data = ByteString.CopyFrom(p.Item1) } };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = new SessionId
          { Session = session.UnPackId().Session, SubSession = parentId },
      };
      createTaskRequest.TaskRequests.Add(taskRequests);
      var createTaskReply = controlPlaneService.CreateTask(createTaskRequest);
      return createTaskReply.TaskIds.Select(id => id.Task);
    }

    public byte[] TryGetResult(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = SessionId.SubSession;
      var response = controlPlaneService.TryGetResult(taskFilter);
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