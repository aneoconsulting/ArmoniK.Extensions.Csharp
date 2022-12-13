using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.Common.Timer;

/// <summary>
///   Utility class for triggering an event every 24 hours at a specified time of day
/// </summary>
public class TimerTrigger : IDisposable
{
  /// <summary>
  ///   Initiator
  /// </summary>
  /// <param name="second">The second to trigger</param>
  /// <param name="milliseconds">The milliseconds to trigger</param>
  public TimerTrigger(int second       = 0,
                      int milliseconds = 0)
  {
    var timeSpan = new TimeSpan(0,
                                0,
                                0,
                                second,
                                milliseconds);

    CancellationToken = new CancellationTokenSource();
    RunningTask = Task.Run(() =>
                           {
                             while (true)
                             {
                               Thread.Sleep(timeSpan);
                               OnTimeTriggered?.Invoke();
                             }
                           },
                           CancellationToken.Token);
  }

  /// <summary>
  ///   Time of day (from 00:00:00) to trigger
  /// </summary>
  private TimeSpan TriggerMilli { get; }

  /// <summary>
  ///   Task cancellation token source to cancel delayed task on disposal
  /// </summary>
  private CancellationTokenSource CancellationToken { get; set; }

  /// <summary>
  ///   Reference to the running task
  /// </summary>
  private Task RunningTask { get; set; }

  /// <inheritdoc />
  public void Dispose()
  {
    CancellationToken?.Cancel();
    CancellationToken?.Dispose();
    CancellationToken = null;
    RunningTask?.Dispose();
    RunningTask = null;
  }

  /// <summary>
  ///   Triggers once every 24 hours on the specified time
  /// </summary>
  public event Action OnTimeTriggered;

  /// <summary>
  ///   Finalized to ensure Dispose is called when out of scope
  /// </summary>
  ~TimerTrigger()
    => Dispose();
}
