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

using ArmoniK.DevelopmentKit.Client.Factory;
using ArmoniK.DevelopmentKit.Client.Services.Common;


namespace ArmoniK.DevelopmentKit.Client.Services.Admin
{
  /// <summary>
  /// The class to access to all Admin and monitoring API 
  /// </summary>
  public class ServiceAdmin : AbstractClientService
  {
    /// <summary>
    /// the Properties that access to the control plane
    /// </summary>
    public AdminMonitoringService AdminMonitoringService { get; set; }

    private SessionServiceFactory SessionServiceFactory { get; set; }

    /// <summary>
    /// The constructor of the service Admin class
    /// </summary>
    /// <param name="properties">the properties setting to connection to the control plane</param>
    public ServiceAdmin(Properties properties) : base(properties)
    {
      SessionServiceFactory = new(LoggerFactory);

      AdminMonitoringService = SessionServiceFactory.GetAdminMonitoringService(properties);
    }
    
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public override void Dispose()
    {
    }

  }
}