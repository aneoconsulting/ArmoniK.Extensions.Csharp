using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.LargeSubmitAsync;

public class LargeSubmitAsyncTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.LargePayloadSubmit";
  private const string ApplicationService   = "LargePayloadSubmitWorker";

  [TearDown]
  public void Cleanup()
  {
  }

  [TestCase(1,
            1,
            100,
            2,
            2,
            50)]
  [TestCase(10,
            1,
            100,
            2,
            2,
            50)]
  [TestCase(1000,
            1,
            100,
            2,
            2,
            50)]
  public void Check_That_Buffering_With_SubmitAsync_Is_Working(int nbTasks,
                                                               int nbElementInWorkLoad,
                                                               int bufferRequestSize,
                                                               int maxConcurrentBuffers = 2,
                                                               int maxParallelChannels  = 2,
                                                               int workloadTimeInMs     = 1)
  {
    var localUnifiedTestHelper = new UnifiedTestHelper(EngineType.Unified,
                                                       ApplicationNamespace,
                                                       ApplicationService,
                                                       bufferRequestSize,
                                                       maxConcurrentBuffers,
                                                       maxParallelChannels,
                                                       TimeSpan.FromSeconds(10));

    int indexTask;
    var taskIds            = new List<Task<string>>();
    var cancellationSource = new CancellationTokenSource();

    var service = localUnifiedTestHelper.Service as Service;

    var numbers = Enumerable.Range(1,
                                   nbElementInWorkLoad)
                            .Select(elem => (double)elem)
                            .ToArray();


    for (indexTask = 0; indexTask < nbTasks; indexTask++)
    {
      taskIds.Add(service?.SubmitAsync("ComputeSum",
                                      UnitTestHelperBase.ParamsHelper(numbers,
                                                                      workloadTimeInMs),
                                      localUnifiedTestHelper,
                                      cancellationSource.Token) ?? throw new NoNullAllowedException(nameof(service)));
    }
    //System.Threading.Thread.Sleep(10000);

    var taskIdsStr = Task.WhenAll(taskIds)
                         .Result;

    Assert.That(taskIds.Count,
                Is.EqualTo(nbTasks));

    Assert.That(taskIdsStr,
                Has.All.Not.Null);

    var results = localUnifiedTestHelper.WaitForResultcompletion(taskIdsStr);
    var allSumResults = results.Values.Cast<double>()
                               .ToArray();

    var expectedResult = numbers.Sum();

    Assert.IsNotNull(results);

    Assert.That(allSumResults,
                Has.All.EqualTo(expectedResult));
  }
}
