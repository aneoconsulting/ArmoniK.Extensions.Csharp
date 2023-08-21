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

using System.Diagnostics;
using System.Linq;

using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckSubtaskingTree_SymphonySDK;

public class SubtaskingTreeClientTest
{
  private const string             ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckSubtaskingTree_SymphonySDK";
  private const string             ApplicationService   = "ServiceContainer";
  private       SymphonyTestHelper symphonyTestHelper_;

  [SetUp]
  public void Setup()
    => symphonyTestHelper_ = new SymphonyTestHelper(ApplicationNamespace,
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
  public void Check_That_Subtasking_Is_Working_With_Symphony_SDK(int maxNumberToSum,
                                                                 int subtaskSplitCount)
  {
    var numbers = System.Linq.Enumerable.Range(1,
                                               maxNumberToSum)
                        .ToList();
    var expectedResult = numbers.Sum(elem => (long)elem);

    var payload = new ClientPayload
                  {
                    IsRootTask = true,
                    Numbers    = numbers,
                    NbSubTasks = subtaskSplitCount,
                    Type       = ClientPayload.TaskType.None,
                  };

    var sw = Stopwatch.StartNew();

    var taskId = symphonyTestHelper_.SessionService.SubmitTask(payload.Serialize());

    var taskResult = symphonyTestHelper_.WaitForTaskResult(taskId);
    var result     = ClientPayload.Deserialize(taskResult);
    symphonyTestHelper_.Log
                       .LogInformation($"SplitAndSum {numbers.First()} ... {numbers.Last()}: Result is {result.Result} expected : {expectedResult} => {(result.Result == expectedResult ? "OK" : "NOT OK")} in {sw.ElapsedMilliseconds / 1000} sec.");

    Assert.That(result.Result,
                Is.EqualTo(expectedResult));
  }
}
