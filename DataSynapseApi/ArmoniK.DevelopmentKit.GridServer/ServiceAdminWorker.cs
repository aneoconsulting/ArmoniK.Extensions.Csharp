using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArmoniK.DevelopmentKit.Common;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer
{
  public class ServiceAdminWorker
  {
    public Session SessionId { get; set; }
    public Dictionary<string, Task> TaskWarehouse { get; set; }

    public ArmonikDataSynapsePollingService ClientService { get; set; }

    public string ServiceType { get; set; }

    public ServiceAdminWorker(IConfiguration configuration, ILoggerFactory loggerFactory, TaskOptions taskOptions)
    {
      ClientService = new ArmonikDataSynapsePollingService(configuration,
                                                           loggerFactory,
                                                           taskOptions);
      throw new NotImplementedException("Service Admin Worker need to move into Poling agent");
    }

    public byte[] UploadResources(string path)
    {
      ArmonikPayload payload = new()
      {
        ArmonikRequestType = ArmonikRequestType.Upload
      };
      var taskId = ClientService.SubmitTask(payload.Serialize());

      throw new NotImplementedException("Service Admin Worker need to move into Poling agent");

      return new byte[] { };
    }

    public void DeployResources()
    {
      throw new NotImplementedException();
    }

    public void DeleteResources()
    {
      throw new NotImplementedException();
    }

    public void DownloadResource(string path)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<string> ListResources()
    {
      throw new NotImplementedException();
    }

    public void GetRegisteredServices()
    {
      throw new NotImplementedException();
    }

    public void RegisterService(string name)
    {
      throw new NotImplementedException();
    }

    public void UnRegisterService(string name)
    {
      throw new NotImplementedException();
    }

    public void GetServiceBinding(string name)
    {
      throw new NotImplementedException();
    }

    public void ResourceExists(string name)
    {
      throw new NotImplementedException();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
    }
  }
}