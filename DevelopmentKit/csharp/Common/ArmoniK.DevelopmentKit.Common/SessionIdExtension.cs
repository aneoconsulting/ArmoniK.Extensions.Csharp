using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Common
{
  public static class SessionIdExtension
  {
    /// <summary>
    /// Concatenante SessionId and SubSessionId into a string
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    public static string PackId(this SessionId sessionId) => $"{sessionId.Session}#{sessionId.SubSession}";

    /// <summary>
    /// Unpack SessionId and SubSessionId
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static SessionId UnPackId(this string id)
    {
      var split = id.Split('#');
      if (split.Length != 2)
        throw new ArgumentException("Id is not a valid SessionId",
                                    nameof(id));
      return new SessionId { Session = split[0], SubSession = split[1] };
    }
  }
}