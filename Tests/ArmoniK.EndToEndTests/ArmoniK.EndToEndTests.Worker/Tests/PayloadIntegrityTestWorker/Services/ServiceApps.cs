using ArmoniK.DevelopmentKit.Worker.Unified;

namespace ArmoniK.EndToEndTests.Worker.Tests.PayloadIntegrityTestWorker.Services;

public class ServiceApps : TaskWorkerService
{
  public static string CopyPayload(string inputs)
    => inputs;
}
