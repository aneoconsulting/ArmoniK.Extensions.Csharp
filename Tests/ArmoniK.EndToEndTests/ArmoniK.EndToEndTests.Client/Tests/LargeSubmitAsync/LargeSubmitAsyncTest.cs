using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;
using System.Xml.Linq;

namespace ArmoniK.EndToEndTests.Client.Tests.LargeSubmitAsync;

public class LargeSubmitAsyncTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.LargePayloadSubmit";
  private const string ApplicationService   = "LargePayloadSubmitWorker";

  private UnifiedTestHelper unifiedTestHelper_;

  //[SetUp]
  //public void Setup()
  //  => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
  //                                                ApplicationNamespace,
  //                                                ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [TestCase(1, 1, 100, 500)]
  [TestCase(10, 1, 100, 500)]
  [TestCase(100, 1, 100, 500)]
  [TestCase(1000, 1, 100, 500)]
  [TestCase(10000, 1, 100, 500)]
  public void Check_That_buffering_With_SubmitAsync_Is_Working(int nbTasks,
                                                 int nbElementInWorkLoad,
                                                 int bufferRequestSize,
                                                 int workloadTimeInMs)
  {
    var localUnifiedTestHelper = new UnifiedTestHelper(EngineType.Unified,ApplicationNamespace,ApplicationService, bufferRequestSize, TimeSpan.FromMilliseconds(workloadTimeInMs));
    int indexTask;
    var taskIds = new List<Task<string>>();
    var cancellationSource = new CancellationTokenSource();

    var service = localUnifiedTestHelper.Service as ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter.Service;

    var numbers = Enumerable.Range(1, nbElementInWorkLoad).Select(elem => (double)elem).ToArray();


    for (indexTask = 0; indexTask < nbTasks; indexTask++)
    {
      taskIds.Add(service.SubmitAsync("ComputeSum",
                                      UnitTestHelperBase.ParamsHelper(numbers,
                                                   workloadTimeInMs),
                                      localUnifiedTestHelper,
                                      cancellationSource.Token));
    }
    //System.Threading.Thread.Sleep(10000);

    var taskIdsStr =  System.Threading.Tasks.Task.WhenAll(taskIds).Result;

    Assert.That(taskIds.Count, Is.EqualTo(nbTasks));

    Assert.That(taskIdsStr, Has.All.Not.Null);

    var results = localUnifiedTestHelper.WaitForResultcompletion(taskIdsStr);
    var allSumResults = results.Values.Cast<double>().ToArray();

    var expectedResult = numbers.Sum();

    Assert.IsNotNull(results);

    Assert.That(allSumResults, Has.All.EqualTo(expectedResult));
  }
}
