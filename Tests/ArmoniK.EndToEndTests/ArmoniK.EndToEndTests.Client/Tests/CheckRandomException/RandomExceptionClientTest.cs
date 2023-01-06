using System.Linq;

using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckRandomException;

public class RandomExceptionClientTest
{
  private const string             ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckRandomException";
  private const string             ApplicationService   = "RandomExceptionWorker";
  private       UnifiedTestHelper? unifiedTestHelper_;

  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.DataSynapse,
                                                  ApplicationNamespace,
                                                  ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [Test]
  public void Check_That_Result_Is_Correct_When_Exceptions_Are_Throwns()
  {
    var numbers = Enumerable.Range(0,
                                   10)
                            .Select(i => (double)i)
                            .ToArray();
    var expectedResult = numbers.Select(x => x * x * x)
                                .ToArray();

    for (var launchCount = 0; launchCount < 10; launchCount++)
    {
      var taskId = unifiedTestHelper_.Service.Submit("ComputeBasicArrayCube",
                                                     UnitTestHelperBase.ParamsHelper(numbers,
                                                                                     0.2),
                                                     unifiedTestHelper_);
      var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
      Assert.IsNotNull(result);
      Assert.IsInstanceOf(typeof(double[]),
                          result);
      Assert.That(result,
                  Is.EqualTo(expectedResult));
    }
  }
}
