using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// This class is instantiated by ServiceFactory and allows to execute task on ArmoniK
  /// Grid.
  /// </summary>
  [MarkDownDoc]
  public class Service : IDisposable
  {
    /// <summary>
    /// Propoerty Get the SessionId
    /// </summary>
    public SessionId SessionId { get; set; }

    public Dictionary<string, Task> TaskWarehouse { get; set; } = new();

    private ArmonikDataSynapseClientService ClientService { get; set; }

    private string ServiceType { get; set; }

    private ProtoSerializer ProtoSerializer { get; }

    /// <summary>
    /// The default constructor to open connection with the control plane
    /// and create the session to ArmoniK
    /// </summary>
    /// <param name="configuration">The Iconfiguration with all parameters coming from appsettings.json or
    /// coming from Environment variables</param>
    /// <param name="serviceType"></param>
    /// <param name="taskOptions">The task parameters to set MaxDuration,
    /// MaxRetries and service which will called during the session
    /// </param>
    public Service(IConfiguration configuration, string serviceType, ILoggerFactory loggerFactory, TaskOptions taskOptions)
    {
      ClientService = new ArmonikDataSynapseClientService(configuration,
                                                          loggerFactory,
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
    /// The method submit will execute task asynchronously on the server
    /// </summary>
    /// <param name="methodName">The name of the method inside the service</param>
    /// <param name="arguments">A list of object that can be passed in parameters of the function</param>
    /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
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

    /// <summary>
    /// The method to destroy the service and close the session
    /// </summary>
    public void Destroy()
    {
      Dispose();
    }

    /// <summary>
    /// Check if this service has been destroyed before that call
    /// </summary>
    /// <returns>Returns true if the service was destroyed previously</returns>
    public bool IsDestroyed()
    {
      if (SessionId == null || ClientService == null)
        return true;

      return false;
    }
  }
}