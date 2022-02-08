using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client.api
{
  /// <summary>
  /// The class SessionService will be create each time the function CreateSession or OpenSession will
  /// be called by client or by the worker.
  /// </summary>
  [MarkDownDoc]
  public class SessionService
  {
    /// <summary>
    ///   Set or Get TaskOptions with inside MaxDuration, Priority, AppName, VersionName and AppNamespace
    /// </summary>
    public TaskOptions TaskOptions { get; set; }

    /// <summary>
    ///   Only used for internal DO NOT USED IT
    ///   Get or Set SessionId object stored during the call of SubmitTask, SubmitSubTask,
    ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public SessionId SessionId { get; private set; }


#pragma warning disable CS1591
    public SessionOptions SessionOptions { get; private set; }

    public SessionId SubSessionId { get; private set; }


    public string ParentTaskId { get; set; }

#pragma warning restore CS1591

    private ILoggerFactory LoggerFactory { get; set; }

    internal ILogger<SessionService> Logger { get; set; }

    private ClientService.ClientServiceClient ControlPlaneService { get; set; }

    /// <summary>
    /// Ctor to instantiate a new SessionService
    /// This is an object to send task or get Results from a session
    /// </summary>
    public SessionService(ILoggerFactory                    loggerFactory,
                          ClientService.ClientServiceClient controlPlaneService,
                          TaskOptions                       taskOptions    = null,
                          SessionOptions                    sessionOptions = null)
    {
      Logger = loggerFactory.CreateLogger<SessionService>();


      taskOptions ??= SessionOptions?.DefaultTaskOption;
      taskOptions ??= InitializeDefaultTaskOptions();

      TaskOptions = taskOptions;
      CopyTaskOptionsForClient(TaskOptions);

      ControlPlaneService = controlPlaneService;

      sessionOptions ??= new SessionOptions
      {
        DefaultTaskOption = TaskOptions,
      };
      SessionOptions = sessionOptions;

      Logger.LogDebug("Creating Session... ");

      SessionId = ControlPlaneService.CreateSession(SessionOptions);

      Logger.LogDebug($"Session Created {SessionId.PackSessionId()}");
    }

    /// <summary>
    /// Create SessionService with a previous opened session
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="controlPlaneService"></param>
    /// <param name="sessionId"></param>
    /// <param name="clientOptions">CLient options passed during the CreateSession</param>
    public SessionService(ILoggerFactory                    loggerFactory,
                          ClientService.ClientServiceClient controlPlaneService,
                          SessionId                         sessionId,
                          IDictionary<string, string>       clientOptions)
    {
      Logger = loggerFactory.CreateLogger<SessionService>();

      TaskOptions = CopyClientToTaskOptions(clientOptions);

      ControlPlaneService = controlPlaneService;

      var sessionOptions = new SessionOptions
      {
        DefaultTaskOption = TaskOptions,
      };
      SessionOptions = sessionOptions;

      Logger.LogDebug("Creating Session... ");

      SessionId = sessionId;

      Logger.LogInformation($"Session Created {SessionId.PackSessionId()} with taskOptions.Priority : {TaskOptions.Priority}");
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
      return SessionId?.PackSessionId();
    }

    private static TaskOptions InitializeDefaultTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new()
        {
          Seconds = 300,
        },
        MaxRetries = 3,
        Priority   = 1,
        IdTag      = "ArmonikTag",
      };
      taskOptions.Options.Add(AppsOptions.EngineTypeNameKey,
                              EngineType.Symphony.ToString());

      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.Samples.SymphonyPackage");
      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.0.0");
      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.Samples.Symphony.Packages");

      CopyTaskOptionsForClient(taskOptions);

      return taskOptions;
    }

    private static void CopyTaskOptionsForClient(TaskOptions taskOptions)
    {
      taskOptions.Options["MaxDuration"] = taskOptions.MaxDuration.Seconds.ToString();
      taskOptions.Options["MaxRetries"]  = taskOptions.MaxRetries.ToString();
      taskOptions.Options["Priority"]    = taskOptions.Priority.ToString();
      taskOptions.Options["IdTag"]       = taskOptions.IdTag.ToString();
    }

    private TaskOptions CopyClientToTaskOptions(IDictionary<string, string> clientOptions)
    {
      TaskOptions defaultTaskOption = InitializeDefaultTaskOptions();

      TaskOptions taskOptions = new()
      {
        MaxDuration = new()
        {
          Seconds = clientOptions.ContainsKey("MaxDuration") ? long.Parse(clientOptions["MaxDuration"]) : defaultTaskOption.MaxDuration.Seconds,
        },
        MaxRetries = clientOptions.ContainsKey("MaxRetries") ? int.Parse(clientOptions["MaxRetries"]) : defaultTaskOption.MaxRetries,
        Priority   = clientOptions.ContainsKey("Priority") ? int.Parse(clientOptions["Priority"]) : defaultTaskOption.Priority,

        IdTag = clientOptions.ContainsKey("IdTag") ? clientOptions["IdTag"] : defaultTaskOption.IdTag,
      };

      defaultTaskOption.Options.ToList()
                       .ForEach(pair => taskOptions.Options[pair.Key] = pair.Value);

      clientOptions.ToList()
                   .ForEach(pair => taskOptions.Options[pair.Key] = pair.Value);

      return taskOptions;
    }

    /// <summary>
    ///   Set connection to an already opened Session
    /// </summary>
    /// <param name="session">SessionId previously opened</param>
    public void OpenSession(SessionId session)
    {
      if (SessionId == null) Logger.LogDebug($"Open Session {session.PackSessionId()}");
      SessionId = session;
    }

    /// <summary>
    ///   Create the SubSession to submit task
    /// </summary>
    /// <returns></returns>
    private void CreateSubSession(string parentId)
    {
      Logger.LogDebug($"Creating SubSession from Session : {SessionId.PackSessionId()}");
      lock (this)
      {
        if (string.IsNullOrEmpty(ParentTaskId) || !ParentTaskId.Equals(parentId))
        {
          var sessionOptions = new SessionOptions
          {
            DefaultTaskOption = TaskOptions,
            ParentTask = new()
            {
              Session    = SessionId.Session,
              SubSession = parentId.UnPackTaskId().SubSession,
              Task       = parentId.UnPackTaskId().Task,
            },
          };
          Logger.LogDebug($"Creating SubSession from Session :   {SessionId?.PackSessionId()}");
          SubSessionId = ControlPlaneService.CreateSession(sessionOptions);

          ParentTaskId = parentId;
          Logger.LogDebug($"Created  SubSession from Session {SessionId?.PackSessionId()}" +
                          $" new SubSession {SubSessionId?.PackSessionId()} with ParentTask {sessionOptions.ParentTask.PackTaskId()}");
        }
      }
    }


    /// <summary>
    ///   Method to GetResults when the result is returned by a task
    ///   The method WaitForCompletion should called before these method
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
      taskFilter.IncludedTaskIds.Add(ids.Select(id => id.UnPackTaskId().Task));
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = ids.Select(id => id.UnPackTaskId().SubSession).First();

      Logger.LogDebug($"GetResults for tasks [{string.Join(", ", ids.Select(id => id.UnPackTaskId().Task))}] coming from Session {SessionId.Session} " +
                      $"and subSession {SessionId.SubSession} with Task SubSession {ids.Select(id => id.UnPackTaskId().SubSession).First()}");

      var result = await ControlPlaneService.TryGetResultAsync(taskFilter);


      return result.Payloads.Select(p => new Tuple<string, byte[]>(p.TaskId.PackTaskId(),
                                                                   p.Data?.Data?.ToByteArray()));
    }


    /// <summary>
    ///   User method to submit task from the client
    ///   Need a client Service. In case of ServiceContainer
    ///   controlPlaneService can be null until the OpenSession is called
    /// </summary>
    /// <param name="payloads">
    ///   The user payload list to execute. General used for subTasking.
    /// </param>
    public IEnumerable<string> SubmitTasks(IEnumerable<byte[]> payloads)
    {
      CreateTaskRequest requests = new();

      requests.SessionId = SessionId;

      var lPayloads = payloads?.ToList();
      if (lPayloads == null || !lPayloads.Any())
      {
        throw new WorkerApiException("No payload was sent by the submitter");
      }


      foreach (var payload in lPayloads)
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

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitTasks with CreateTask");

      requests.TaskOptions = TaskOptions;
      var requestReply = ControlPlaneService.CreateTask(requests);

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Called  SubmitTasks return [{string.Join(", ", requestReply.TaskIds.Select(id => id.PackTaskId()))}]");

      return requestReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    ///   The method to submit sub task inside a parent task
    ///   Use this method only on server side developpement
    /// </summary>
    /// <param name="session">The session Id to attached the task </param>
    /// <param name="parentTaskId">The task Id of a parent task</param>
    /// <param name="payloads">A lists of payloads creating a list of subTask</param>
    /// <returns>Return a list of taskId</returns>
    public IEnumerable<string> SubmitSubTasks(string parentTaskId, IEnumerable<byte[]> payloads)
    {
      CreateSubSession(parentTaskId);

      var taskRequests = payloads.Select(p => new TaskRequest
      {
        Payload = new()
        {
          Data = ByteString.CopyFrom(p),
        },
      });

      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = SubSessionId,
      };
      createTaskRequest.TaskRequests.Add(taskRequests);
      createTaskRequest.TaskOptions = TaskOptions;

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitSubTasks with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Called  SubmitSubTasks return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.PackTaskId()))}]");
      return createTaskReply.TaskIds.Select(id => id.PackTaskId());
    }


    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the task will be attached</param>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      IEnumerable<Tuple<byte[], IList<string>>> withDependencies = payloadWithDependencies.ToList();

      var taskRequests = withDependencies.Select(p =>
      {
        var output = new TaskRequest
        {
          Payload = new()
          {
            Data = ByteString.CopyFrom(p.Item1),
          },
        };
        output.DependenciesTaskIds.Add(p.Item2);
        return output;
      });
      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = SessionId,
      };
      createTaskRequest.TaskRequests.Add(taskRequests);

      createTaskRequest.TaskOptions = TaskOptions;

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitTasksWithDependencies with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Called  SubmitTasksWithDependencies return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.PackTaskId()))}] " +
                      $" with dependencies : {string.Join("\n\t", withDependencies.Select(p => $"[{string.Join(", ", p.Item2)}]"))}");

      return createTaskReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    ///   The method to submit One SubTask with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the task will be attached</param>
    /// <param name="parentId">The parent Task who want to create the SubTask</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created SubTask</param>
    /// <returns>return the taskId of the created SubTask </returns>
    public string SubmitSubtaskWithDependencies(string parentId, byte[] payload, IList<string> dependencies)
    {
      return SubmitSubtasksWithDependencies(parentId,
                                            new[]
                                            {
                                              Tuple.Create(payload,
                                                           dependencies),
                                            }).Single();
    }

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="session">The session Id where the Subtask will be attached</param>
    /// <param name="parentTaskId">The parent Task who want to create the SubTasks</param>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created Subtasks</param>
    /// <returns>return a list of taskIds of the created Subtasks </returns>
    public IEnumerable<string> SubmitSubtasksWithDependencies(string parentTaskId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      CreateSubSession(parentTaskId);

      IEnumerable<Tuple<byte[], IList<string>>> withDependencies = payloadWithDependencies.ToList();
      var taskRequests = withDependencies.Select(p =>
      {
        var output = new TaskRequest
        {
          Payload = new()
          {
            Data = ByteString.CopyFrom(p.Item1),
          },
        };
        output.DependenciesTaskIds.Add(p.Item2.Select(t => t.UnPackTaskId().Task));
        return output;
      });
      var createTaskRequest = new CreateTaskRequest
      {
        SessionId = SubSessionId,
      };
      createTaskRequest.TaskRequests.Add(taskRequests);
      createTaskRequest.TaskOptions = TaskOptions;


      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: Calling SubmitTasksWithDependencies with CreateTask");

      var createTaskReply = ControlPlaneService.CreateTask(createTaskRequest);

      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Called  SubmitSubtasksWithDependencies return [{string.Join(", ", createTaskReply.TaskIds.Select(id => id.PackTaskId()))}] " +
                      $" with dependencies : {string.Join("\n\t", withDependencies.Select(p => $"[{string.Join(", ", p.Item2)}]"))}");

      return createTaskReply.TaskIds.Select(id => id.PackTaskId());
    }

    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] TryGetResult(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId.UnPackTaskId().Task);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskId.UnPackTaskId().SubSession;
      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Calling TryGetResult for {taskId}");

      var response = ControlPlaneService.TryGetResult(taskFilter);
      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Called TryGetResult for  {taskId}");

      return response.Payloads.Single().Data.ToByteArray();
    }

    /// <summary>
    /// Try to get result of a list of taskIds 
    /// </summary>
    /// <param name="taskIds"></param>
    /// <returns>Returns an Enumerable pair of </returns>
    public IEnumerable<Tuple<string, byte[]>> TryGetResults(IEnumerable<string> taskIds)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskIds.Select(t => t.UnPackTaskId().Task));
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskIds.First().UnPackTaskId().SubSession;
      Logger.LogDebug($"{SessionId?.PackSessionId()} {SubSessionId?.PackSessionId()}: " +
                      $"Calling TryGetResult for : \n\t{string.Join("\n\t", taskIds)}");

      var response = ControlPlaneService.TryGetResult(taskFilter);

      return response.Payloads.Select(x => new Tuple<string, byte[]>(x.TaskId.PackTaskId(),
                                                              x.Data.ToByteArray()));
    }

    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskId">
    ///   The task id of the task to wait for
    /// </param>
    public void WaitForTaskCompletion(string taskId)
    {
      var taskFilter = new TaskFilter();
      taskFilter.IncludedTaskIds.Add(taskId.UnPackTaskId().Task);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = taskId.UnPackTaskId().SubSession;

      Logger.LogDebug($"Wait for task {taskId} coming from Session {SessionId.Session} " +
                      $"and subSession {SessionId.SubSession} with Task SubSession {taskId.UnPackTaskId().SubSession}");

      ControlPlaneService.WaitForCompletion(new()
      {
        Filter                  = taskFilter,
        ThrowOnTaskCancellation = true,
        ThrowOnTaskError        = true,
      });
    }

    /// <summary>
    ///   Wait for the taskIds and all its dependencies taskIds
    /// </summary>
    /// <param name="parentTaskId">The taskIds to </param>
    public void WaitSubtasksCompletion(string parentTaskId)
    {
      var taskFilter = new TaskFilter();
      var taskId     = parentTaskId.CanUnPackTaskId() ? parentTaskId.UnPackTaskId().Task : parentTaskId;
      taskFilter.IncludedTaskIds.Add(taskId);
      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = parentTaskId.UnPackTaskId().SubSession;
      Logger.LogDebug($"Wait for subTask {parentTaskId} coming from Session {SessionId.Session} " +
                      $"and subSession {SessionId.SubSession} with Task SubSession {parentTaskId.UnPackTaskId().SubSession}");

      ControlPlaneService.WaitForSubTasksCompletion(new()
      {
        Filter                  = taskFilter,
        ThrowOnTaskCancellation = true,
        ThrowOnTaskError        = true,
      });
    }

    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskIds">List of taskIds
    /// </param>
    public void WaitForTasksCompletion(IEnumerable<string> taskIds)
    {
      var taskFilter = new TaskFilter();
      var ids        = taskIds.ToList();
      taskFilter.IncludedTaskIds.AddRange(ids.Select(x => x.UnPackTaskId().Task));

      taskFilter.SessionId    = SessionId.Session;
      taskFilter.SubSessionId = ids?.First().UnPackTaskId().SubSession;

      Logger.LogDebug($"Wait for task taskIds LIST coming from Session {SessionId.Session} " +
                      $"and subSession {SessionId.SubSession} with Task SubSession {ids.First().UnPackTaskId().SubSession}");

      ControlPlaneService.WaitForCompletion(new()
      {
        Filter                  = taskFilter,
        ThrowOnTaskCancellation = true,
        ThrowOnTaskError        = true,
      });
    }
  }


  /// <summary>
  ///   The SessionService Extension to single task creation
  /// </summary>
  public static class SessionServiceExt
  {
    /// <summary>
    ///   User method to submit task from the client
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="payload">
    ///   The user payload to execute.
    /// </param>
    public static string SubmitTask(this SessionService client, byte[] payload)
    {
      return client.SubmitTasks(new[] { payload })
                   .Single();
    }

    /// <summary>
    ///   The method to submit sub task coming from a parent task
    ///   Use this method only on server side development
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="parentTaskId">The task Id of a parent task</param>
    /// <param name="payloads">A lists of payloads creating a list of subTask</param>
    /// <returns>Return a list of taskId</returns>
    public static string SubmitSubTask(this SessionService client, string parentTaskId, byte[] payloads)
    {
      return client.SubmitSubTasks(parentTaskId,
                                   new[] { payloads }).Single();
    }

    /// <summary>
    ///   The method to submit One task with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created task</param>
    /// <returns>return the taskId of the created task </returns>
    public static string SubmitTaskWithDependencies(this SessionService client, byte[] payload, IList<string> dependencies)
    {
      return client.SubmitTasksWithDependencies(new[]
      {
        Tuple.Create(payload,
                     dependencies),
      }).Single();
    }

    /// <summary>
    ///   Get the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="client">The client instance for extension</param>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public static byte[] GetResult(this SessionService client, string taskId)
    {
      var results = client.GetResults(new List<string>
      {
        taskId,
      });
      Task.WaitAll(results);
      client.Logger.LogDebug($"{client.SessionId?.PackSessionId()} {client.SubSessionId?.PackSessionId()}: " +
                             $"Called GetResult for  {taskId}");

      return results.Result.Single(t => t.Item1.Equals(taskId)).Item2;
    }
  }
}