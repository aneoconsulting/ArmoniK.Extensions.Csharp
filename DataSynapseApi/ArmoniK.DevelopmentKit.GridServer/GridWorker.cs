using ArmoniK.Attributes;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Reflection;

using ArmoniK.Extensions.Common.StreamWrapper.Worker;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer
{
  [XmlDocIgnore]
  public class GridWorker : IGridWorker
  {
    private ILogger<GridWorker> Logger { get; set; }

    public ILoggerFactory LoggerFactory { get; set; }

    public GridWorker(IConfiguration configuration, ILoggerFactory factory)
    {
      Configuration = configuration;
      LoggerFactory = factory;
      Logger        = factory.CreateLogger<GridWorker>();
    }

    public IConfiguration Configuration { get; set; }

    public void Configure(IConfiguration                      configuration,
                          IReadOnlyDictionary<string, string> clientOptions,
                          IAppsLoader                         appsLoader)
    {
      Configurations       = configuration;
      ClientServiceOptions = clientOptions;


      GridAppName      = clientOptions[AppsOptions.GridAppNameKey];
      GridAppVersion   = clientOptions[AppsOptions.GridAppVersionKey];
      GridAppNamespace = clientOptions[AppsOptions.GridAppNamespaceKey];
      GridServiceName  = clientOptions[AppsOptions.GridServiceNameKey];

      ServiceClass = appsLoader.GetServiceContainerInstance<object>(GridAppNamespace,
                                                                    GridServiceName);
    }

    public object ServiceClass { get; set; }

    public string GridServiceName { get; set; }

    public string GridAppNamespace { get; set; }

    public string GridAppVersion { get; set; }

    public string GridAppName { get; set; }

    public IReadOnlyDictionary<string, string> ClientServiceOptions { get; set; }

    public IConfiguration Configurations { get; set; }

    public void InitializeSessionWorker(Session session, IReadOnlyDictionary<string, string> requestTaskOptions)
    {
      if (session == null)
        throw new ArgumentNullException($"Session is null in the Execute function");

      ServiceInvocationContext ??= new ServiceInvocationContext()
      {
        SessionId = session
      };
      Logger.BeginPropertyScope(("SessionId", session));

      TaskOptions serviceAdminTaskOptions = new()
      {
        MaxDuration = new Duration()
        {
          Seconds = 3600,
        },
        MaxRetries = 5,
        Priority   = 1,
      };
    }

    public ServiceAdminWorker ServiceAdminWorker { get; set; }

    public ServiceInvocationContext ServiceInvocationContext { get; set; }


    public byte[] Execute(ITaskHandler taskHandler)
    {
      using var _ = Logger.BeginPropertyScope(("SessionId", taskHandler.SessionId),
                                              ("TaskId", $"{taskHandler.TaskId}"));

      var payload = taskHandler.Payload;

      var dataSynapsePayload = ArmonikPayload.Deserialize(payload);

      if (dataSynapsePayload.ArmonikRequestType != ArmonikRequestType.Execute)
      {
        return RequestTypeBalancer(dataSynapsePayload);
      }

      var methodName = dataSynapsePayload.MethodName;
      if (methodName == null)
        throw new WorkerApiException($"Method name is empty in Service class [{GridAppNamespace}.{GridServiceName}]");

      var methodInfo = ServiceClass.GetType().GetMethod(methodName);
      if (methodInfo == null)
        throw new WorkerApiException($"Cannot found method [{methodName}] in Service class [{GridAppNamespace}.{GridServiceName}]");

      var arguments = dataSynapsePayload.SerializedArguments
        ? new object[] { dataSynapsePayload.ClientPayload }
        : ProtoSerializer.DeSerializeMessageObjectArray(dataSynapsePayload.ClientPayload);

      try
      {
        var result = methodInfo.Invoke(ServiceClass,
                                       arguments);
        if (result != null)
        {
          return new ProtoSerializer().SerializeMessageObjectArray(new[] { result });
        }
      }
      catch (TargetException e)
      {
        throw new WorkerApiException(e);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }
      catch (TargetInvocationException e)
      {
        throw new WorkerApiException(e.InnerException);
      }
      catch (TargetParameterCountException e)
      {
        throw new WorkerApiException(e);
      }
      catch (MethodAccessException e)
      {
        throw new WorkerApiException(e);
      }
      catch (InvalidOperationException e)
      {
        throw new WorkerApiException(e);
      }
      catch (Exception e)
      {
        throw new WorkerApiException(e);
      }

      return new byte[] { };
    }

    private byte[] RequestTypeBalancer(ArmonikPayload dataSynapsePayload)
    {
      switch (dataSynapsePayload.ArmonikRequestType)
      {
        case ArmonikRequestType.Upload:
          return ServiceAdminWorker.UploadResources("TODO");
        default:
          return new byte[] { };
      }
    }

    public void SessionFinalize()
    {
      ServiceInvocationContext = null;
    }

    public void DestroyService()
    {
      Dispose();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
      SessionFinalize();
      ServiceAdminWorker.Dispose();
    }
  }
}