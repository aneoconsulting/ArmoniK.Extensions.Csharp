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

using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArmoniK.DevelopmentKit.Client.Unified.Factory;

/// <summary>
///   The service Factory to load service previously registered
/// </summary>
[MarkDownDoc]
public class ServiceFactory
{
  /// <summary>
  ///   The method to create new Service
  /// </summary>
  /// <param name="props">Properties for the service containing IConfiguration and TaskOptions</param>
  /// <param name="loggerFactory">Logger factory to create loggers for service</param>
  /// <returns>returns the new instantiated service</returns>
  [PublicAPI]
  public static Service CreateService(Properties      props,
                                      ILoggerFactory? loggerFactory = null)
    => new(props,
           loggerFactory);

  /// <summary>
  ///   Method to get the ServiceAdmin
  /// </summary>
  /// <param name="props"></param>
  /// <param name="loggerFactory">Logger factory to create loggers for service</param>
  /// <returns></returns>
  [PublicAPI]
  public static ServiceAdmin GetServiceAdmin(Properties      props,
                                             ILoggerFactory? loggerFactory = null)
    => new(props,
           loggerFactory ?? new NullLoggerFactory());
}
