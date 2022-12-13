using System;
using System.Linq;

using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckUnifiedApi;

public class SimpleUnifiedApiClientTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckUnifiedApi";
  private const string ApplicationService   = "CheckUnifiedApiWorker";

  private readonly double[] numbers_ = Enumerable.Range(0,
                                                        10)
                                                 .Select(i => (double)i)
                                                 .ToArray();

  private UnifiedTestHelper unifiedTestHelper_;

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
  public void Check_That_Output_As_Array_Is_Working()
  {
    var expectedResult = numbers_.Select(elem => elem * elem * elem)
                                 .ToArray();

    var taskId = unifiedTestHelper_.Service.Submit("ComputeBasicArrayCube",
                                                   UnitTestHelperBase.ParamsHelper(numbers_),
                                                   unifiedTestHelper_);

    var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(result);
    Assert.IsInstanceOf(typeof(double[]),
                        result);

    CollectionAssert.AreEqual(expectedResult,
                              (double[])result);
  }

  [Test]
  public void Check_That_Task_Options_Are_Well_Initialized_In_Worker_Size()
  {
    var expectedResult = numbers_.Select(elem => elem * elem * elem)
                                 .ToArray();

    var taskId = unifiedTestHelper_.Service.Submit("GetTaskOptionsFromWorker",
                                                   UnitTestHelperBase.ParamsHelper(),
                                                   unifiedTestHelper_);

    var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(result);
    Assert.IsInstanceOf(typeof(string),
                        result);
    var resultStr = result as string;

    Assert.That(resultStr,
                Is.EqualTo(unifiedTestHelper_.TaskOptions.ApplicationName));
  }

  [Test]
  public void Check_That_Method_Overload_As_Int_Is_Working()
  {
    var expectedResult = numbers_.Sum(elem => elem * elem * elem);

    var taskId = unifiedTestHelper_.Service.Submit("ComputeReduceCube",
                                                   UnitTestHelperBase.ParamsHelper(numbers_),
                                                   unifiedTestHelper_);

    var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(result);
    Assert.IsInstanceOf(typeof(double),
                        result);

    Assert.That(result,
                Is.EqualTo(expectedResult));
  }

  [Test]
  public void Check_That_Method_Overload_As_Byte_Array_Is_Working()
  {
    var expectedResult = numbers_.Sum(elem => elem * elem * elem);

    var taskId = unifiedTestHelper_.Service.Submit("ComputeReduceCube",
                                                   UnitTestHelperBase.ParamsHelper(numbers_.SelectMany(BitConverter.GetBytes)
                                                                                           .ToArray()),
                                                   unifiedTestHelper_);

    var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(result);
    Assert.IsInstanceOf(typeof(double),
                        result);

    Assert.That(result,
                Is.EqualTo(expectedResult));
  }

  [Test]
  public void Check_That_Statics_Methods_Can_Be_Called()
  {
    var expectedResult = numbers_.Select((x,
                                          idx) => 4 * x * numbers_[idx])
                                 .ToArray();

    var taskId = unifiedTestHelper_.Service.Submit("ComputeMadd",
                                                   UnitTestHelperBase.ParamsHelper(numbers_.SelectMany(BitConverter.GetBytes)
                                                                                           .ToArray(),
                                                                                   numbers_.SelectMany(BitConverter.GetBytes)
                                                                                           .ToArray(),
                                                                                   4.0),
                                                   unifiedTestHelper_);

    var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(result);
    Assert.IsInstanceOf(typeof(double[]),
                        result);

    Assert.That(result,
                Is.EqualTo(expectedResult));
  }

  [Test]
  public void Check_That_Instance_Methods_Can_Be_Called()
  {
    var expectedResult = numbers_.Select((x,
                                          idx) => 4 * x * numbers_[idx])
                                 .ToArray();

    var taskId = unifiedTestHelper_.Service.Submit("NonStaticComputeMadd",
                                                   UnitTestHelperBase.ParamsHelper(numbers_.SelectMany(BitConverter.GetBytes)
                                                                                           .ToArray(),
                                                                                   numbers_.SelectMany(BitConverter.GetBytes)
                                                                                           .ToArray(),
                                                                                   4.0),
                                                   unifiedTestHelper_);

    var result = unifiedTestHelper_.WaitForResultcompletion(taskId);
    Assert.IsNotNull(result);
    Assert.IsInstanceOf(typeof(double[]),
                        result);

    Assert.That(result,
                Is.EqualTo(expectedResult));
  }

  [Test]
  public void Check_That_We_Get_Exception_If_RetryCount_Is_Exceeded()
  {
    var nbTasksToSubmit = 3;
    var taskId = unifiedTestHelper_.Service.Submit("RandomTaskError",
                                                   Enumerable.Range(1,
                                                                    nbTasksToSubmit)
                                                             .Select(_ => UnitTestHelperBase.ParamsHelper(100)),
                                                   unifiedTestHelper_);

    var results = unifiedTestHelper_.WaitForResultcompletion(taskId);

    var errorCount = 0;
    foreach (var result in results.Values)
    {
      Assert.IsNotNull(result);
      Assert.That(result.GetType(),
                  Is.AnyOf(typeof(double[]),
                           typeof(ServiceInvocationException)));
      if (result.GetType() == typeof(ServiceInvocationException))
      {
        errorCount++;
      }
    }

    Assert.Greater(errorCount,
                   0);
  }
}
