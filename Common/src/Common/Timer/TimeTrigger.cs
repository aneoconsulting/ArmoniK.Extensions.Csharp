using System;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Common.Timer;

/// <summary>
///   Utility class for triggering an event every 24 hours at a specified time of day
/// </summary>
[PublicAPI]
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

    RunningTask = Task.Run(() =>
                           {
                             while (!CancellationToken.Token.IsCancellationRequested)
                             {
                               Thread.Sleep(timeSpan);
                               OnTimeTriggered.Invoke();
                             }
                           },
                           CancellationToken.Token);
  }

  /// <summary>
  ///   Task cancellation token source to cancel delayed task on disposal
  /// </summary>
  private CancellationTokenSource CancellationToken { get; set; } = new();

  /// <summary>
  ///   Reference to the running task
  /// </summary>
  private Task RunningTask { get; set; }

  /// <inheritdoc />
  public void Dispose()
  {
    CancellationToken.Cancel();
    CancellationToken.Dispose();
    RunningTask.Dispose();
  }

  /// <summary>
  ///   Triggers once every 24 hours on the specified time
  /// </summary>
  public event Action OnTimeTriggered = delegate
                                        {
                                        };

  /// <summary>
  ///   Finalized to ensure Dispose is called when out of scope
  /// </summary>
  ~TimerTrigger()
    => Dispose();
}
