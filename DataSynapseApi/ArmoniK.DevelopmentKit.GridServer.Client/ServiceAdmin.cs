using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;

using Microsoft.Extensions.Configuration;

using System.Text.Json;

using Microsoft.Extensions.Logging;

//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  public class ServiceAdmin : IDisposable
  {
    public SessionId SessionId { get; set; }
    public Dictionary<string, Task> TaskWarehouse { get; set; }

    public ArmonikDataSynapseClientService ClientService { get; set; }

    public string ServiceType { get; set; }

    public ServiceAdmin(IConfiguration configuration, ILoggerFactory loggerFactory, TaskOptions taskOptions)
    {
      ClientService = new ArmonikDataSynapseClientService(configuration,
                                                          loggerFactory,
                                                          taskOptions);
      SessionId = ClientService.CreateSession(taskOptions);

      ServiceType = "ServiceAdmin";
    }

    public void UploadResources(string path)
    {
      DataSynapsePayload payload = new() { DataSynapseRequestType = DataSynapseRequestType.Upload };
      string             taskId  = ClientService.SubmitTask(payload.Serialize());

      ClientService.WaitCompletion(taskId);
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