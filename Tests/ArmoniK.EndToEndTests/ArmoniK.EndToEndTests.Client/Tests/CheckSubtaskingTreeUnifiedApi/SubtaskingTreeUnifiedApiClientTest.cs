using System.Data;
using System.Linq;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckSubtaskingTreeUnifiedApi;

public class SubtaskingTreeUnifiedApiClientTest
{
  private const string             ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckSubtaskingTreeUnifiedApi";
  private const string             ApplicationService   = "SubtaskingTreeUnifiedApiWorker";
  private       UnifiedTestHelper? unifiedTestHelper_;

  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
                                                  ApplicationNamespace,
                                                  ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [TestCase(1,
            2)]
  [TestCase(32,
            2)]
  [TestCase(128,
            4)]
  public void Check_That_Subtasking_Is_Working_With_Unified_SDK(int maxNumberToSum,
                                                                int subtaskSplitCount)
  {
    unifiedTestHelper_?.Log?.LogInformation($"Launching Sum of numbers 1 to {maxNumberToSum}");
    var numbers = Enumerable.Range(1,
                                   maxNumberToSum)
                            .ToList();
    var payload = new ClientPayload
                  {
                    IsRootTask = true,
                    Numbers    = numbers,
                    NbSubTasks = subtaskSplitCount,
                    Type       = ClientPayload.TaskType.None,
                  };

    var taskId = unifiedTestHelper_?.Service.Submit("ComputeSubTaskingTreeSum",
                                                    UnitTestHelperBase.ParamsHelper(payload.Serialize()),
                                                    unifiedTestHelper_) ?? throw new NoNullAllowedException(nameof(unifiedTestHelper_));


    var expectedResult = numbers.Sum();

    var taskResult = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(taskResult);
    Assert.IsInstanceOf(typeof(byte[]),
                        taskResult);
    var clientPayloadResult = ClientPayload.Deserialize((byte[]?)taskResult);
    Assert.That(clientPayloadResult.Result,
                Is.EqualTo(expectedResult));
  }
}
