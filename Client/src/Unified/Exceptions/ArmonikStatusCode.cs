namespace ArmoniK.DevelopmentKit.Client.Unified.Exceptions;

/// <summary>
///   List of status for task and result in Armonik
/// </summary>
public enum ArmonikStatusCode
{
  /// <summary>
  ///   The task is completed but result could not be ready
  /// </summary>
  TaskCompleted,

  /// <summary>
  ///   The task has failed and no result can be expected
  /// </summary>
  TaskFailed,

  /// <summary>
  ///   The task has been canceled by operator or user himself
  /// </summary>
  TaskCanceled,

  /// <summary>
  ///   the task has reached the max duration of execution
  /// </summary>
  TaskTimeout,

  /// <summary>
  ///   The result is ready to be retrieved
  /// </summary>
  ResultReady,

  /// <summary>
  ///   The result is not yet ready and the task is still in processing
  /// </summary>
  ResultNotReady,

  /// <summary>
  ///   The result is in error and the task could finished without no result
  /// </summary>
  ResultError,

  /// <summary>
  ///   Unknown status of task or result
  /// </summary>
  Unknown,
}
