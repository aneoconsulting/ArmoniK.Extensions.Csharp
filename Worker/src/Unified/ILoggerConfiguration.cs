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

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.Worker.Unified;

/// <summary>
///   implementation of this interface in <see cref="TaskWorkerService" /> or your own implementation allows to have a
///   logger configured automatically by the <see cref="GridWorker" />
/// </summary>
public interface ILoggerConfiguration
{
  /// <summary>
  ///   The configure method is an internal call to prepare the ServiceContainer.
  ///   Its holds several configuration coming from the Client call
  /// </summary>
  /// <param name="configuration">The appSettings.json configuration prepared during the deployment</param>
  void ConfigureLogger(IConfiguration configuration);
}
