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

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.EndToEndTests.Common;

public class ClientTestHelpers
{
  public ClientTestHelpers()
  {
    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();
  }

  public static TaskOptions InitializeTaskOptions()
    => new()
       {
         MaxDuration = new Duration
                       {
                         Seconds = 300,
                       },
         MaxRetries           = 5,
         Priority             = 1,
         ApplicationName      = "ArmoniK.Samples.EndToEndTests",
         ApplicationVersion   = "1.0.0",
         ApplicationNamespace = "ArmoniK.Samples.EndToEndTests",
       };
}
