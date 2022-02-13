using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common.Exceptions;


#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#else
using Grpc.Core;
#endif

namespace ArmoniK.DevelopmentKit.GridServer
{
  /// <summary>
  ///   The main object to communicate with the control Plane from the client side
  ///   The class will connect to the control plane to createSession, SubmitTask,
  ///   Wait for result and get the result.
  ///   See an example in the project ArmoniK.Samples in the sub project
  ///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/GridServerLike
  ///   Samples.ArmoniK.Sample.SymphonyClient
  /// </summary>
  [MarkDownDoc]
  public class ArmonikDataSynapseClientService
  {
    private readonly  IConfigurationSection                    controlPlanAddress_;
    internal readonly ILogger<ArmonikDataSynapseClientService> Logger;
    private ClientService.ClientServiceClient ControlPlaneService { get; set; }

    /// <summary>
    /// Returns the section key Grpc from appSettings.json
    /// </summary>
    public static string SectionControlPlan { get; } = "Grpc";

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

    /// <summary>
    /// The ctor with IConfiguration and optional TaskOptions
    /// 
    /// </summary>
    /// <param name="configuration">IConfiguration to set Client Data information and Grpc EndPoint</param>
    /// <param name="loggerFactory">The factory to create the logger for clientService</param>
    /// <param name="taskOptions">TaskOptions for any Session</param>
    public ArmonikDataSynapseClientService(IConfiguration configuration, ILoggerFactory loggerFactory, TaskOptions taskOptions = null)
    {
      controlPlanAddress_ = configuration.GetSection(SectionControlPlan);

      Logger = loggerFactory.CreateLogger<ArmonikDataSynapseClientService>();

      if (taskOptions != null) TaskOptions = taskOptions;
    }

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="taskOptions">Optional parameter to set TaskOptions during the Session creation</param>
    /// <returns></returns>
    public SessionId CreateSession(TaskOptions taskOptions = null)
    {
      if (taskOptions != null) TaskOptions = taskOptions;

      ControlPlaneConnection();

      var sessionOptions = new SessionOptions
      {
        DefaultTaskOption = TaskOptions,
      };
      Logger.LogDebug("Creating Session... ");

      SessionId = ControlPlaneService.CreateSession(sessionOptions);
      Logger.LogDebug($"Session Created {SessionId.Session}");


      return SessionId;
    }

    private void ControlPlaneConnection()
    {
#if NET5_0_OR_GREATER
      var channel = GrpcChannel.ForAddress(controlPlanAddress_["Endpoint"]);
#else
      var uri     = new Uri(controlPlanAddress_["Endpoint"]);
      var channel = new Channel(uri.Host,
                                uri.Port,
                                ChannelCredentials.Insecure);
#endif
      ControlPlaneService ??= new ClientService.ClientServiceClient(channel);
    }

    /// <summary>
    /// Set connection to an already opened Session
    /// </summary>
    /// <param name="session">SessionId previously opened</param>
    public void OpenSession(string session)
    {
      ControlPlaneConnection();

      if (SessionId == null) Logger.LogDebug($"Open Session {session}");
      SessionId ??= new()
      {
        Session = session
      };
    }

    /// <summary>
    /// This method is creating a default taskOptions initialization where
    /// MaxDuration is 40 seconds, MaxRetries = 2 The app name is ArmoniK.DevelopmentKit.GridServer
    /// The version is 1.0.0 the namespace ArmoniK.DevelopmentKit.GridServer and simple service FallBackServerAdder 
    /// </summary>
    /// <returns>Return the default taskOptions</returns>
    public static TaskOptions InitializeDefaultTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new()
        {
          Seconds = 40,
        },
        MaxRetries = 2,
        Priority   = 1,
        IdTag      = "ArmonikTag",
      };

      taskOptions.Options.Add(AppsOptions.EngineTypeNameKey,
                              EngineType.DataSynapse.ToString());

      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.0.0");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridServiceNameKey,
                              "FallBackServerAdder");

      return taskOptions;
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
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId = SessionId.Session;

      Logger.LogDebug($"Wait for task {taskId} coming from Session {SessionId.Session} " +
                      $"and subSession {SessionId.SubSession} with Task SubSession {taskId}");

      ControlPlaneService.WaitForSubTasksCompletion(new()
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
    public async Task<IEnumerable<Tuple<string, byte[]>>> GetResults(IEnumerable<string> taskIds)
    {
      if (taskIds == null) throw new ArgumentNullException(nameof(taskIds));
      var ids = taskIds.ToList();

      if (!ids.Any())
        throw new ArgumentException("Must contains at least one message",
                                    nameof(taskIds));

      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(ids);
      taskFilter.SessionId = SessionId.Session;

      Logger.LogDebug($"GetResults for tasks [{string.Join(", ", ids)}] coming from Session {SessionId.Session} " +
                      $"and subSession {SessionId.SubSession} with Task SubSession {ids.Select(id => id).First()}");

      var result = await ControlPlaneService.TryGetResultAsync(taskFilter);


      return result.Payloads.Select(p => new Tuple<string, byte[]>(p.TaskId.Task,
                                                                   p.Data?.Data?.ToByteArray()));
    }


    /// <summary>
    /// User method to submit task from the client
    ///  Need a client Service. In case of ServiceContainer
    /// controlPlaneService can be null until the OpenSession is called
    /// </summary>
    /// <param name="payloads">
    /// The user payload list to execute. General used for subTasking.
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

      Logger.LogDebug($"{SessionId?.Session} {SubSessionId?.Session}: Calling SubmitTasks with CreateTask");

      requests.TaskOptions = TaskOptions;
      var requestReply = ControlPlaneService.CreateTask(requests);

      Logger.LogDebug($"{SessionId?.Session} {SubSessionId?.Session}: " +
                      $"Called  SubmitTasks return [{string.Join(", ", requestReply.TaskIds)}]");

      return requestReply.TaskIds.Select(id => id.Task);
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
      OpenSession(session);
      IEnumerable<Tuple<byte[], IList<string>>> withDependencies = payloadWithDependencies.ToList();

      var taskRequests = withDependencies.Select(p =>
      {
        var output = new TaskRequest
        {
          Payload = new Payload
          {
            Data = ByteString.CopyFrom(p.Item1)
          }
        };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = new()
        {
          Session = session
        }
      };
      createTaskRequest.TaskRequests.Add(taskRequests);

      createTaskRequest.TaskOptions = TaskOptions;

      Logger.LogDebug($"{SessionId?.Session} {SubSessionId?.Session}: Calling SubmitTasksWithDependencies with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      Logger.LogDebug($"{SessionId?.Session} {SubSessionId?.Session}: " +
                      $"Called  SubmitTasksWithDependencies return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.Task))}] " +
                      $" with dependencies : {string.Join("\n\t", withDependencies.Select(p => $"[{string.Join(", ", p.Item2)}]"))}");

      return createTaskReply.TaskIds.Select(id => id.Task);
    }


    /// <summary>
    /// Try to find the result of One task. If there no result, the function return byte[0] 
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] TryGetResult(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId = SessionId.Session;

      Logger.LogDebug($"{SessionId?.Session} {SubSessionId?.Session}: " +
                      $"Calling TryGetResult for {taskId}");

      var response = ControlPlaneService.TryGetResult(taskFilter);
      Logger.LogDebug($"{SessionId?.Session} {SubSessionId?.Session}: " +
                      $"Called TryGetResult for  {taskId}");

      return response.Payloads.Single().Data.ToByteArray();
    }


    /// <summary>
    /// Close Session. This function will disabled in nex Release. The session is automatically
    /// closed after an other creation or after a disconnection or after end of timeout the tasks submitted
    /// </summary>
    public void CloseSession()
    {
      ControlPlaneService.CloseSession(SessionId);
      SessionId = null;
    }

    /// <summary>
    /// Cancel the current Session where the SessionId is the one created previously
    /// </summary>
    public void CancelSession()
    {
      ControlPlaneService.CancelSession(SessionId);
    }
  }

  /// <summary>
  /// The ArmonikSymphonyClient Extension to single task creation
  /// </summary>
  public static class ArmonikDataSynapseClientServiceExt
  {
    /// <summary>
    /// User method to submit task from the client
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="payload">
    /// The user payload to execute.
    /// </param>
    public static string SubmitTask(this ArmonikDataSynapseClientService client, byte[] payload)
    {
      return client.SubmitTasks(new[] { payload })
                   .Single();
    }

    /// <summary>
    /// The method to submit One task with dependencies tasks. This task will wait for
    /// to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created task</param>
    /// <returns>return the taskId of the created task </returns>
    public static string SubmitTaskWithDependencies(this ArmonikDataSynapseClientService client, byte[] payload, IList<string> dependencies)
    {
      return client.SubmitTasksWithDependencies(client.SessionId.Session,
                                                new[]
                                                {
                                                  Tuple.Create(payload,
                                                               dependencies),
                                                }).Single();
    }

    /// <summary>
    /// Get the result of One task. If there no result, the function return byte[0] 
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public static byte[] GetResult(this ArmonikDataSynapseClientService client, string taskId)
    {
      var results = client.GetResults(new List<string>
      {
        taskId
      });
      Task.WaitAll(results);
      client.Logger.LogDebug($"{client.SessionId?.Session} {client.SubSessionId?.Session}: " +
                             $"Called GetResult for  {taskId}");

      return results.Result.Single(t => t.Item1.Equals(taskId)).Item2;
    }
  }
}