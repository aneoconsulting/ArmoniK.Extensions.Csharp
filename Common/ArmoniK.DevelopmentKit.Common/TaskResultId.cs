using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Common
{
  /// <summary>
  ///   Data structure to hold task metadata used for retrieving tasks and results
  /// </summary>
  public class TaskResultId
  {
    public  string              SessionId;
    public  string              TaskId;
    public  IEnumerable<string> ResultIds;

  }

  public class ResultIds
  {
    public string              SessionId;
    public IEnumerable<string> Ids;

    public static implicit operator ResultIds(TaskResultId taskResultId) =>
      new()
      {
        Ids       = taskResultId.ResultIds,
        SessionId = taskResultId.SessionId,
      };

    public ResultIds(TaskResultId taskResultId)
    {
      Ids       = taskResultId.ResultIds;
      SessionId = taskResultId.SessionId;
    }

    public ResultIds()
    {
    }
  }
}
