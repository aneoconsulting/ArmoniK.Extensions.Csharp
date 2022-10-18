using System;
using System.Collections.Generic;
using System.Linq;

namespace ArmoniK.DevelopmentKit.Common.Exceptions;

/// <summary>
///   Bundle an exception with list of task in Error
/// </summary>
public class ClientResultsException : Exception
{
  /// <summary>
  ///   The default constructor to refer the list of task in error
  /// </summary>
  /// <param name="taskIds">The list of taskId</param>
  public ClientResultsException(params string[] taskIds)
    : base(BuildMessage(taskIds))
    => TaskIds = taskIds;

  /// <summary>
  ///   The default constructor to refer the list of task in error
  /// </summary>
  /// <param name="message">the message in exception</param>
  /// <param name="taskIds">The list of taskId</param>
  public ClientResultsException(string          message,
                                params string[] taskIds)
    : base(message)
    => TaskIds = taskIds;

  /// <summary>
  ///   The default constructor to refer the list of task in error
  /// </summary>
  /// <param name="message">The string message in exception</param>
  /// <param name="taskIds"></param>
  public ClientResultsException(string              message,
                                IEnumerable<string> taskIds)
    : base(message)
    => TaskIds = taskIds;

  /// <summary>
  ///   The list of taskId in error
  /// </summary>
  public IEnumerable<string> TaskIds { get; set; }

  private static string BuildMessage(IEnumerable<string> taskIds)
  {
    var arrTaskIds = taskIds as string[] ?? taskIds.ToArray();
    var msg =
      $"The missing tasks are in error. Please check log for more information on Armonik grid server list of taskIds in Error : [ {string.Join(", ", arrTaskIds)}";

    msg += " ]";

    return msg;
  }
}
