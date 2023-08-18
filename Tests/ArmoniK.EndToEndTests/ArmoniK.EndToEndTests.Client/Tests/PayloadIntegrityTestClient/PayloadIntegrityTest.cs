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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Common;

using AutoFixture;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Serilog;

namespace ArmoniK.EndToEndTests.Client.Tests.PayloadIntegrityTestClient;

[TestFixture]
public class PayloadIntegrityTest
{
  [SetUp]
  public void Setup()
  {
    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();
    _configuration = builder.Build();
    if (_configuration.GetSection("ProxyUrl")
                      .Exists())
    {
      Environment.SetEnvironmentVariable("https_proxy",
                                         _configuration.GetSection("ProxyUrl")
                                                       .Value,
                                         EnvironmentVariableTarget.Process);
    }

    taskOptions_ = new TaskOptions
                   {
                     MaxDuration = new Duration
                                   {
                                     Seconds = 3600 * 24,
                                   },
                     MaxRetries           = 3,
                     Priority             = 1,
                     EngineType           = EngineType.Unified.ToString(),
                     ApplicationVersion   = "1.0.0-700",
                     ApplicationService   = "ServiceApps",
                     ApplicationName      = "ArmoniK.EndToEndTests.Worker",
                     ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.PayloadIntegrityTestWorker.Services",
                   };


    resultHandler_ = new ResultHandler((exception,
                                        id) =>
                                       {
                                         Assert.Fail($"taskId:[{id}] failed with exception : {exception}");
                                       },
                                       (response,
                                        id) =>
                                       {
                                         responseAndData_.TryAdd(id,
                                                                 response.ToString() ?? string.Empty);
                                       });
  }

  private          TaskOptions?                         taskOptions_;
  private          ResultHandler?                       resultHandler_;
  private readonly ConcurrentDictionary<string, string> taskAndData_     = new();
  private readonly ConcurrentDictionary<string, string> responseAndData_ = new();
  private          IConfigurationRoot                   _configuration;

  [TestCase(1,
            1,
            1)]
  [TestCase(1,
            50,
            1)]
  [TestCase(5,
            50,
            5)]
  public void CopyPayload(int maxConcurrentBuffers,
                          int maxTasksPerBuffer,
                          int maxParallelChannels)
  {
    var numberOfPayload = 100;

    var fixture = new Fixture();
    var tasks   = new List<Task>();
    var props = new Properties(_configuration,
                               taskOptions_)
                {
                  MaxConcurrentBuffers = maxConcurrentBuffers,
                  MaxTasksPerBuffer    = maxTasksPerBuffer,
                  MaxParallelChannels  = maxParallelChannels,
                };
    var service = ServiceFactory.CreateService(props);
    for (var i = 0; i < numberOfPayload; i++)
    {
      tasks.Add(NewSubmitCallAsync(fixture,
                                   service));
    }

    Task.WaitAll(tasks.ToArray());
    while (responseAndData_.Count < numberOfPayload)
    {
      Thread.Sleep(100);
    }

    CollectionAssert.AreEquivalent(taskAndData_,
                                   responseAndData_);

    service.Dispose();
    responseAndData_.Clear();
    taskAndData_.Clear();
  }

  private async Task NewSubmitCallAsync(Fixture           fixture,
                                        ISubmitterService service)
  {
    var payload = new[]
                  {
                    fixture.Create<string>(),
                  };
    try
    {
      var taskId = await service.SubmitAsync("CopyPayload",
                                             payload.ToArray(),
                                             resultHandler_);
      taskAndData_.TryAdd(taskId,
                          payload.First());
    }
    catch (Exception ex)
    {
      Log.Error("Error during the SubmitASync");
    }
  }
}
