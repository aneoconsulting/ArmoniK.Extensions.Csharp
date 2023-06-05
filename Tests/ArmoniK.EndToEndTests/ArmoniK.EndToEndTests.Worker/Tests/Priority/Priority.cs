using System;

using ArmoniK.DevelopmentKit.Worker.Unified;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Worker.Tests.Priority;

[UsedImplicitly]
public class Priority : TaskWorkerService
{
  public byte[] GetPriority(byte[] payload)
  {
    var expected = BitConverter.ToInt32(payload);
    Logger.LogInformation($"Expected priority : {expected}, Actual : {TaskOptions.Priority}");
    return BitConverter.GetBytes(TaskOptions.Priority);
  }
}
