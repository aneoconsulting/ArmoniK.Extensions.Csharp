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

using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Common;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.GridServer;

/// <summary>
///   The service Factory to load service previously registered
/// </summary>
[MarkDownDoc]
public class ServiceFactory
{
  private static ServiceFactory _instanceFactory = null!;

  private ServiceFactory()
  {
  }

  /// <summary>
  ///   Get a single instance of ServiceFactory to create new Service
  /// </summary>
  /// <returns>Returns the ServiceFactory to create new Service</returns>
  public static ServiceFactory GetInstance()
  {
    return _instanceFactory ??= new ServiceFactory();
  }

  /// <summary>
  ///   The method to create new Service
  /// </summary>
  /// <param name="serviceType">
  ///   Future value no usage for now.
  ///   This is the Service type reflection for method
  /// </param>
  /// <param name="props">Properties for the service containing IConfiguration and TaskOptions</param>
  /// <param name="loggerFactory">Logger factory to produce logs</param>
  /// <returns>returns the new instantiated service</returns>
  public Service CreateService(string                      serviceType,
                               Properties                  props,
                               [CanBeNull] ILoggerFactory? loggerFactory = null)
    => new(serviceType,
           props,
           loggerFactory);
}
