using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;

using Microsoft.Extensions.Configuration;

using System.Text.Json;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

using JetBrains.Annotations;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// This class is instanciated by ServiceFactory and allows to execute task on ArmoniK
  /// Grid.
  /// </summary>
  public class Service : IDisposable
  {
    /// <summary>
    /// Propoerty Get or Set the SessionId
    /// </summary>
    public SessionId SessionId { get; set; }

    private Dictionary<string, Task> TaskWarehouse { get; set; } = new();

    private ArmonikDataSynapseClientService ClientService { get; set; }

    private string ServiceType { get; set; }


    private ProtoSerializer ProtoSerializer { get; }

    public Service(IConfiguration configuration, string serviceType, TaskOptions taskOptions)
    {
      ClientService = new ArmonikDataSynapseClientService(configuration,
                                                          taskOptions);
      SessionId = ClientService.CreateSession(taskOptions);

      ServiceType = serviceType;

      ProtoSerializer = new ProtoSerializer();
    }

    /// <summary>
    /// This function execute code locally with the same configuration as Armonik Grid execution
    /// The method needs the Service to execute, the method name to call and arguments of method to pass
    /// </summary>
    /// <param name="service">The instance of object containing the method to call</param>
    /// <param name="methodName">The string name of the method</param>
    /// <param name="arguments">the array of object to pass as arguments for the method</param>
    /// <returns>Returns an object as result of the method call</returns>
    /// <exception cref="WorkerApiException"></exception>
    [CanBeNull]
    public object LocalExecute(object service, string methodName, object[] arguments)
    {
      byte[] payload = ProtoSerializer.SerializeMessage(new object[] { methodName, arguments });

      object[] functionData = ProtoSerializer.DeSerializeMessage(payload);


      if (functionData != null && functionData.Length >= 1)
      {
        methodName = Convert.ToString(functionData[0]);
      }

      if (methodName == null) throw new WorkerApiException("Null function");

      var methodInfo = service.GetType().GetMethod(methodName);

      object[] array = functionData?[1] as object[];

      if (methodInfo != null)
      {
        object result = methodInfo.Invoke(service,
                                          array);

        var sresult = ProtoSerializer.SerializeMessage(new object[] { result });
        var objects = new ProtoSerializer().DeSerializeMessage(sresult);

        return objects?[0];
      }

      return null;
    }

    /// <summary>
    /// This method is used to execute task and waiting after the result.
    /// the method will return the result of the execution until the grid returns the task result
    /// </summary>
    /// <param name="methodName">The string name of the method</param>
    /// <param name="arguments">the array of object to pass as arguments for the method</param>
    /// <returns>Returns an object as result of the method call</returns>
    public object Execute(string methodName, object[] arguments)
    {
      byte[] payload = ProtoSerializer.SerializeMessage(new object[] { methodName, arguments });

      DataSynapsePayload dataSynapsePayload = new()
      {
        DataSynapseRequestType = DataSynapseRequestType.Execute,
        ClientPayload          = payload
      };

      string taskId = ClientService.SubmitTask(dataSynapsePayload.Serialize());

      ClientService.WaitCompletion(taskId);
      var result = new ProtoSerializer().DeSerializeMessage(ClientService.GetResult(taskId));

      return result?[0];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="arguments"></param>
    /// <param name="handler"></param>
    public void Submit(string methodName, object[] arguments, IServiceInvocationHandler handler)
    {
      byte[] payload = ProtoSerializer.SerializeMessage(new object[] { methodName, arguments });

      DataSynapsePayload dataSynapsePayload = new()
      {
        DataSynapseRequestType = DataSynapseRequestType.Execute,
        ClientPayload          = payload
      };

      string taskId = ClientService.SubmitTask(dataSynapsePayload.Serialize());

      HandlerResponse = Task.Run(() =>
      {
        try
        {
          ClientService.WaitCompletion(taskId);
          byte[] byteResults = ClientService.GetResult(taskId);
          var    result      = new ProtoSerializer().DeSerializeMessage(ClientService.GetResult(taskId));


          handler.HandleResponse(result?[0],
                                 taskId);

        }
        catch (Exception e)
        {
          ServiceInvocationException ex = new(e);
          handler.HandleError(ex,
                              taskId);
          Console.WriteLine(e.ToString());
        }
      });

      TaskWarehouse[taskId] = HandlerResponse;
    }

    public Task HandlerResponse { get; set; }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
      ClientService?.CloseSession();
      SessionId     = null;
      ClientService = null;
      HandlerResponse?.Dispose();
    }

    public void Destroy()
    {
      Dispose();
    }

    public bool IsDestroyed()
    {
      if (SessionId == null || ClientService == null)
        return true;

      return false;
    }
  }
}