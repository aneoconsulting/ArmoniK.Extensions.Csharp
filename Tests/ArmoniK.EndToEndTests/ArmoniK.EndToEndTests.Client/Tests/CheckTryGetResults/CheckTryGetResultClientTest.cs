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
using System.Threading.Tasks;

using ArmoniK.EndToEndTests.Common;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckTryGetResults;

public class CheckTryGetResultClientTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckTryGetResults";
  private const string ApplicationService   = "ServiceContainer";


  [SetUp]
  public void Setup()
  {
  }


  [TearDown]
  public void Cleanup()
  {
  }

  [Test]
  public void Check_That_We_Can_Launch_tasks_In_Multiple_Sessions_At_The_Same_Time()
  {
    var numbersToSquareReduce = System.Linq.Enumerable.Range(1,
                                                             3)
                                      .ToList();
    var clientPayload = new ClientPayload
                        {
                          Type    = ClientPayload.TaskType.ComputeCube,
                          Numbers = numbersToSquareReduce,
                        }.Serialize();
    var expectedResult = numbersToSquareReduce.Sum(x => x * x * x);
    var payloadsTasks = System.Linq.Enumerable.Range(1,
                                                     2)
                              .Select(elem => new Task<IEnumerable<ClientPayload>>(() => SendTaskAndGetPayloadResults(clientPayload)))
                              .ToArray();
    payloadsTasks.AsParallel()
                 .ForAll(t => t.Start());
    Task.WaitAll(payloadsTasks);

    foreach (var task in payloadsTasks)
    {
      var toto = task.Result;
      Assert.That(task.Result.Select(clientPayload => clientPayload.Result),
                  Has.All.EqualTo(expectedResult),
                  "It seems that the retry on exceptions is not working properly !");
    }
  }

  public IEnumerable<ClientPayload> SendTaskAndGetPayloadResults(byte[] clientPayload)
  {
    var symphonyTestHelper = new SymphonyTestHelper(ApplicationNamespace,
                                                    ApplicationService);

    var payloads = System.Linq.Enumerable.Repeat(0,
                                                 20)
                         .Select(_ => clientPayload);
    var taskIds = symphonyTestHelper.SessionService.SubmitTasks(payloads);
    var taskResults = symphonyTestHelper.WaitForTaskResults(taskIds)
                                        .ToList();
    return taskResults.Select(result => ClientPayload.Deserialize(result.Item2));
  }
}
