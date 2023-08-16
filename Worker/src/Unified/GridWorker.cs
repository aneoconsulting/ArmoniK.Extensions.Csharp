// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
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
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.Worker.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591

// ReSharper disable once CheckNamespace

namespace ArmoniK.DevelopmentKit.Worker.Unified;

[XmlDocIgnore]
public class GridWorker : IGridWorker
{
  private ServiceContext serviceContext_;
  private SessionContext sessionContext_;


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

  public TaskOptions TaskOptions { get; set; }

  public IConfiguration Configurations { get; set; }

  public Session SessionId { get; set; }


  public void Configure(IConfiguration configuration,
                        TaskOptions    clientOptions,
                        IAppsLoader    appsLoader)
  {
    Configurations = configuration;
    TaskOptions    = clientOptions.Clone();


    GridAppName      = clientOptions.ApplicationName;
    GridAppVersion   = clientOptions.ApplicationVersion;
    GridAppNamespace = clientOptions.ApplicationNamespace;
    GridServiceName  = clientOptions.ApplicationService;

    serviceContext_ = new ServiceContext
                      {
                        ApplicationName  = GridAppName,
                        ServiceName      = GridServiceName,
                        ClientLibVersion = GridAppVersion,
                        AppNamespace     = GridAppNamespace,
                      };

    ServiceClass = appsLoader.GetServiceContainerInstance<object>(GridAppNamespace,
                                                                  GridServiceName);

    if (ServiceClass is ITaskOptionsConfiguration iTaskOptionsConfiguration)
    {
      iTaskOptionsConfiguration.ConfigureTaskOptions(clientOptions);
    }

    if (ServiceClass is ILoggerConfiguration iLoggerConfiguration)
    {
      iLoggerConfiguration.ConfigureLogger(configuration);
    }

    Logger.LogDebug("Call OnCreateService");

    OnCreateService();
  }

  public void InitializeSessionWorker(Session     session,
                                      TaskOptions requestTaskOptions)
  {
    if (session == null)
    {
      throw new ArgumentNullException("Session is null in the Execute function");
    }

    Logger.BeginPropertyScope(("sessionId", session));

    if (ServiceClass is ISessionConfiguration iSessionConfiguration)
    {
      if (SessionId == null)
      {
        SessionId = session;
        iSessionConfiguration.ConfigureSession(SessionId,
                                               requestTaskOptions);
        OnSessionEnter(session);
      }
      else if (!session.Equals(SessionId))
      {
        OnSessionLeave();
        SessionId = session;
        iSessionConfiguration.ConfigureSession(SessionId,
                                               requestTaskOptions);
        OnSessionEnter(session);
      }
      else
      {
        iSessionConfiguration.ConfigureSession(SessionId,
                                               requestTaskOptions);
      }
    }
  }

  public byte[] Execute(ITaskHandler taskHandler)
  {
    using var _ = Logger.BeginPropertyScope(("sessionId", taskHandler.SessionId),
                                            ("taskId", $"{taskHandler.TaskId}"));

    var payload = taskHandler.Payload;

    var armonikPayload = ArmonikPayload.Deserialize(payload);

    var methodName = armonikPayload.MethodName;
    if (methodName == null)
    {
      throw new WorkerApiException($"Method name is empty in Service class [{GridAppNamespace}.{GridServiceName}]");
    }


    var arguments = armonikPayload.SerializedArguments
                      ? new object[]
                        {
                          armonikPayload.ClientPayload,
                        }
                      : ProtoSerializer.Deserialize<object[]>(armonikPayload.ClientPayload);

    MethodInfo methodInfo;
    if (arguments == null || arguments.Any() == false)
    {
      methodInfo = ServiceClass.GetType()
                               .GetMethod(methodName);
    }
    else
    {
      methodInfo = ServiceClass.GetType()
                               .GetMethod(methodName,
                                          arguments.Select(x => x.GetType())
                                                   .ToArray());
    }

    if (ServiceClass is ITaskContextConfiguration taskContextConfiguration)
    {
      taskContextConfiguration.TaskContext = new TaskContext
                                             {
                                               TaskId              = taskHandler.TaskId,
                                               TaskInput           = taskHandler.Payload,
                                               SessionId           = taskHandler.SessionId,
                                               DependenciesTaskIds = taskHandler.DataDependencies.Select(t => t.Key),
                                               DataDependencies    = taskHandler.DataDependencies,
                                             };
    }

    if (ServiceClass is ISessionServiceConfiguration sessionServiceConfiguration)
    {
      sessionServiceConfiguration.ConfigureSessionService(taskHandler);
    }

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
        return ProtoSerializer.Serialize(new[]
                                                           {
                                                             result,
                                                           });
      }
    }
    // Catch all exceptions from MethodBase.Invoke except TargetInvocationException (triggered by an exception in the invoked code)
    // which we want to catch higher to allow for task retry
    catch (TargetException e)
    {
      throw new WorkerApiException(e);
    }
    catch (ArgumentException e)
    {
      throw new WorkerApiException(e);
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
    catch (NotSupportedException e)
    {
      throw new WorkerApiException(e);
    }
    catch (TargetInvocationException e)
    {
      throw e.InnerException ?? e;
    }

    return null;
  }


  public void SessionFinalize()
    => OnSessionLeave();

  public void DestroyService()
  {
    OnDestroyService();
    Dispose();
  }

  /// <summary>
  ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
  /// </summary>
  public void Dispose()
    => SessionFinalize();

  public void OnCreateService()
  {
    if (ServiceClass is ISessionConfiguration iSessionConfiguration)
    {
      using (AssemblyLoadContext.EnterContextualReflection(ServiceClass.GetType()
                                                                       .Assembly))
      {
        iSessionConfiguration.OnCreateService(serviceContext_);
      }
    }
  }


  /// <summary>
  ///   The internal function onSessionEnter to openSession for clientService under GridWorker
  /// </summary>
  /// <param name="session"></param>
  public void OnSessionEnter(Session session)
  {
    sessionContext_ = new SessionContext
                      {
                        ClientLibVersion = GridAppVersion,
                        SessionId        = session.Id,
                      };
    SessionId = session;


    if (ServiceClass is ISessionConfiguration iSessionConfiguration)
    {
      iSessionConfiguration.SessionId = session;
      iSessionConfiguration.OnSessionEnter(sessionContext_);
    }
  }

  public void OnSessionLeave()
  {
    if (sessionContext_ != null)
    {
      if (ServiceClass is ISessionConfiguration iSessionConfiguration)
      {
        iSessionConfiguration.OnSessionLeave(sessionContext_);
      }

      SessionId       = null;
      sessionContext_ = null;
    }
  }

  public void OnDestroyService()
  {
    OnSessionLeave();

    if (serviceContext_ != null)
    {
      if (ServiceClass is ISessionConfiguration iSessionConfiguration)
      {
        iSessionConfiguration.OnDestroyService(serviceContext_);
      }

      serviceContext_ = null;
      SessionId       = null;
    }
  }
}
