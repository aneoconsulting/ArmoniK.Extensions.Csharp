using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common.Exceptions;
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client.api
{
  public class SessionStorage
  {
    private static SessionStorage                   _instance;

    private readonly Dictionary<string, List<string>> taskIdsFromSession_;
    
    private readonly Dictionary<string, string> sessionFromTaskIds_;

    private SessionStorage()
    {
      this.taskIdsFromSession_ = new Dictionary<string, List<string>>();
      this.sessionFromTaskIds_ = new Dictionary<string, string>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public SessionStorage GetInstance()
    {
      _instance ??= new SessionStorage();

      return _instance;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="session"></param>
    /// <param name="taskId"></param>
    public void AttachTaskToSession(string session, string taskId)
    {
      taskIdsFromSession_[session] ??= new List<string>();

      taskIdsFromSession_[session].Add(taskId);

      if (sessionFromTaskIds_.ContainsKey(taskId)) 
        throw new WorkerApiException("TaskId {} already exist");

      sessionFromTaskIds_[taskId] = session;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    /// <exception cref="WorkerApiException"></exception>
    public string GetSessionFromTaskId(string taskId)
    {
      if (!sessionFromTaskIds_.ContainsKey(taskId) || sessionFromTaskIds_[taskId] == null)
        throw new WorkerApiException($"Cannot find the taskId {taskId} in the storage");

      return sessionFromTaskIds_[taskId];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    /// <exception cref="WorkerApiException"></exception>
    public IEnumerable<string> GetTaskIdsFromSession(string session)
    {
      if (!taskIdsFromSession_.ContainsKey(session))
        throw new WorkerApiException($"Cannot find Session {session} in the storage");

      return taskIdsFromSession_[session];
    }
  }
}
