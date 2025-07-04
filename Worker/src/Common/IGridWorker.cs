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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.Worker.Common;

public interface IGridWorker : IDisposable
{
  public void Configure(IConfiguration configuration,
                        TaskOptions    clientOptions,
                        IAppsLoader    appsLoader);

  public void InitializeSessionWorker(Session     sessionId,
                                      TaskOptions requestTaskOptions);

  public byte[] Execute(ITaskHandler taskHandler);

  public void SessionFinalize();

  public void DestroyService();

  /// <summary>
  ///   Checks the health of the service.
  /// </summary>
  /// <returns>True if the service is healthy, false otherwise.</returns>
  public bool CheckHealth();
}
