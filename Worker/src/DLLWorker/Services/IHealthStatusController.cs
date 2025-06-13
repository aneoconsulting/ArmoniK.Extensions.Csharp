
namespace Armonik.DevelopmentKit.Worker.DLLWorker.Services;

/// <summary>
/// 
/// </summary>
public interface IHealthStatusController
{
    /// <summary>
    /// Marks the service as healthy.
    /// </summary>
    void MarkHealthy();

    /// <summary>
    /// Marks the service as unhealthy.
    /// </summary>
    void MarkUnhealthy();

    /// <summary>
    /// Checks if the service is healthy.
    /// </summary>
    /// <returns>True if the service is healthy, otherwise false.</returns>
  public  bool IsHealthy();
/// <summary>
/// 
/// </summary>
/// <returns></returns>
    string GetStatusInfo();
}