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
using System.Threading;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Common;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;

internal class BlockRequest
{
  public IServiceInvocationHandler Handler;

  public ArmonikPayload? Payload { get; set; }

  public SemaphoreSlim Lock     { get; set; }
  public Guid          SubmitId { get; set; }

  public string      ResultId    { get; set; }
  public int         MaxRetries  { get; set; } = 5;
  public TaskOptions TaskOptions { get; set; }
}
