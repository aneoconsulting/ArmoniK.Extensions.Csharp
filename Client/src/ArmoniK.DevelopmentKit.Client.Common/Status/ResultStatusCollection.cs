using System;
using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   List of result status that will be collected during the request GetResultStatus
/// </summary>
public class ResultStatusCollection
{
  /// <summary>
  ///   List of completed task where the result is ready to be retrieved
  /// </summary>
  public IEnumerable<ResultStatusData> IdsReady { get; set; } = default;

  /// <summary>
  ///   List of task or task result in error
  /// </summary>
  public IEnumerable<ResultStatusData> IdsResultError { get; set; } = default;

  /// <summary>
  ///   List of Unknown TaskIds. There is a heavy error somewhere else in the execution when this list has element
  /// </summary>
  public IEnumerable<string> IdsError { get; set; } = default;

  /// <summary>
  ///   List of result not yet written in database
  /// </summary>
  public IEnumerable<ResultStatusData> IdsNotReady { get; set; }

  /// <summary>
  ///   The list of canceled task
  /// </summary>
  public IEnumerable<ResultStatusData> Canceled { get; set; }
}
