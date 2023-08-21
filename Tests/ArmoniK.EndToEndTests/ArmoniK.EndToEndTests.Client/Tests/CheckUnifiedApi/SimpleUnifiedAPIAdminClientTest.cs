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
using System.Linq;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckUnifiedApi;

public class SimpleUnifiedApiAdminClientTest
{
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.CheckUnifiedApi";
  private const string ApplicationService   = "CheckUnifiedApiWorker";

  private readonly double[] numbers_ = System.Linq.Enumerable.Range(0,
                                                                    10)
                                             .Select(i => (double)i)
                                             .ToArray();

  private UnifiedTestHelper unifiedTestHelper_;

  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
                                                  ApplicationNamespace,
                                                  ApplicationService);


  [TearDown]
  public void Cleanup()
  {
  }

  [Test]
  public void Check_That_CancelSession_Is_Working()
  {
    const int wantedCount = 100;
    var tasks = unifiedTestHelper_.Service.Submit("ComputeBasicArrayCube",
                                                  System.Linq.Enumerable.Range(1,
                                                                               wantedCount)
                                                        .Select(_ => UnitTestHelperBase.ParamsHelper(numbers_)),
                                                  unifiedTestHelper_);
    if (tasks.Count() is var count && count != wantedCount)
    {
      throw new ApplicationException($"Expected {wantedCount} submitted tasks, got {count}");
    }

    unifiedTestHelper_.ServiceAdmin.AdminMonitoringService.CancelSession(unifiedTestHelper_.Service.SessionId);

    unifiedTestHelper_.WaitForResultcompletion(tasks);
    var cancelledTaskCount = unifiedTestHelper_.ServiceAdmin.AdminMonitoringService.CountTaskBySession(unifiedTestHelper_.Service.SessionId,
                                                                                                       TaskStatus.Cancelled);

    Assert.That(cancelledTaskCount,
                Is.GreaterThan(0));
  }

  [Test]
  public void Check_TaskIdListing()
  {
    const int wantedCount = 100;
    var tasks = unifiedTestHelper_.Service.Submit("ComputeBasicArrayCube",
                                                  System.Linq.Enumerable.Range(1,
                                                                               wantedCount)
                                                        .Select(_ => UnitTestHelperBase.ParamsHelper(numbers_)),
                                                  unifiedTestHelper_);
    if (tasks.Count() is var count && count != wantedCount)
    {
      throw new ApplicationException($"Expected {wantedCount} submitted tasks, got {count}");
    }

    Assert.That(((Service)unifiedTestHelper_.Service).CurrentlyHandledTaskIds,
                Is.Not.Null);

    unifiedTestHelper_.ServiceAdmin.AdminMonitoringService.CancelSession(unifiedTestHelper_.Service.SessionId);

    unifiedTestHelper_.WaitForResultcompletion(tasks);
    var cancelledTaskCount = unifiedTestHelper_.ServiceAdmin.AdminMonitoringService.CountTaskBySession(unifiedTestHelper_.Service.SessionId,
                                                                                                       TaskStatus.Cancelled);
  }
}
