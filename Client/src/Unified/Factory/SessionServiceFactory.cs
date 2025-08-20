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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Unified.Services;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Admin;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.Utils.Pool;

using Google.Protobuf.WellKnownTypes;

using Grpc.Net.Client;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArmoniK.DevelopmentKit.Client.Unified.Factory;

/// <summary>
///   The main object to communicate with the control Plane from the client side
///   The class will connect to the control plane to createSession, SubmitTask,
///   Wait for result and get the result.
///   See an example in the project ArmoniK.Samples in the sub project
///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/UnifiedAPI
///   Samples.ArmoniK.Sample.UnifiedAPI.Client
/// </summary>
[MarkDownDoc]
public class SessionServiceFactory
{
  /// <summary>
  ///   The ctor with IConfiguration and optional TaskOptions
  /// </summary>
  /// <param name="loggerFactory">The factory to create the logger for clientService</param>
  public SessionServiceFactory(ILoggerFactory? loggerFactory = null)
  {
    LoggerFactory = loggerFactory ?? new NullLoggerFactory();
    Logger        = LoggerFactory.CreateLogger<SessionServiceFactory>();
  }

  private ILogger<SessionServiceFactory> Logger { get; }

  private ObjectPool<GrpcChannel>? GrpcPool { get; set; }


  private ILoggerFactory LoggerFactory { get; }

  /// <summary>
  ///   Create the session to submit task
  /// </summary>
  /// <param name="properties">All settings to create the session</param>
  /// <returns></returns>
  public SessionService CreateSession(Properties properties)
  {
    ControlPlaneConnection(properties);

    Logger?.LogDebug("Creating Session... ");

    return new SessionService(properties,
                              LoggerFactory,
                              properties.TaskOptions);
  }

  private void ControlPlaneConnection(Properties properties)
  {
    if (GrpcPool != null)
    {
      return;
    }


    GrpcPool = ClientServiceConnector.ControlPlaneConnectionPool(properties,
                                                                 LoggerFactory);
  }

  /// <summary>
  ///   Set connection to an already opened Session
  /// </summary>
  /// <param name="properties">The properties setting for the session</param>
  /// <param name="sessionId">SessionId previously opened</param>
  /// <param name="clientOptions"></param>
  public SessionService OpenSession(Properties   properties,
                                    string       sessionId,
                                    TaskOptions? clientOptions = null)
  {
    ControlPlaneConnection(properties);

    return new SessionService(properties,
                              LoggerFactory,
                              clientOptions,
                              new Session
                              {
                                Id = sessionId,
                              });
  }

  /// <summary>
  ///   This method is creating a default taskOptions initialization where
  ///   MaxDuration is 40 seconds, MaxRetries = 2 The app name is ArmoniK.DevelopmentKit.Unified
  ///   The version is 1.0.0 the namespace ArmoniK.DevelopmentKit.Unified and simple service FallBackServerAdder
  /// </summary>
  /// <returns>Return the default taskOptions</returns>
  public static TaskOptions InitDefaultSessionOptions()
  {
    TaskOptions taskOptions = new()
                              {
                                MaxDuration = new Duration
                                              {
                                                Seconds = 40,
                                              },
                                MaxRetries           = 2,
                                Priority             = 1,
                                EngineType           = EngineType.Unified.ToString(),
                                ApplicationName      = "ArmoniK.DevelopmentKit.Worker.Unified",
                                ApplicationVersion   = "1.X.X",
                                ApplicationNamespace = "ArmoniK.DevelopmentKit.Worker.Unified",
                                ApplicationService   = "FallBackServerAdder",
                              };

    return taskOptions;
  }

  /// <summary>
  ///   Return a connection interface with the control plane to manage and monitor the Armonik grid
  /// </summary>
  /// <param name="properties">The properties containing all information for connection</param>
  /// <returns>returns the services of Administration and Monitoring</returns>
  public AdminMonitoringService GetAdminMonitoringService(Properties properties)
  {
    ControlPlaneConnection(properties);

    return new AdminMonitoringService(GrpcPool,
                                      LoggerFactory);
  }
}
