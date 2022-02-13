using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

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
    /// Class to return TaskId and the result
    /// </summary>
    public class ServiceResult
    {
      /// <summary>
      /// The getter to return the taskId
      /// </summary>
      public string TaskId { get; set; }

      /// <summary>
      /// The getter to return the result in object type format
      /// </summary>
      public object Result { get; set; }
    }

    /// <summary>
    /// Property Get the SessionId
    /// </summary>
    public SessionId SessionId { get; set; }

    public Dictionary<string, Task> TaskWarehouse { get; set; } = new();

    private ArmonikDataSynapseClientService ClientService { get; set; }

    private ProtoSerializer ProtoSerializer { get; }

    /// <summary>
    /// The default constructor to open connection with the control plane
    /// and create the session to ArmoniK
    /// </summary>
    /// <param name="configuration">The IConfiguration with all parameters coming from appsettings.json or
    /// coming from Environment variables</param>
    /// <param name="serviceType"></param>
    /// <param name="loggerFactory">The logger factory to instantiate Logger with the current class type</param>
    /// <param name="taskOptions">The task parameters to set MaxDuration,
    /// MaxRetries and service which will called during the session
    /// </param>
    public Service(IConfiguration configuration, string serviceType, ILoggerFactory loggerFactory, TaskOptions taskOptions)
    {
      ClientService = new ArmonikDataSynapseClientService(configuration,
                                                          loggerFactory,
                                                          taskOptions);
      SessionId = ClientService.CreateSession(taskOptions);

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
    public ServiceResult LocalExecute(object service, string methodName, object[] arguments)
    {
      var methodInfo = service.GetType().GetMethod(methodName);

      if (methodInfo == null)
        throw new InvalidOperationException($"MethodName [{methodName}] was not found");

      var result = methodInfo.Invoke(service,
                                     arguments);

      return new ServiceResult()
      {
        TaskId = Guid.NewGuid().ToString(),
        Result = result,
      };
    }

    /// <summary>
    /// This method is used to execute task and waiting after the result.
    /// the method will return the result of the execution until the grid returns the task result
    /// </summary>
    /// <param name="methodName">The string name of the method</param>
    /// <param name="arguments">the array of object to pass as arguments for the method</param>
    /// <returns>Returns a tuple with the taskId string and an object as result of the method call</returns>
    public ServiceResult Execute(string methodName, object[] arguments)
    {
      ArmonikPayload dataSynapsePayload = new()
      {
        ArmonikRequestType = ArmonikRequestType.Execute,
        MethodName         = methodName,
        ClientPayload      = ProtoSerializer.SerializeMessageObjectArray(arguments)
      };

      string taskId = ClientService.SubmitTask(dataSynapsePayload.Serialize());

      ClientService.WaitCompletion(taskId);
      var result = ProtoSerializer.DeSerializeMessageObjectArray(ClientService.GetResult(taskId));

      return new ServiceResult()
      {
        TaskId = taskId,
        Result = result?[0],
      };
    }

    /// <summary>
    /// This method is used to execute task and waiting after the result.
    /// the method will return the result of the execution until the grid returns the task result
    /// </summary>
    /// <param name="methodName">The string name of the method</param>
    /// <param name="arguments">the array of object to pass as arguments for the method</param>
    /// <returns>Returns a tuple with the taskId string and an object as result of the method call</returns>
    public ServiceResult Execute(string methodName, byte[] dataArg)
    {
      ArmonikPayload dataSynapsePayload = new()
      {
        ArmonikRequestType  = ArmonikRequestType.Execute,
        MethodName          = methodName,
        ClientPayload       = dataArg,
        SerializedArguments = true,
      };

      var taskId = ClientService.SubmitTask(dataSynapsePayload.Serialize());

      ClientService.WaitCompletion(taskId);
      var result = ProtoSerializer.DeSerializeMessageObjectArray(ClientService.GetResult(taskId));

      return new ServiceResult()
      {
        TaskId = taskId,
        Result = result?[0],
      };
    }

    /// <summary>
    /// The function submit where all information are already ready to send with class ArmonikPayload
    /// </summary>
    /// <param name="dataSynapsePayload">Th armonikPayload to pass with Function name and serialized arguments</param>
    /// <param name="handler">The handler callBack for Error and response</param>
    /// <returns>Return the taskId</returns>
    public string Submit(ArmonikPayload dataSynapsePayload, IServiceInvocationHandler handler)
    {
      var taskId = ClientService.SubmitTask(dataSynapsePayload.Serialize());

      HandlerResponse = Task.Run(() =>
      {
        try
        {
          ClientService.WaitCompletion(taskId);
          byte[] byteResults = ClientService.GetResult(taskId);
          var    result      = ProtoSerializer.DeSerializeMessageObjectArray(ClientService.GetResult(taskId));


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

      return taskId;
    }


    /// <summary>
    /// The method submit will execute task asynchronously on the server
    /// </summary>
    /// <param name="methodName">The name of the method inside the service</param>
    /// <param name="arguments">A list of object that can be passed in parameters of the function</param>
    /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
    /// <returns>Return the taskId string</returns>
    public string Submit(string methodName, object[] arguments, IServiceInvocationHandler handler)
    {
      ArmonikPayload dataSynapsePayload = new()
      {
        ArmonikRequestType = ArmonikRequestType.Execute,
        MethodName         = methodName,
        ClientPayload      = ProtoSerializer.SerializeMessageObjectArray(arguments)
      };

      return Submit(dataSynapsePayload,
                    handler);
    }

    /// <summary>
    /// The method submit with One serialized argument that will be already serialized for byte[] MethodName(byte[] argument).
    /// </summary>
    /// <param name="methodName">The name of the method inside the service</param>
    /// <param name="argument">One serialized argument that will already serialize for MethodName.</param>
    /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
    /// <returns>Return the taskId string</returns>
    public string Submit(string methodName, byte[] argument, IServiceInvocationHandler handler)
    {
      ArmonikPayload dataSynapsePayload = new()
      {
        ArmonikRequestType  = ArmonikRequestType.Execute,
        MethodName          = methodName,
        ClientPayload       = argument,
        SerializedArguments = true,
      };

      return Submit(dataSynapsePayload,
                    handler);
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