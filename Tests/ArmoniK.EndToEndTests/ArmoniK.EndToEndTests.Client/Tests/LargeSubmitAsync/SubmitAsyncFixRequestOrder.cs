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

public class SubmitAsyncFixRequestOrder
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
  public void Check_That_Batching_return_Ids_In_Good_Order_With_Unified_SDK(int nbTasks,
                                                                            int nbElementInWorkLoad,
                                                                            int bufferRequestSize,
                                                                            int maxConcurrentBuffers = 2,
                                                                            int maxParallelChannels  = 2,
                                                                            int workloadTimeInMs     = 1)
  {
    int indexTask;
    var taskIds            = new List<Task<string>>();
    var cancellationSource = new CancellationTokenSource();


    var localUnifiedTestHelper = new UnifiedTestHelper(EngineType.Unified,
                                                       ApplicationNamespace,
                                                       ApplicationService,
                                                       bufferRequestSize,
                                                       4,
                                                       4,
                                                       TimeSpan.FromMilliseconds(workloadTimeInMs));

    var service = localUnifiedTestHelper.Service as Service;
    if (service == null)
    {
      throw new NoNullAllowedException("Unified API Service is null");
    }

    var numbers = Enumerable.Range(1,
                                   nbElementInWorkLoad)
                            .Select(elem => (double)elem)
                            .ToArray();
    var taskIdExpectedResults = new List<(string taskId, double expectedResult)>();

    Enumerable.Range(0,
                     200)
              .LoopAsync(function: async (i) =>
                                   {
                                     double expectedResult = i * i * i;
                                     var myTaskId = await service.SubmitAsync("ComputeBasicArrayCube",
                                                                        UnitTestHelperBase.ParamsHelper(new double[]
                                                                                                        {
                                                                                                          i,
                                                                                                        }),
                                                                        unifiedTestHelper_,
                                                                        cancellationSource.Token);
                                     taskIdExpectedResults.Add((myTaskId, expectedResult));

                                   }).Wait(cancellationSource.Token);

    var taskResult = unifiedTestHelper_.WaitForResultcompletion(taskIdExpectedResults.Select(elem => elem.taskId));
    Assert.IsNotNull(taskResult);
    foreach (var taskIdExpectedResult in taskIdExpectedResults)
    {
      Assert.That(((double[])taskResult[taskIdExpectedResult.taskId])[0],
                  Is.EqualTo(taskIdExpectedResult.expectedResult));
    }
  }
}
