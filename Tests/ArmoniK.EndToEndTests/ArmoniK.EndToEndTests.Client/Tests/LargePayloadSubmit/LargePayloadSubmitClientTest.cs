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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.LargePayloadSubmit;

public class LargePayloadSubmitClientTest
{
  private const string            ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.LargePayloadSubmit";
  private const string            ApplicationService   = "LargePayloadSubmitWorker";
  private       UnifiedTestHelper unifiedTestHelper_;
  protected     ILoggerFactory    LoggerFactory { get; set; }

  protected IConfiguration Configuration { get; set; }

  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
                                                  ApplicationNamespace,
                                                  ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [TestCase(10,
            32000)]
  public void Check_That_Large_Payload_Submission_Is_Working(int nbTasks,
                                                             int nbElement)
  {
    const int workloadTimeInMs = 100;

    using var cancellationTokenSource = new CancellationTokenSource();
    var numbers = Enumerable.Range(0,
                                   nbElement)
                            .Select(x => (double)x)
                            .ToArray();
    var expectedResult = numbers.Sum();

    var sw      = Stopwatch.StartNew();
    var taskIds = new List<string>(nbTasks);
    for (var indexTask = 0; indexTask < nbTasks; indexTask++)
    {
      var taskId = unifiedTestHelper_.Service.Submit("ComputeSum",
                                                     UnitTestHelperBase.ParamsHelper(numbers,
                                                                                     workloadTimeInMs),
                                                     unifiedTestHelper_);
      taskIds.Add(taskId);
    }

    unifiedTestHelper_.Log.LogInformation($"{nbTasks} tasks executed in : {sw.ElapsedMilliseconds / 1000} secs with Total bytes {nbTasks * nbElement / 128} Ko");
    var results       = unifiedTestHelper_.WaitForResultcompletion(taskIds);
    var listOfResults = results.Select(elem => (double)elem.Value);

    Assert.That(listOfResults,
                Has.All.EqualTo(expectedResult));
  }
}
