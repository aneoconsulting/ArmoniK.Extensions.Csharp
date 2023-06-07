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
using System.Linq;

using ArmoniK.EndToEndTests.Common;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckSubtaskingTree_SymphonySDK;

public class SimpleComputeNSubtaskingClientTest
{
  private const string             ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.SimpleComputeNSubtasking";
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

  [Test]
  public void Check_That_Basic_Subtasking_Is_Working_With_Symphony_SDK()
  {
    var numbers = new List<int>
                  {
                    1,
                    2,
                    3,
                  };
    var expectedResult = numbers.Sum(elem => elem * elem);

    var clientPayload = new ClientPayload
                        {
                          IsRootTask = true,
                          Numbers    = numbers,
                          Type       = ClientPayload.TaskType.ComputeSquare,
                        };
    var taskId     = symphonyTestHelper_.SessionService.SubmitTask(clientPayload.Serialize());
    var taskResult = symphonyTestHelper_.WaitForTaskResult(taskId);

    var result = ClientPayload.Deserialize(taskResult);

    Assert.That(result.Result,
                Is.EqualTo(expectedResult));
  }
}
