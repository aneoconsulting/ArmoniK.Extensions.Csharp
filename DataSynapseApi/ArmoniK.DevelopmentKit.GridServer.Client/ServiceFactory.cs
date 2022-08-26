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

using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// The service Factory to load service previously registered
  /// </summary>
  [MarkDownDoc]
  public class ServiceFactory
  {
    private ServiceFactory()
    {
      
    }

    private static ServiceFactory _instanceFactory;

    /// <summary>
    /// Get a single instance of ServiceFactory to create new Service
    /// </summary>
    /// <returns>Returns the ServiceFactory to create new Service</returns>
    public static ServiceFactory GetInstance()
    {
      if (_instanceFactory == null) _instanceFactory = new ServiceFactory();

      return _instanceFactory;
    }

    /// <summary>
    /// The method to create new Service
    /// </summary>
    /// <param name="serviceType">Future value no usage for now.
    /// This is the Service type reflection for method</param>
    /// <param name="props">Properties for the service containing IConfiguration and TaskOptions</param>
    /// <returns>returns the new instantiated service</returns>
    public Service CreateService(string serviceType, Properties props)
    {
      var factory = new LoggerFactory(new[]
      {
        new SerilogLoggerProvider(new LoggerConfiguration()
                                  .ReadFrom
                                  .Configuration(props.Configuration)
                                  .CreateLogger())
      });
      return new Service(serviceType,
                         factory,
                         props);
    }
  }
}