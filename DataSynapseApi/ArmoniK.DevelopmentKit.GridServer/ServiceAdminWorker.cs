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
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer;

public class ServiceAdminWorker
{
  public ServiceAdminWorker(IConfiguration configuration,
                            ILoggerFactory loggerFactory,
                            TaskOptions    taskOptions)
  {
    ClientService = new ArmonikDataSynapsePollingService(configuration,
                                                         loggerFactory,
                                                         taskOptions);
    throw new NotImplementedException("Service Admin Worker need to move into Poling agent");
  }

  public Session                  SessionId     { get; set; }
  public Dictionary<string, Task> TaskWarehouse { get; set; }

  public ArmonikDataSynapsePollingService ClientService { get; set; }

  public string ServiceType { get; set; }

  public byte[] UploadResources(string path)
  {
    ArmonikPayload payload = new()
                             {
                               ArmonikRequestType = ArmonikRequestType.Upload,
                             };
    var taskId = ClientService.SubmitTask(payload.Serialize());

    throw new NotImplementedException("Service Admin Worker need to move into Poling agent");
  }

  public void DeployResources()
    => throw new NotImplementedException();

  public void DeleteResources()
    => throw new NotImplementedException();

  public void DownloadResource(string path)
    => throw new NotImplementedException();

  public IEnumerable<string> ListResources()
    => throw new NotImplementedException();

  public void GetRegisteredServices()
    => throw new NotImplementedException();

  public void RegisterService(string name)
    => throw new NotImplementedException();

  public void UnRegisterService(string name)
    => throw new NotImplementedException();

  public void GetServiceBinding(string name)
    => throw new NotImplementedException();

  public void ResourceExists(string name)
    => throw new NotImplementedException();

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public void Dispose()
  {
  }
}
