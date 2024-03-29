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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests;

internal class UnifiedTestHelper : UnitTestHelperBase, IServiceInvocationHandler
{
  private readonly ConcurrentDictionary<string, object> expectedResults_ = new();

  public UnifiedTestHelper(EngineType engineType,
                           string     applicationNamespace,
                           string     applicationService,
                           int        bufferRequestSize    = 100,
                           int        maxConcurrentBuffers = 2,
                           int        maxParallelChannels  = 2,
                           TimeSpan?  timeOut              = null)
    : base(engineType,
           applicationNamespace,
           applicationService)
    => InitService(engineType,
                   bufferRequestSize,
                   maxConcurrentBuffers,
                   maxParallelChannels,
                   timeOut);

  public ISubmitterService Service      { get; private set; }
  public ServiceAdmin      ServiceAdmin { get; private set; }

  public void HandleError(ServiceInvocationException e,
                          string                     taskId)
  {
    Log.LogError($"Error (ignore) from {taskId} : " + e.Message);
    expectedResults_[taskId] = e;
  }

  public void HandleResponse(object response,
                             string taskId)
    => expectedResults_[taskId] = response;

  public void InitService(EngineType engineType,
                          int        bufferRequestSize    = 100,
                          int        maxConcurrentBuffers = 2,
                          int        maxParallelChannels  = 2,
                          TimeSpan?  timeOut              = null)
  {
    Props.MaxConcurrentBuffers = maxConcurrentBuffers;
    Props.MaxTasksPerBuffer    = bufferRequestSize;
    Props.MaxParallelChannels  = maxParallelChannels;
    Props.TimeTriggerBuffer    = timeOut ?? Props.TimeTriggerBuffer;

    Service = ServiceFactory.CreateService(Props,
                                           LoggerFactory);
    ServiceAdmin = ServiceFactory.GetServiceAdmin(Props,
                                                  LoggerFactory);

    Log.LogInformation($"New session created : {Service.SessionId}");
  }

  internal object WaitForResultcompletion(string taskIdToWait)
    => WaitForResultcompletion(new[]
                               {
                                 taskIdToWait,
                               })
       .First()
       .Value;

  internal Dictionary<string, object> WaitForResultcompletion(IEnumerable<string> tasksIdToWait)
  {
    while (tasksIdToWait.Any(key => expectedResults_.ContainsKey(key) == false))
    {
      Thread.Sleep(1000);
    }

    return tasksIdToWait.Select(taskIdToWait => (taskIdToWait, expectedResults_[taskIdToWait]))
                        .ToDictionary(result => result.taskIdToWait,
                                      result => result.Item2);
  }
}
