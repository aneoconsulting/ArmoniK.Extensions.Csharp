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

using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckMultipleTasks;

public class MultipleTasksClientTest
{
  private const string             ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckMultipleTasks";
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

  [TestCase(1)]
  public void MultipleTasksClient(int nbTasks)
  {
    var clientPayload = new ClientPayload
                        {
                          IsRootTask = true,
                          Numbers = new List<int>
                                    {
                                      1,
                                      2,
                                      3,
                                    },
                          Type = ClientPayload.TaskType.None,
                        }.Serialize();

    var payloads = new List<byte[]>(nbTasks);
    for (var i = 0; i < nbTasks; i++)
    {
      payloads.Add(clientPayload);
    }

    var taskIds = symphonyTestHelper_.SessionService.SubmitTasks(payloads);

    var finalResult = 0;
    foreach (var taskId in taskIds)
    {
      symphonyTestHelper_.Log.LogInformation($"Client is calling {nbTasks} tasks...");
      var taskResult = symphonyTestHelper_.WaitForTaskResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      finalResult += result.Result;
    }

    Assert.That(finalResult,
                Is.EqualTo(nbTasks * 8));
  }
}
