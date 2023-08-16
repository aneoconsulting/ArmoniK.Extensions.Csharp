// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
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
      taskIds.Add(service!.SubmitAsync("ComputeSum",
                                      new object[]
                                      {
                                        numbers,
                                        workloadTimeInMs,
                                      },
                                      localUnifiedTestHelper,
                                      token: cancellationSource.Token));
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
