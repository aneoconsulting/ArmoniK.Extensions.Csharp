// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

namespace ArmoniK.Extensions.Common.StreamWrapper.Tests.Client;

internal class CreateSessionTests
{
  private Submitter.SubmitterClient client_;

  [SetUp]
  public void SetUp()
  {
    Dictionary<string, string> baseConfig = new()
    {
      { "Grpc:Endpoint", "http://localhost:5001" },
    };

    var builder              = new ConfigurationBuilder().AddInMemoryCollection(baseConfig).AddEnvironmentVariables();
    var configuration        = builder.Build();
    var configurationSection = configuration.GetSection(Options.Grpc.SettingSection);
    var endpoint             = configurationSection.GetValue<string>("Endpoint");

    Console.WriteLine($"endpoint : {endpoint}");
    var channel = GrpcChannel.ForAddress(endpoint);
    client_ = new Submitter.SubmitterClient(channel);
  }

  [Test]
  public void NullDefaultTaskOptionShouldThrowException()
  {
    var sessionId = Guid.NewGuid() + "mytestsession";

    Assert.Throws(typeof(RpcException),
                  () => client_.CreateSession(new CreateSessionRequest
                  {
                    DefaultTaskOption = null,
                    Id                = sessionId,
                  }));
  }

  [Test]
  public void EmptyIdTaskOptionShouldThrowException()
  {
    Assert.Throws(typeof(RpcException),
                  () => client_.CreateSession(new CreateSessionRequest
                  {
                    DefaultTaskOption = new TaskOptions
                    {
                      Priority    = 1,
                      MaxDuration = Duration.FromTimeSpan(TimeSpan.FromSeconds(2)),
                      MaxRetries  = 2,
                    },
                    Id = "",
                  }));
  }

  [Test]
  public void SessionShouldBeCreated()
  {
    var sessionId = Guid.NewGuid() + "mytestsession";

    var createSessionReply = client_.CreateSession(new CreateSessionRequest
    {
      DefaultTaskOption = new TaskOptions
      {
        Priority    = 1,
        MaxDuration = Duration.FromTimeSpan(TimeSpan.FromSeconds(2)),
        MaxRetries  = 2,
      },
      Id = sessionId,
    });
    Assert.AreEqual(createSessionReply.ResultCase,
                    CreateSessionReply.ResultOneofCase.Ok);
  }
}