using System;
using System.Data;
using System.Linq;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckUnifiedApi;

public class SimpleUnifiedApiAdminClientTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckUnifiedApi";
  private const string ApplicationService   = "CheckUnifiedApiWorker";

  private readonly double[] numbers_ = Enumerable.Range(0,
                                                        10)
                                                 .Select(i => (double)i)
                                                 .ToArray();

  private UnifiedTestHelper? unifiedTestHelper_;

  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
                                                  ApplicationNamespace,
                                                  ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [Test]
  public void Check_That_CancelSession_Is_Working()
  {
    const int wantedCount = 100;
    var tasks = unifiedTestHelper_?.Service.Submit("ComputeBasicArrayCube",
                                                  Enumerable.Range(1,
                                                                   wantedCount)
                                                            .Select(_ => UnitTestHelperBase.ParamsHelper(numbers_)),
                                                  unifiedTestHelper_) ?? throw new NoNullAllowedException(nameof(unifiedTestHelper_));
    if (tasks != null && tasks?.Count() is var count && count != wantedCount)
    {
      throw new ApplicationException($"Expected {wantedCount} submitted tasks, got {count}");
    }

    unifiedTestHelper_!.ServiceAdmin?.AdminMonitoringService.CancelSession(unifiedTestHelper_.Service.SessionId);

    unifiedTestHelper_!.WaitForResultcompletion(tasks ?? throw new NoNullAllowedException(nameof(tasks)));
    var cancelledTaskCount = unifiedTestHelper_?.ServiceAdmin?.AdminMonitoringService.CountTaskBySession(unifiedTestHelper_.Service.SessionId,
                                                                                                       TaskStatus.Cancelled);

    Assert.That(cancelledTaskCount,
                Is.GreaterThan(0));
  }
}
