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

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
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
    private SessionService SessionService { get; set; }

    private Dictionary<string, Task> TaskWarehouse { get; set; } = new();

    private ArmonikDataSynapseClientService ClientService { get; set; }

    private ProtoSerializer ProtoSerializer { get; }

    private ILogger Logger { get; set; }

    /// <summary>
    /// The default constructor to open connection with the control plane
    /// and create the session to ArmoniK
    /// </summary>
    /// <param name="serviceType">The service type (NOT YET USED)</param>
    /// <param name="loggerFactory">The logger factory to instantiate Logger with the current class type</param>
    /// <param name="properties">The properties containing TaskOptions and information to communicate with Control plane and </param>
    public Service(string serviceType, ILoggerFactory loggerFactory, Properties properties)
    {
      ClientService = new ArmonikDataSynapseClientService(loggerFactory,
                                                          properties);
      SessionService = ClientService.CreateSession(properties.TaskOptions);

      ProtoSerializer = new ProtoSerializer();

      Logger = loggerFactory.CreateLogger<Service>();
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
      {
        throw new InvalidOperationException($"MethodName [{methodName}] was not found");
      }

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

      string taskId = SessionService.SubmitTask(dataSynapsePayload.Serialize());

      var result = ProtoSerializer.DeSerializeMessageObjectArray(SessionService.GetResult(taskId));

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
    /// <param name="dataArg">the array of byte to pass as argument for the methodName(byte[] dataArg)</param>
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

      var taskId = SessionService.SubmitTask(dataSynapsePayload.Serialize());

      var result = ProtoSerializer.DeSerializeMessageObjectArray(SessionService.GetResult(taskId));

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
      var taskId = SessionService.SubmitTask(dataSynapsePayload.Serialize());

      HandlerResponse = Task.Run(() =>
      {
        try
        {
          byte[] byteResults = ActiveGetResult(taskId);
          var    result      = ProtoSerializer.DeSerializeMessageObjectArray(byteResults);


          handler.HandleResponse(result?[0],
                                 taskId);
        }
        catch (Exception ex)
        {
          switch (ex)
          {
            case ServiceInvocationException invocationException:
              handler.HandleError(invocationException,
                                  taskId);
              break;
            case AggregateException aggregateException:
              handler.HandleError(new(aggregateException.InnerException),
                                  taskId);
              break;
            default:
              handler.HandleError(new(ex),
                                  taskId);
              break;
          }
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

    private byte[] ActiveGetResult(params string[] taskId)
    {
      return ActiveGetResults(taskId).Single().Item2;
    }

    private IEnumerable<Tuple<string, byte[]>> ActiveGetResults(IEnumerable<string> taskIds)
    {
      var missing  = taskIds.ToHashSet();
      var results  = new List<Tuple<string, byte[]>>();
      var holdPrev = 0;
      var waitInSeconds = new List<int>
      {
        10,
        1000,
        5000,
        10000,
        20000,
      };
      var idx = 0;

      Logger.BeginPropertyScope(("SessionId", SessionId),
                                ("Function", "ActiveGetResults"));

      while (missing.Count != 0)
      {
        foreach (var bucket in missing.Batch(10000))
        {
          var partialResults = SessionService.TryGetResults(bucket).ToList();

          results.AddRange(partialResults);

          missing.ExceptWith(partialResults.Select(x => x.Item1));

          Thread.Sleep(waitInSeconds[0]);
        }

        if (holdPrev == results.Count)
        {
          idx = idx >= waitInSeconds.Count - 1 ? waitInSeconds.Count - 1 : idx + 1;
          Logger.LogInformation("Result not ready. Wait for {timeWait} sec before new retry",
                                waitInSeconds[idx] / 1000);
        }
        else
        {
          idx      = 0;
          holdPrev = results.Count;
        }

        Thread.Sleep(waitInSeconds[idx]);
      }

      return results;
    }

    public Task HandlerResponse { get; set; }
    public string SessionId => SessionService?.SessionId.Id;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
      try
      {
        Task.WaitAll(TaskWarehouse.Values.ToArray());
      }
      catch (Exception)
      {
        // ignored
      }

      SessionService = null;
      ClientService  = null;
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
      if (SessionService == null || ClientService == null)
      {
        return true;
      }

      return false;
    }
  }
}