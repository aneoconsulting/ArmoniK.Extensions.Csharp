using ArmoniK.DevelopmentKit.Worker.Unified;

namespace ArmoniK.EndToEndTests.Worker.Tests.PayloadIntegrityTestWorker.Services;

public class ServiceApps : TaskSubmitterWorkerService
{
  public static string CopyPayload(string inputs)
    => inputs;
}
