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

#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#else
#endif
using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Services;
using ArmoniK.DevelopmentKit.Client.Services.Admin;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Submitter;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Factory
{
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
    private ILogger<SessionServiceFactory> Logger { get; set; }
    private Submitter.SubmitterClient ControlPlaneService { get; set; }


    private ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// The ctor with IConfiguration and optional TaskOptions
    /// 
    /// </summary>
    /// <param name="loggerFactory">The factory to create the logger for clientService</param>
    public SessionServiceFactory(ILoggerFactory loggerFactory)
    {
      LoggerFactory = loggerFactory;
      Logger        = loggerFactory.CreateLogger<SessionServiceFactory>();
    }

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="properties">All settings to create the session</param>
    /// <returns></returns>
    public SessionService CreateSession(Properties properties)
    {
      ControlPlaneConnection(properties);

      Logger.LogDebug("Creating Session... ");

      return new SessionService(LoggerFactory,
                                ControlPlaneService,
                                properties.TaskOptions);
    }

    private void ControlPlaneConnection(Properties properties)
    {
      if (ControlPlaneService != null)
        return;


      ControlPlaneService = ClientServiceConnector.ControlPlaneConnection(properties.ConnectionString,
                                                                          properties.ClientCertFilePem,
                                                                          properties.ClientKeyFilePem,
                                                                          properties.ConfSSLValidation,
                                                                          LoggerFactory);
    }

    /// <summary>
    /// Set connection to an already opened Session
    /// </summary>
    /// <param name="properties">The properties setting for the session</param>
    /// <param name="sessionId">SessionId previously opened</param>
    /// <param name="clientOptions"></param>
    public SessionService OpenSession(Properties properties, string sessionId, IDictionary<string, string> clientOptions = null)
    {
      ControlPlaneConnection(properties);

      return new SessionService(LoggerFactory,
                                ControlPlaneService,
                                new Session()
                                {
                                  Id = sessionId,
                                },
                                clientOptions);
    }

    /// <summary>
    /// This method is creating a default taskOptions initialization where
    /// MaxDuration is 40 seconds, MaxRetries = 2 The app name is ArmoniK.DevelopmentKit.GridServer
    /// The version is 1.0.0 the namespace ArmoniK.DevelopmentKit.GridServer and simple service FallBackServerAdder 
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
        MaxRetries = 2,
        Priority   = 1,
      };

      taskOptions.Options.Add(AppsOptions.EngineTypeNameKey,
                              EngineType.DataSynapse.ToString());

      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.X.X");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridServiceNameKey,
                              "FallBackServerAdder");

      return taskOptions;
    }

    /// <summary>
    /// Return a connection interface with the control plane to manage and monitor the Armonik grid
    /// </summary>
    /// <param name="properties">The properties containing all information for connection</param>
    /// <returns>returns the services of Administration and Monitoring</returns>
    public AdminMonitoringService GetAdminMonitoringService(Properties properties)
    {
      ControlPlaneConnection(properties);

      return new AdminMonitoringService(LoggerFactory,
                                        ControlPlaneService);
    }
  }
}