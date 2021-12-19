using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Common
{
    public static class TaskIdExt
    {
      /// <summary>
      /// Concatenate TaskId and SubSessionId into a string
      /// </summary>
      /// <param name="taskId"></param>
      /// <returns></returns>
      public static string PackTaskId(this TaskId taskId) => $"{taskId.SubSession}#{taskId.Task}";

      /// <summary>
      /// Unpack TaskId and SubTaskId
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentException"></exception>
      public static TaskId UnPackTaskId(this string id)
      {
        var split = id.Split('#');
        if (split.Length != 2)
          throw new ArgumentException("Id is not a valid TaskId",
                                      nameof(id));
        return new TaskId { SubSession = split[0], Task = split[1] };
      }

      /// <summary>
      /// Unpack TaskId and SubTaskId
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentException"></exception>
      public static bool CanUnPackTaskId(this string id)
      {
        var split = id.Split('#');
        if (split.Length != 2)
          return false;

        return true;
      }
    }
}
