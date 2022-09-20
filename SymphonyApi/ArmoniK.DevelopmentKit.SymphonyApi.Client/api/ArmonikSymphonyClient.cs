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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.SymphonyApi.Client.api;

using Grpc.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.SymphonyApi.Client;

/// <summary>
///   The main object to communicate with the control Plane from the client side
///   The class will connect to the control plane to createSession, SubmitTask,
///   Wait for result and get the result.
///   See an example in the project ArmoniK.Samples in the sub project
///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/SymphonyLike
///   Samples.ArmoniK.Sample.SymphonyClient
/// </summary>
[MarkDownDoc]
public class ArmonikSymphonyClient
{
  private readonly IConfigurationSection          controlPlanSection_;
  private readonly ILogger<ArmonikSymphonyClient> Logger;


  /// <summary>
  ///   The ctor with IConfiguration and optional TaskOptions
  /// </summary>
  /// <param name="configuration">IConfiguration to set Client Data information and Grpc EndPoint</param>
  /// <param name="loggerFactory">Factory to create logger in the client service</param>
  public ArmonikSymphonyClient(IConfiguration configuration,
                               ILoggerFactory loggerFactory)
  {
    Configuration = configuration;
    controlPlanSection_ = configuration.GetSection(SectionGrpc)
                                       .Exists()
                            ? configuration.GetSection(SectionGrpc)
                            : null;
    LoggerFactory = loggerFactory;
    Logger        = loggerFactory.CreateLogger<ArmonikSymphonyClient>();
  }

  private ILoggerFactory LoggerFactory { get; }

  /// <summary>
  ///   Returns the section key Grpc from appSettings.json
  /// </summary>
  public string SectionGrpc { get; set; } = "Grpc";

  private static string SectionEndPoint { get; } = "Endpoint";

  /// <summary>
  ///   The key to to select option in configuration
  /// </summary>
  public string SectionMTLS { get; set; } = "mTLS";

  private static string SectionSSlValidation  { get; } = "SSLValidation";
  private static string SectionClientCertFile { get; } = "ClientCert";
  private static string SectionClientKeyFile  { get; } = "ClientKey";

  private ChannelBase GrpcChannel { get; set; }


  private IConfiguration Configuration { get; }

  /// <summary>
  ///   Create the session to submit task
  /// </summary>
  /// <param name="taskOptions">Optional parameter to set TaskOptions during the Session creation</param>
  /// <returns>Returns the SessionService to submit, wait or get result</returns>
  public SessionService CreateSession(TaskOptions taskOptions = null)
  {
    ControlPlaneConnection();

    return new SessionService(LoggerFactory,
                              GrpcChannel,
                              taskOptions);
  }

  /// <summary>
  ///   Open the session already created to submit task
  /// </summary>
  /// <param name="sessionId">The sessionId string which will opened</param>
  /// <param name="clientOptions">the customer taskOptions send to the server by the client</param>
  /// <returns>Returns the SessionService to submit, wait or get result</returns>
  public SessionService OpenSession(Session     sessionId,
                                    TaskOptions clientOptions = null)
  {
    ControlPlaneConnection();

    return new SessionService(LoggerFactory,
                              GrpcChannel,
                              clientOptions ?? SessionService.InitializeDefaultTaskOptions(),
                              sessionId);
  }

  private void ControlPlaneConnection()
  {
    if (GrpcChannel != null)
    {
      return;
    }


    string clientCertFilename = null;
    string clientKeyFilename  = null;
    var    sslValidation      = true;

    if (controlPlanSection_!.GetSection(SectionMTLS)
                            .Exists() && controlPlanSection_[SectionMTLS]
          .ToLower() == "true")
    {
      if (controlPlanSection_!.GetSection(SectionClientCertFile)
                              .Exists())
      {
        clientCertFilename = controlPlanSection_[SectionClientCertFile];
      }

      if (controlPlanSection_!.GetSection(SectionClientKeyFile)
                              .Exists())
      {
        clientKeyFilename = controlPlanSection_[SectionClientKeyFile];
      }
    }

    if (controlPlanSection_!.GetSection(SectionSSlValidation)
                            .Exists() && controlPlanSection_![SectionSSlValidation] == "disable")
    {
      sslValidation = false;
    }

    GrpcChannel = ClientServiceConnector.ControlPlaneConnection(controlPlanSection_[SectionEndPoint],
                                                                clientCertFilename,
                                                                clientKeyFilename,
                                                                sslValidation,
                                                                LoggerFactory);
  }
}
