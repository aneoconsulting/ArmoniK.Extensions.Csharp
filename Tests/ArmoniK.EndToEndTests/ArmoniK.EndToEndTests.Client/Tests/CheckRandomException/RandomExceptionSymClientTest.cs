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

using System.Linq;
using System.Threading.Tasks;

using ArmoniK.EndToEndTests.Common;

using NUnit.Framework;

using Assert = NUnit.Framework.Assert;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckRandomException;

public class RandomExceptionSymClientTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckRandomException";
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
  public void Check_That_Exceptions_In_A_Session_Does_Not_Affect_The_Other_Session()
  {
    var clientPayload = new ClientPayload
                        {
                          Type = ClientPayload.TaskType.Expm1,
                        }.Serialize();

    var payloadsTasks = Enumerable.Range(1,
                                         2)
                                  .Select(_ => new Task<int>(() => SendTaskAndGetErrorCount(clientPayload)))
                                  .ToArray();
    payloadsTasks.AsParallel()
                 .ForAll(t => t.Start());

    Task.WaitAll(payloadsTasks.ToArray<Task>());

    foreach (var task in payloadsTasks)
    {
      Assert.That(task.Result,
                  Is.EqualTo(0),
                  "It seems that the retry on exception is not working properly !");
    }
  }

  public int SendTaskAndGetErrorCount(byte[] clientPayload)
  {
    var symphonyTestHelper = new SymphonyTestHelper(ApplicationNamespace,
                                                    ApplicationService);

    var payloads = Enumerable.Repeat(0,
                                     50)
                             .Select(_ => clientPayload);
    var taskIds = symphonyTestHelper.SessionService.SubmitTasks(payloads);

    //var taskResults = symphonyTestHelper.WaitForTasksResult(0, taskIds).ToList();
    var taskResults = symphonyTestHelper.WaitForTaskResults(taskIds)
                                        .ToList();

    return taskResults.Count(x => x.Item2 == null);
  }
}
