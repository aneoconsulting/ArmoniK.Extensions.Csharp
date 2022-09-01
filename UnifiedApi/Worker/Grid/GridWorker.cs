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
using System.Linq;
using System.Reflection;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.Attributes;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Grid;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591

// ReSharper disable once CheckNamespace

namespace ArmoniK.DevelopmentKit.Worker;

[XmlDocIgnore]
public class GridWorker : IGridWorker
{
  private ServiceContext serviceContext_;

  public GridWorker(IConfiguration configuration,
                    ILoggerFactory factory)
  {
    Configuration = configuration;
    LoggerFactory = factory;
    Logger        = factory.CreateLogger<GridWorker>();
  }

  private ILogger<GridWorker> Logger { get; }

  public ILoggerFactory LoggerFactory { get; set; }

  public IConfiguration Configuration { get; set; }

  public object ServiceClass { get; set; }

  public string GridServiceName { get; set; }

  public string GridAppNamespace { get; set; }

  public string GridAppVersion { get; set; }

  public string GridAppName { get; set; }

  public IReadOnlyDictionary<string, string> ClientServiceOptions { get; set; }

  public IConfiguration Configurations { get; set; }

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

    serviceContext_ = new ServiceContext
                      {
                        ApplicationName  = GridAppName,
                        ServiceName      = GridServiceName,
                        ClientLibVersion = GridAppVersion,
                        AppNamespace     = GridAppNamespace,
                      };

    ServiceClass = appsLoader.GetServiceContainerInstance<object>(GridAppNamespace,
                                                                  GridServiceName);
  }

  public void InitializeSessionWorker(Session                             session,
                                      IReadOnlyDictionary<string, string> requestTaskOptions)
  {
    if (session == null)
    {
      throw new ArgumentNullException("Session is null in the Execute function");
    }

    Logger.BeginPropertyScope(("sessionId", session));
  }

  public byte[] Execute(ITaskHandler taskHandler)
  {
    using var _ = Logger.BeginPropertyScope(("sessionId", taskHandler.SessionId),
                                            ("taskId", $"{taskHandler.TaskId}"));

    var payload = taskHandler.Payload;

    var dataSynapsePayload = ArmonikPayload.Deserialize(payload);

    var methodName = dataSynapsePayload.MethodName;
    if (methodName == null)
    {
      throw new WorkerApiException($"Method name is empty in Service class [{GridAppNamespace}.{GridServiceName}]");
    }


    var arguments = dataSynapsePayload.SerializedArguments
                      ? new object[]
                        {
                          dataSynapsePayload.ClientPayload,
                        }
                      : ProtoSerializer.DeSerializeMessageObjectArray(dataSynapsePayload.ClientPayload);

    var methodInfo = ServiceClass.GetType()
                                 .GetMethod(methodName,
                                            arguments.Select(x => x.GetType())
                                                     .ToArray());
    if (methodInfo == null)
    {
      throw new
        WorkerApiException($"Cannot found method [{methodName}({string.Join(", ", arguments.Select(x => x.GetType().Name))})] in Service class [{GridAppNamespace}.{GridServiceName}]");
    }

    try
    {
      var result = methodInfo.Invoke(ServiceClass,
                                     arguments);
      if (result != null)
      {
        return new ProtoSerializer().SerializeMessageObjectArray(new[]
                                                                 {
                                                                   result,
                                                                 });
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


    return new byte[]
           {
           };
  }

  public void SessionFinalize()
  {
  }

  public void DestroyService()
    => Dispose();

  /// <summary>
  ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
  /// </summary>
  public void Dispose()
    => SessionFinalize();
}
