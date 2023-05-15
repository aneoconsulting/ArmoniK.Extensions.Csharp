using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.LargeSubmitAsync;

public class SubmitAsyncFixRequestOrder
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckUnifiedApi";
  private const string ApplicationService   = "CheckUnifiedApiWorker";

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
  [TestCase(100,
            1,
            100,
            2,
            2,
            50)]
  [TestCase(1000,
            1,
            100,
            4,
            4,
            50)]
  public void Check_That_Batching_return_Ids_In_Good_Order_With_Unified_SDK(int nbTasks,
                                                                            int nbElementInWorkLoad,
                                                                            int bufferRequestSize,
                                                                            int maxConcurrentBuffers = 2,
                                                                            int maxParallelChannels  = 2,
                                                                            int workloadTimeInMs     = 1)
  {
    var cancellationSource = new CancellationTokenSource();


    var localUnifiedTestHelper = new UnifiedTestHelper(EngineType.Unified,
                                                       ApplicationNamespace,
                                                       ApplicationService,
                                                       bufferRequestSize,
                                                       maxConcurrentBuffers,
                                                       maxParallelChannels,
                                                       TimeSpan.FromSeconds(1));

    var service = localUnifiedTestHelper.Service as Service;
    if (service == null)
    {
      throw new NoNullAllowedException("Unified API Service is null");
    }

    var taskIdExpectedResults = new ConcurrentDictionary<string, double>();

    async Task Function(int i)
    {
      double expectedResult = i * i * i;
      var myTaskId = await service.SubmitAsync("ComputeBasicArrayCube",
                                               UnitTestHelperBase.ParamsHelper(new double[]
                                                                               {
                                                                                 i,
                                                                               }),
                                               localUnifiedTestHelper,
                                               token: cancellationSource.Token);
      taskIdExpectedResults[myTaskId] = expectedResult;
    }

    Enumerable.Range(0,
                     nbTasks)
              .LoopAsync(Function)
              .Wait(cancellationSource.Token);

    var taskResult = localUnifiedTestHelper.WaitForResultcompletion(taskIdExpectedResults.Select(elem => elem.Key));
    Assert.IsNotNull(taskResult);
    Assert.IsNotNull(taskIdExpectedResults);

    Assert.That(nbTasks == taskIdExpectedResults.Count);

    foreach (var taskIdExpectedResult in taskIdExpectedResults!)
    {
      Assert.That(((double[])taskResult[taskIdExpectedResult.Key])[0],
                  Is.EqualTo(taskIdExpectedResult.Value));
    }
  }
}
