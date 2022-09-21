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

using ArmoniK.DevelopmentKit.Client.Services;
using ArmoniK.DevelopmentKit.Client.Services.Admin;
using ArmoniK.DevelopmentKit.Client.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Factory;

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
  /// <param name="loggerFactory"></param>
  /// <param name="cancellationTimeSpan">Time to wait cancellation in dispose method</param>
  /// <returns>returns the new instantiated service</returns>
  public static Service CreateService(Properties     props,
                                      ILoggerFactory loggerFactory,
                                      TimeSpan?      cancellationTimeSpan = null)
    => new(props,
           loggerFactory,
           cancellationTimeSpan);

  /// <summary>
  ///   Method to get the ServiceAdmin
  /// </summary>
  /// <param name="props"></param>
  /// <param name="loggerFactory"></param>
  /// <returns></returns>
  public static ServiceAdmin GetServiceAdmin(Properties     props,
                                             ILoggerFactory loggerFactory)
    => new(props,
           loggerFactory);
}
