using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Submitter;

using Microsoft.Extensions.Logging;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Client.Services.Admin
{
  /// <summary>
  /// The Administration and monitoring class to get or manage information on grid server
  /// </summary>
  public class AdminMonitoringService
  {
    private ILoggerFactory LoggerFactory { get; set; }

    private ILogger Logger { get; set; }

    /// <summary>
    /// The control plane service to request Grpc API
    /// </summary>
    private Api.gRPC.V1.Submitter.Submitter.SubmitterClient ControlPlaneService { get; set; }

    /// <summary>
    /// The constructor to instantiate this service
    /// </summary>
    /// <param name="loggerFactory">The factory logger to create logger</param>
    /// <param name="controlPlaneService">The entry point to the control plane</param>
    public AdminMonitoringService(ILoggerFactory loggerFactory, Api.gRPC.V1.Submitter.Submitter.SubmitterClient controlPlaneService)
    {
      LoggerFactory       = loggerFactory;
      Logger              = LoggerFactory.CreateLogger<AdminMonitoringService>();
      ControlPlaneService = controlPlaneService;
    }


    /// <summary>
    /// Returns the service configuration 
    /// </summary>
    public void GetServiceConfiguration()
    {
      var configuration = ControlPlaneService.GetServiceConfiguration(new Empty());

      Logger.LogInformation($"This configuration will be update in the nex version [ {configuration} ]");
    }

    /// <summary>
    /// This method can mark the session in status Canceled and
    /// mark all tasks in cancel status
    /// </summary>
    /// <param name="sessionId">the sessionId of the session to cancel</param>
    public void CancelSession(string sessionId)
    {
      ControlPlaneService.CancelSession(new Session()
      {
        Id = sessionId,
      });
    }

    /// <summary>
    /// Return the filtered list of task of a session
    /// </summary>
    /// <param name="taskFilter">The filter to apply on list of task</param>
    /// <returns>The list of filtered task </returns>
    public IEnumerable<string> ListTasks(TaskFilter taskFilter)
    {
      return ControlPlaneService.ListTasks(taskFilter).TaskIds;
    }

    /// <summary>
    /// Return the whole list of task of a session
    /// </summary>
    /// <returns>The list of filtered task </returns>
    public IEnumerable<string> ListAllTasksBySession(string sessionId)
    {
      return ControlPlaneService.ListTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId
          },
        },
      }).TaskIds;
    }

    /// <summary>
    /// Return the list of task of a session filtered by status
    /// </summary>
    /// <returns>The list of filtered task </returns>
    public IEnumerable<string> ListTasksBySession(string sessionId, params TaskStatus[] taskStatus)
    {
      return ControlPlaneService.ListTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId
          },
        },
        Included = new()
        {
          Statuses = { taskStatus },
        },
      }).TaskIds;
    }

    /// <summary>
    /// Return the list of running tasks of a session
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns>The list of filtered task </returns>
    [Obsolete]
    public IEnumerable<string> ListRunningTasks(string sessionId)
    {
      return ControlPlaneService.ListTasks(new()
      {
        Session = new()
        {
          Ids =
          {
            sessionId
          }
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Creating,
            TaskStatus.Dispatched,
            TaskStatus.Processing,
            TaskStatus.Submitted,
          }
        }
      }).TaskIds;
    }

    /// <summary>
    /// Return the list of error tasks of a session
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns>The list of filtered task </returns>
    [Obsolete]
    public IEnumerable<string> ListErrorTasks(string sessionId)
    {
      return ControlPlaneService.ListTasks(new()
      {
        Session = new()
        {
          Ids =
          {
            sessionId,
          },
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Error,
            TaskStatus.Failed,
            TaskStatus.Timeout,
          },
        }
      }).TaskIds;
    }

    /// <summary>
    /// Return the list of canceled tasks of a session
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns>The list of filtered task </returns>
    [Obsolete]
    public IEnumerable<string> ListCanceledTasks(string sessionId)
    {
      return ControlPlaneService.ListTasks(new()
      {
        Session = new()
        {
          Ids =
          {
            sessionId
          },
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Canceled,
            TaskStatus.Canceling,
          },
        },
      }).TaskIds;
    }

    /// <summary>
    /// Return the list of all sessions
    /// </summary>
    /// <returns>The list of filtered session </returns>
    public IEnumerable<string> ListAllSessions()
    {
      return ControlPlaneService.ListSessions(new()).SessionIds;
    }

    /// <summary>
    /// The method is to get a filtered list of session
    /// </summary>
    /// <param name="sessionFilter">The filter to apply on the request</param>
    /// <returns>returns a list of session filtered</returns>
    public IEnumerable<string> ListSessions(SessionFilter sessionFilter)
    {
      return ControlPlaneService.ListSessions(sessionFilter).SessionIds;
    }

    /// <summary>
    /// The method is to get a filtered list of running session
    /// </summary>
    /// <returns>returns a list of session filtered</returns>
    public IEnumerable<string> ListRunningSessions()
    {
      return ControlPlaneService.ListSessions(new SessionFilter()
      {
        Included = new()
        {
          Statuses =
          {
            SessionStatus.Running
          }
        }
      }).SessionIds;
    }

    /// <summary>
    /// The method is to get a filtered list of running session
    /// </summary>
    /// <returns>returns a list of session filtered</returns>
    public IEnumerable<string> ListCanceledSessions()
    {
      return ControlPlaneService.ListSessions(new SessionFilter()
      {
        Included = new SessionFilter.Types.StatusesRequest()
        {
          Statuses =
          {
            SessionStatus.Canceled,
          },
        },
      }).SessionIds;
    }

    /// <summary>
    /// The method is to get the number of filtered tasks
    /// </summary>
    /// <param name="taskFilter">the filter to apply on tasks</param>
    /// <returns>return the number of task</returns>
    public int CountTasks(TaskFilter taskFilter)
    {
      return ControlPlaneService.CountTasks(taskFilter).Values.Count;
    }

    /// <summary>
    /// The method is to get the number of all task in a session
    /// </summary>
    /// <returns>return the number of task</returns>
    public int CountAllTasksBySession(string sessionId)
    {
      return ControlPlaneService.CountTasks(new TaskFilter()
      {
        Session = new TaskFilter.Types.IdsRequest() {Ids = { sessionId }},
      }).Values.Count;
    }


    /// <summary>
    /// The method is to get the number of running tasks in the session
    /// </summary>
    /// <returns>return the number of task</returns>
    public int CountRunningTasksBySession(string sessionId)
    {
      return ControlPlaneService.CountTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId,
          },
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Creating,
            TaskStatus.Dispatched,
            TaskStatus.Processing,
            TaskStatus.Submitted,
          },
        },
      }).Values.Count;
    }

    /// <summary>
    /// The method is to get the number of error tasks in the session
    /// </summary>
    /// <returns>return the number of task</returns>
    public int CountErrorTasksBySession(string sessionId)
    {
      return ControlPlaneService.CountTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId,
          },
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Error,
            TaskStatus.Failed,
            TaskStatus.Timeout,
          },
        },
      }).Values.Count;
    }

    /// <summary>
    /// Count task in a session and select by status
    /// </summary>
    /// <param name="sessionId">the id of the session</param>
    /// <param name="taskStatus">a variadic list of taskStatus </param>
    /// <returns>return the number of task</returns>
    public int CountTaskBySession(string sessionId, params TaskStatus[] taskStatus)
    {
      return ControlPlaneService.CountTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId,
          },
        },
        Included = new()
        {
          Statuses = { taskStatus }
          ,
        },
      }).Values.Count;
    }

    /// <summary>
    /// The method is to get the number of error tasks in the session
    /// </summary>
    /// <returns>return the number of task</returns>
    [Obsolete]
    public int CountCancelTasksBySession(string sessionId)
    {
      return ControlPlaneService.CountTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId,
          },
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Canceling,
            TaskStatus.Canceled,
          },
        },
      }).Values.Count;
    }

    /// <summary>
    /// The method is to get the number of error tasks in the session
    /// </summary>
    /// <returns>return the number of task</returns>
    [Obsolete]
    
    public int CountCompletedTasksBySession(string sessionId)
    {
      return ControlPlaneService.CountTasks(new TaskFilter()
      {
        Session = new()
        {
          Ids =
          {
            sessionId,
          },
        },
        Included = new()
        {
          Statuses =
          {
            TaskStatus.Completed
          },
        },
      }).Values.Count;
    }

    /// <summary>
    /// Cancel a list of task in a session
    /// </summary>
    /// <param name="taskIds">the taskIds list to cancel</param>
    public void CancelTasksBySession(IEnumerable<string> taskIds)
    {
      ControlPlaneService.CancelTasks(new TaskFilter()
      {
        Task = new()
        {
          Ids =
          {
            taskIds,
          },
        },
      });
    }

    /// <summary>
    /// The method to get status of a list of tasks
    /// </summary>
    /// <param name="taskIds">The list of task</param>
    /// <returns>returns a list of pair TaskId/TaskStatus</returns>
    public IEnumerable<Tuple<string, TaskStatus>> GetTaskStatus(IEnumerable<string> taskIds)
    {
      return ControlPlaneService.GetTaskStatus(new GetTaskStatusRequest()
      {
        TaskIds =
        {
          taskIds,
        },
      }).IdStatuses.Select(idsStatus => Tuple.Create(idsStatus.TaskId,
                                                   idsStatus.Status));
    }


    private void UploadResources(string path)
    {
      throw new NotImplementedException();
    }

    private void DeployResources()
    {
      throw new NotImplementedException();
    }

    private void DeleteResources()
    {
      throw new NotImplementedException();
    }

    private void DownloadResource(string path)
    {
      throw new NotImplementedException();
    }

    private IEnumerable<string> ListResources()
    {
      throw new NotImplementedException();
    }

    private void GetRegisteredServices()
    {
      throw new NotImplementedException();
    }

    private void RegisterService(string name)
    {
      throw new NotImplementedException();
    }

    private void UnRegisterService(string name)
    {
      throw new NotImplementedException();
    }

    private void GetServiceBinding(string name)
    {
      throw new NotImplementedException();
    }

    private void ResourceExists(string name)
    {
      throw new NotImplementedException();
    }

    private string UploadResource(string filepath)
    {
      throw new NotImplementedException();
    }
  }
}