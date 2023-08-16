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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace ArmoniK.EndToEndTests.Client.Tests.Priority;

public class Priority
{
  /// <summary>
  ///   ApplicationNamespace is the namespace of the Priority application.
  /// </summary>
  private const string ApplicationNamespace = "ArmoniK.EndToEndTests.Worker.Tests.Priority";

  /// <summary>
  ///   ApplicationService is the name of the Priority application.
  /// </summary>
  private const string ApplicationService = "Priority";

  /// <summary>
  ///   unifiedTestHelper_ is an instance of UnifiedTestHelper class.
  /// </summary>
  private UnifiedTestHelper unifiedTestHelper_;

  [SetUp]
  public void Setup()
    => unifiedTestHelper_ = new UnifiedTestHelper(EngineType.Unified,
                                                  ApplicationNamespace,
                                                  ApplicationService);

  /// <summary>
  ///   Cleanup is a method that cleans up after the test.
  /// </summary>
  [TearDown]
  public void Cleanup()
  {
  }

  [Test]
  public void TestThatPrioritiesAreAccountedFor()
  {
    var nPriorities                 = 5;
    var nTasksPerSessionPerPriority = 5;


    var builder = new ConfigurationBuilder().SetBasePath(TestContext.CurrentContext.TestDirectory)
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();


    var config = builder.Build();
    var properties = new Properties(config,
                                    new TaskOptions
                                    {
                                      MaxDuration = new Duration
                                                    {
                                                      Seconds = 300,
                                                    },
                                      MaxRetries      = 5,
                                      Priority        = 1,
                                      PartitionId     = Environment.GetEnvironmentVariable("PARTITION") ?? "",
                                      ApplicationName = "ArmoniK.EndToEndTests.Worker",
                                      ApplicationVersion = Regex.Replace(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()
                                                                                                                .Location)
                                                                                        .ProductVersion,
                                                                         @"\+.*", // Remove Hash build From Version
                                                                         "") ?? "1.0.0-700",
                                      ApplicationNamespace = ApplicationNamespace,
                                      ApplicationService   = ApplicationService,
                                      EngineType           = EngineType.Unified.ToString(),
                                    });

    var factory = new SessionServiceFactory();
    var service = factory.CreateSession(properties);


    var tasks = new Dictionary<string, int>();
    foreach (var t in Enumerable.Range(1,
                                       5))
    {
      var options = properties.TaskOptions.Clone();
      options.Priority = t;
      var payload = new ArmonikPayload
                    {
                      ClientPayload       = BitConverter.GetBytes(options.Priority),
                      MethodName          = "GetPriority",
                      SerializedArguments = true,
                    }.Serialize();
      foreach (var submitTask in service.SubmitTasks(Enumerable.Repeat(payload,
                                                                       nTasksPerSessionPerPriority),
                                                     taskOptions: options))
      {
        tasks.Add(submitTask,
                  options.Priority);
      }
    }


    var results = new List<(string, int, int)>(service.GetResults(tasks.Keys)
                                                      .Select(tuple => (tuple.Item1, tasks[tuple.Item1],
                                                                        BitConverter.ToInt32((ProtoSerializer.Deserialize<object[]>(tuple.Item2)[0] as byte[])!,
                                                                                             0))));

    foreach (var (taskId, expected, actual) in results)
    {
      unifiedTestHelper_.Log.LogInformation("Task : {0}, Expected : {1}, Actual : {2}",
                                            taskId,
                                            expected,
                                            actual);
    }

    Assert.That(results.All(r => r.Item2 == r.Item3));
  }
}
