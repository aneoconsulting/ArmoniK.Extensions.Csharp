using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.Extensions.Common.StreamWrapper.Client;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    public Session SessionId { get; private set; }


#pragma warning restore CS1591

    private ILoggerFactory LoggerFactory { get; set; }

    internal ILogger<SessionService> Logger { get; set; }

    private Submitter.SubmitterClient ControlPlaneService { get; set; }

    /// <summary>
    /// Ctor to instantiate a new SessionService
    /// This is an object to send task or get Results from a session
    /// </summary>
    public SessionService(ILoggerFactory            loggerFactory,
                          Submitter.SubmitterClient controlPlaneService,
                          TaskOptions               taskOptions = null)
    {
      Logger        = loggerFactory.CreateLogger<SessionService>();
      LoggerFactory = loggerFactory;

      taskOptions ??= InitializeDefaultTaskOptions();

      TaskOptions = CopyTaskOptionsForClient(taskOptions);

      ControlPlaneService = controlPlaneService;

      Logger.LogDebug("Creating Session... ");

      SessionId = CreateSession();

      Logger.LogDebug($"Session Created {SessionId}");
    }

    /// <summary>
    /// Create SessionService with a previous opened session
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="controlPlaneService"></param>
    /// <param name="clientOptions">Client options passed during the CreateSession</param>
    /// <param name="sessionId"></param>
    public SessionService(ILoggerFactory              loggerFactory,
                          Submitter.SubmitterClient   controlPlaneService,
                          Session                     sessionId,
                          IDictionary<string, string> clientOptions)
    {
      Logger = loggerFactory.CreateLogger<SessionService>();

      TaskOptions = CopyClientToTaskOptions(clientOptions);

      ControlPlaneService = controlPlaneService;


      Logger.LogDebug("Creating Session... ");

      SessionId = sessionId;

      Logger.LogInformation($"Session Created {SessionId} with taskOptions.Priority : {TaskOptions.Priority}");
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
      if (SessionId?.Id != null)
        return SessionId?.Id;
      else
        return "Session_Not_ready";
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
      };
      taskOptions.Options.Add(AppsOptions.EngineTypeNameKey,
                              EngineType.Symphony.ToString());

      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.Samples.SymphonyPackage");
      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.X.X");
      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.Samples.Symphony.Packages");

      return taskOptions;
    }


    private static TaskOptions CopyTaskOptionsForClient(TaskOptions taskOptions)
    {
      var res = new TaskOptions
      {
        MaxDuration = taskOptions.MaxDuration,
        MaxRetries  = taskOptions.MaxRetries,
        Priority    = taskOptions.Priority,
        Options =
        {
          ["MaxDuration"] = taskOptions.MaxDuration.Seconds.ToString(),
          ["MaxRetries"]  = taskOptions.MaxRetries.ToString(),
          ["Priority"]    = taskOptions.Priority.ToString(),
        },
      };
      taskOptions.Options.ToList()
                 .ForEach(pair => res.Options[pair.Key] = pair.Value);

      return res;
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
      };

      defaultTaskOption.Options.ToList()
                       .ForEach(pair => taskOptions.Options[pair.Key] = pair.Value);

      clientOptions.ToList()
                   .ForEach(pair => taskOptions.Options[pair.Key] = pair.Value);

      return taskOptions;
    }

    private Session CreateSession()
    {
      using var _         = Logger.LogFunction();
      var       sessionId = Guid.NewGuid().ToString();
      var createSessionRequest = new CreateSessionRequest
      {
        DefaultTaskOption = TaskOptions,
        Id                = sessionId,
      };
      var session = ControlPlaneService.CreateSession(createSessionRequest);
      switch (session.ResultCase)
      {
        case CreateSessionReply.ResultOneofCase.Error:
          throw new Exception("Error while creating session : " + session.Error);
        case CreateSessionReply.ResultOneofCase.None:
          throw new Exception("Issue with Server !");
        case CreateSessionReply.ResultOneofCase.Ok:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      return new Session()
      {
        Id = sessionId,
      };
    }

    /// <summary>
    ///   Set connection to an already opened Session
    /// </summary>
    /// <param name="session">SessionId previously opened</param>
    public void OpenSession(Session session)
    {
      if (SessionId == null) Logger.LogDebug($"Open Session {session.Id}");
      SessionId = session;
    }

    /// <summary>
    ///   Method to GetResults when the result is returned by a task
    ///   The method WaitForCompletion should called before these method
    /// </summary>
    /// <param name="taskIds">The Task Ids list of the tasks which the result is expected</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>return a dictionary with key taskId and payload</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public IEnumerable<Tuple<string, byte[]>> GetResults(IEnumerable<string> taskIds, CancellationToken cancellationToken = default)
    {
      return taskIds.Select(id =>
      {
        var res = GetResult(id,
                            cancellationToken);

        return new Tuple<string, byte[]>(id,
                                         res);
      });
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
      return SubmitTasksWithDependencies(payloads.Select(payload =>
      {
        return new Tuple<byte[], IList<string>>(payload,
                                                null);
      }));
    }

    /// <summary>
    ///   The method to submit sub task inside a parent task
    ///   Use this method only on server side development
    /// </summary>
    /// <param name="parentTaskId">The task Id of a parent task</param>
    /// <param name="payloads">A lists of payloads creating a list of subTask</param>
    /// <returns>Return a list of taskId</returns>
    [Obsolete]
    public IEnumerable<string> SubmitSubTasks(string parentTaskId, IEnumerable<byte[]> payloads)
    {
      throw new NotImplementedException("This method is obsolete please call function SubmitTasks");
    }


    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadsWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadsWithDependencies)
    {
      using var _                = Logger.LogFunction();
      var       withDependencies = payloadsWithDependencies as Tuple<byte[], IList<string>>[] ?? payloadsWithDependencies.ToArray();
      Logger.LogDebug("payload with dependencies {len}",
                      withDependencies.Count());
      var taskRequests = new List<TaskRequest>();

      foreach (var (payload, dependencies) in withDependencies)
      {
        var taskId = Guid.NewGuid().ToString();
        Logger.LogDebug("Create task {task}",
                        taskId);
        var taskRequest = new TaskRequest
        {
          Id      = taskId,
          Payload = ByteString.CopyFrom(payload),

          ExpectedOutputKeys =
          {
            taskId,
          },
        };

        if (dependencies != null && dependencies.Count != 0)
        {
          taskRequest.DataDependencies.AddRange(dependencies);

          Logger.LogDebug("Dependencies : {dep}",
                          string.Join(", ",
                                      dependencies?.Select(item => item.ToString())));
        }

        taskRequests.Add(taskRequest);
      }

      var createTaskReply = ControlPlaneService.CreateTasksAsync(SessionId.Id,
                                                                 TaskOptions,
                                                                 taskRequests).Result;
      switch (createTaskReply.DataCase)
      {
        case CreateTaskReply.DataOneofCase.NonSuccessfullIds:
          throw new Exception($"NonSuccessFullIds : {createTaskReply.NonSuccessfullIds}");
        case CreateTaskReply.DataOneofCase.None:
          throw new Exception("Issue with Server !");
        case CreateTaskReply.DataOneofCase.Successfull:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      var taskCreated = taskRequests.Select(t => t.Id);

      Logger.LogDebug("Tasks created : {ids}",
                      taskCreated);
      return taskCreated;
    }

    /// <summary>
    ///   The method to submit One SubTask with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="parentId">The parent Task who want to create the SubTask</param>
    /// <param name="payload">The payload to submit</param>
    /// <param name="dependencies">A list of task Id in dependence of this created SubTask</param>
    /// <returns>return the taskId of the created SubTask </returns>
    [Obsolete]
    public string SubmitSubtaskWithDependencies(string parentId, byte[] payload, IList<string> dependencies)
    {
      throw new NotImplementedException("This function is obsolete please use SubmitTasksWithDependencies");
    }

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="parentTaskId">The parent Task who want to create the SubTasks</param>
    /// <param name="payloadWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created Subtasks</param>
    /// <returns>return a list of taskIds of the created Subtasks </returns>
    [Obsolete]
    public IEnumerable<string> SubmitSubtasksWithDependencies(string parentTaskId, IEnumerable<Tuple<byte[], IList<string>>> payloadWithDependencies)
    {
      throw new NotImplementedException("This function is obsolete please use SubmitTasksWithDependencies");
    }


    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <param name="cancellationToken"></param>
    /// <param name="throwIfNone">Set to true if you want to set up to except when no result is received</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] GetResult(string taskId, CancellationToken cancellationToken = default)
    {
      using var _ = Logger.LogFunction(taskId);

      var res = TryGetResult(taskId,
                             cancellationToken);
      if (res.Length != 0) return res;

      var resultRequest = new ResultRequest
      {
        Key     = taskId,
        Session = SessionId.Id,
      };

      var availabilityReply = ControlPlaneService.WaitForAvailability(resultRequest,
                                                                      cancellationToken: cancellationToken);

      switch (availabilityReply.TypeCase)
      {
        case AvailabilityReply.TypeOneofCase.None:
          throw new Exception("Issue with Server !");
        case AvailabilityReply.TypeOneofCase.Ok:
          break;
        case AvailabilityReply.TypeOneofCase.Error:
          throw new Exception($"Task in Error - {taskId}\nMessage :\n{string.Join("Inner message:\n", availabilityReply.Error.Error)}");
        case AvailabilityReply.TypeOneofCase.NotCompletedTask:
          throw new DataException($"Task {taskId} was not yet completed");
        default:
          throw new ArgumentOutOfRangeException();
      }

      res = TryGetResult(taskId,
                         cancellationToken);
      if (res.Length != 0) return res;

      var taskOutput = ControlPlaneService.TryGetTaskOutput(resultRequest);

      switch (taskOutput.TypeCase)
      {
        case Output.TypeOneofCase.None:
          throw new Exception("Issue with Server !");
        case Output.TypeOneofCase.Ok:
          break;
        case Output.TypeOneofCase.Error:
          throw new Exception($"Task in Error - {taskId}\nMessage :\n{taskOutput.Error.Details}");
        default:
          throw new ArgumentOutOfRangeException();
      }

      res = TryGetResult(taskId,
                         cancellationToken);

      if (res.Length != 0) return res;
      else
      {
        throw new ArgumentException($"Cannot retrieve result for taskId {taskId}");
      }
    }

    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <param name="WaitForResult">Set to true if you want to set up to except when no result is received</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] TryGetResult(string taskId, CancellationToken cancellationToken = default, bool WaitForResult = false)
    {
      using var _ = Logger.LogFunction(taskId);
      var resultRequest = new ResultRequest
      {
        Key     = taskId,
        Session = SessionId.Id,
      };
      var result = new byte[] { };

      try
      {
        var resultReply = ControlPlaneService.TryGetResultAsync(resultRequest,
                                                                cancellationToken);
        resultReply.Wait(cancellationToken);

        return resultReply.Result;
      }
      catch (Exception ex)
      {
        Logger.LogError("Issue with TryGetResult",
                        ex);
      }

      return result;
    }

    /// <summary>
    /// Try to get result of a list of taskIds 
    /// </summary>
    /// <param name="taskIds"></param>
    /// <param name="cancellationToken">A optional default token to cancel</param>
    /// <param name="throwIfNone">Set to true if you want to set up to except when no result is received</param>
    /// <returns>Returns an Enumerable pair of </returns>
    public IEnumerable<Tuple<string, byte[]>> TryGetResults(IEnumerable<string> taskIds, CancellationToken cancellationToken = default, bool throwIfNone = false)
    {
      return taskIds.Select(id =>
      {
        var res = TryGetResult(id,
                               cancellationToken,
                               throwIfNone);

        return res.Length == 0
          ? null
          : new Tuple<string, byte[]>(id,
                                      res);
      }).Where(el => el != null);
    }

    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskId">
    ///   The task taskId of the task to wait for
    /// </param>
    public void WaitForTaskCompletion(string taskId)
    {
      using var _ = Logger.LogFunction(taskId);
      var __ = ControlPlaneService.WaitForCompletion(new WaitRequest
      {
        Filter = new TaskFilter
        {
          Task = new TaskFilter.Types.IdsRequest
          {
            Ids =
            {
              taskId,
            },
          },
        },
        StopOnFirstTaskCancellation = true,
        StopOnFirstTaskError        = true,
      });
    }

    /// <summary>
    ///   Wait for the taskIds and all its dependencies taskIds
    /// </summary>
    /// <param name="parentTaskId">The taskIds to </param>
    [Obsolete]
    public void WaitSubtasksCompletion(string parentTaskId)
    {
      using var _ = Logger.LogFunction(parentTaskId);
      var __ = ControlPlaneService.WaitForCompletion(new WaitRequest
      {
        Filter = new TaskFilter
        {
          Task = new TaskFilter.Types.IdsRequest
          {
            Ids =
            {
              parentTaskId,
            },
          },
        },
        StopOnFirstTaskCancellation = true,
        StopOnFirstTaskError        = true,
      });
    }

    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskIds">List of taskIds
    /// </param>
    public void WaitForTasksCompletion(IEnumerable<string> taskIds)
    {
      var ids = taskIds as string[] ?? taskIds.ToArray();
      using var _ = Logger.LogFunction(string.Join(", ",
                                                   ids));
      var __ = ControlPlaneService.WaitForCompletion(new WaitRequest
      {
        Filter = new TaskFilter
        {
          Task = new TaskFilter.Types.IdsRequest
          {
            Ids =
            {
              ids,
            },
          },
        },
        StopOnFirstTaskCancellation = true,
        StopOnFirstTaskError        = true,
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
    [Obsolete]
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
  }
}