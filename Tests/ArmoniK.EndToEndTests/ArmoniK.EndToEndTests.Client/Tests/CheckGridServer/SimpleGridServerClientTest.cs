using System;
using System.Linq;

using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckGridServer;

public class SimpleGridServerClientTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckGridServer";
  private const string ApplicationService   = "SimpleGridServerTestWorker";

  private readonly double[] numbers_ = Enumerable.Range(0,
                                                        10)
                                                 .Select(i => (double)i)
                                                 .ToArray();

  private UnifiedTestHelper unifiedTestHelper_;

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
  public void ComputeBasicArrayCube()
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
  public void ComputeReduceCube()
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
  public void ComputeReduceCubeWithInputAsByte()
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
  public void ComputeMadd()
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
  public void NonStaticComputeMadd()
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
}
