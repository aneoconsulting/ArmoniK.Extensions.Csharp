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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.Client.Common;

/// <summary>
///   The properties class to set all configuration of the service
///   1. The connection information
///   2. The Option configuration AppSettings
///   The ssl mTLS certificate if needed to connect to the control plane
/// </summary>
[MarkDownDoc]
public class Properties
{
  /// <summary>
  ///   Returns the section key Grpc from appSettings.json
  /// </summary>
  private const string SectionGrpc = "Grpc";

  private const string SectionEndPoint           = "EndPoint";
  private const string SectionSSlValidation      = "SSLValidation";
  private const string SectionCaCert             = "CaCert";
  private const string SectionClientCert         = "ClientCert";
  private const string SectionClientKey          = "ClientKey";
  private const string SectionClientCertP12      = "ClientP12";
  private const string SectionTargetNameOverride = "EndpointNameOverride";

  private const string SectionRetryInitialBackoff    = "RetryInitialBackoff";
  private const string SectionRetryBackoffMultiplier = "RetryBackoffMultiplier";
  private const string SectionRetryMaxBackoff        = "RetryMaxBackoff";

  /// <summary>
  ///   The default configuration to submit task in a Session
  /// </summary>
  public static TaskOptions DefaultTaskOptions = new()
                                                 {
                                                   MaxDuration = new Duration
                                                                 {
                                                                   Seconds = 300000,
                                                                 },
                                                   MaxRetries = 3,
                                                   Priority   = 1,
                                                 };


  /// <summary>
  ///   The constructor to instantiate Properties object
  /// </summary>
  /// <param name="options">The taskOptions to set to a session</param>
  /// <param name="connectionAddress">The control plane address to connect</param>
  /// <param name="connectionPort">The optional port to connect to the control plane</param>
  /// <param name="protocol">the protocol https or http</param>
  /// <param name="clientCertPem">The client certificate fil in a pem format</param>
  /// <param name="clientKeyPem">The client key file in a pem format</param>
  /// <param name="clientP12">The client certificate in a P12/Pkcs12/PFX format</param>
  /// <param name="caCertPem">The Server certificate file to validate mTLS</param>
  /// <param name="sslValidation">Disable the ssl strong validation of ssl certificate (default : enable => true)</param>
  public Properties(TaskOptions options,
                    string      connectionAddress,
                    int         connectionPort = 0,
                    string      protocol       = null,
                    string      clientCertPem  = null,
                    string      clientKeyPem   = null,
                    string      clientP12      = null,
                    string      caCertPem      = null,
                    bool?       sslValidation  = null)
    : this(new ConfigurationBuilder().AddEnvironmentVariables()
                                     .Build(),
           options,
           connectionAddress,
           connectionPort,
           protocol,
           clientCertPem,
           clientKeyPem,
           clientP12,
           caCertPem,
           sslValidation)
  {
  }

  /// <summary>
  ///   The constructor to instantiate Properties object
  /// </summary>
  /// <param name="configuration">The configuration to read information from AppSettings file</param>
  /// <param name="options">The taskOptions to set to a session</param>
  /// <param name="connectionAddress">The control plane address to connect</param>
  /// <param name="connectionPort">The optional port to connect to the control plane</param>
  /// <param name="protocol">the protocol https or http</param>
  /// <param name="caCertPem">The Server certificate file to validate mTLS</param>
  /// <param name="clientCertFilePem">The client certificate fil in a pem format</param>
  /// <param name="clientKeyFilePem">The client key file in a pem format</param>
  /// <param name="clientP12">The client certificate in a P12/Pkcs12/PFX format</param>
  /// <param name="sslValidation">Disable the ssl strong validation of ssl certificate (default : enable => true)</param>
  /// <param name="retryInitialBackoff">Initial retry backoff delay</param>
  /// <param name="retryBackoffMultiplier">Retry backoff multiplier</param>
  /// <param name="retryMaxBackoff">Max retry backoff</param>
  /// <exception cref="ArgumentException"></exception>
  public Properties(IConfiguration configuration,
                    TaskOptions    options,
                    string         connectionAddress      = null,
                    int            connectionPort         = 0,
                    string         protocol               = null,
                    string         clientCertFilePem      = null,
                    string         clientKeyFilePem       = null,
                    string         clientP12              = null,
                    string         caCertPem              = null,
                    bool?          sslValidation          = null,
                    TimeSpan       retryInitialBackoff    = new(),
                    double         retryBackoffMultiplier = 0,
                    TimeSpan       retryMaxBackoff        = new())
  {
    TaskOptions   = options;
    Configuration = configuration;

    var sectionGrpc = configuration.GetSection(SectionGrpc)
                                   .Exists()
                        ? configuration.GetSection(SectionGrpc)
                        : null;

    if (connectionAddress != null)
    {
      var uri = new Uri(connectionAddress);
      ConnectionAddress = uri.Host;

      if (!string.IsNullOrEmpty(uri.Scheme))
      {
        Protocol = uri.Scheme;
      }
    }
    else
    {
      ConnectionString = sectionGrpc?[SectionEndPoint];
    }

    Protocol = protocol ?? Protocol;

    ConfSSLValidation  = sslValidation ?? sectionGrpc?[SectionSSlValidation] != "disable";
    TargetNameOverride = sectionGrpc?[SectionTargetNameOverride];
    CaCertFilePem      = caCertPem         ?? sectionGrpc?[SectionCaCert];
    ClientCertFilePem  = clientCertFilePem ?? sectionGrpc?[SectionClientCert];
    ClientKeyFilePem   = clientKeyFilePem  ?? sectionGrpc?[SectionClientKey];
    ClientP12File      = clientP12         ?? sectionGrpc?[SectionClientCertP12];

    if (retryInitialBackoff != TimeSpan.Zero)
    {
      RetryInitialBackoff = retryInitialBackoff;
    }
    else if (!string.IsNullOrWhiteSpace(sectionGrpc?[SectionRetryInitialBackoff]))
    {
      RetryInitialBackoff = TimeSpan.Parse(sectionGrpc[SectionRetryInitialBackoff]);
    }

    if (retryBackoffMultiplier != 0)
    {
      RetryBackoffMultiplier = retryBackoffMultiplier;
    }
    else if (!string.IsNullOrWhiteSpace(sectionGrpc?[SectionRetryBackoffMultiplier]))
    {
      RetryBackoffMultiplier = double.Parse(sectionGrpc[SectionRetryBackoffMultiplier]);
    }


    if (retryMaxBackoff != TimeSpan.Zero)
    {
      RetryMaxBackoff = retryMaxBackoff;
    }
    else if (!string.IsNullOrWhiteSpace(sectionGrpc?[SectionRetryMaxBackoff]))
    {
      RetryMaxBackoff = TimeSpan.Parse(sectionGrpc[SectionRetryMaxBackoff]);
    }


    if (connectionPort != 0)
    {
      ConnectionPort = connectionPort;
    }

    //Check if Uri is correct
    if (string.IsNullOrEmpty(Protocol) || string.IsNullOrEmpty(ConnectionAddress) || ConnectionPort == 0)
    {
      throw new ArgumentException($"Issue with the connection point : {ConnectionString}");
    }

    ControlPlaneUri = new Uri(ConnectionString!);
  }

  /// <summary>
  ///   Set the number of task by buffer
  /// </summary>
  public int MaxTasksPerBuffer { get; set; } = 500;


  /// <summary>
  ///   Set the number of buffers that can be filled in asynchronous submitAsync
  /// </summary>
  public int MaxConcurrentBuffers { get; set; } = 1;


  /// <summary>
  ///   TimeSpan to trigger a batch to send the batch of submit
  /// </summary>
  public TimeSpan? TimeTriggerBuffer { get; set; } = TimeSpan.FromSeconds(10);

  /// <summary>
  ///   The number of channels used for Buffered Submit (Default 1)
  /// </summary>
  public int MaxParallelChannels { get; set; } = 1;

  /// <summary>
  ///   The control plane url to connect
  /// </summary>
  public Uri ControlPlaneUri { get; set; }

  /// <summary>
  ///   The path to the CA Root file name
  /// </summary>
  public string CaCertFilePem { get; set; }

  /// <summary>
  ///   The property to get the path of the certificate file
  /// </summary>
  public string ClientCertFilePem { get; }

  /// <summary>
  ///   the property to get the path of the key certificate
  /// </summary>
  public string ClientKeyFilePem { get; }

  /// <summary>
  ///   the property to get the path of the certificate in P12/Pkcs12/PFX format
  /// </summary>
  public string ClientP12File { get; }

  /// <summary>
  ///   The SSL validation property to disable SSL strong verification
  /// </summary>
  public bool ConfSSLValidation { get; }

  /// <summary>
  ///   The configuration property to give to the ClientService connector
  /// </summary>
  public IConfiguration Configuration { get; }

  /// <summary>
  ///   The connection string building the value Port Protocol and address
  /// </summary>
  public string ConnectionString
  {
    get => $"{Protocol}://{ConnectionAddress}:{ConnectionPort}";
    set
    {
      try
      {
        if (string.IsNullOrEmpty(value))
        {
          return;
        }

        var uri = new Uri(value);

        Protocol = uri.Scheme;

        ConnectionAddress = uri.Host;
        ConnectionPort    = uri.Port;
      }
      catch (FormatException e)
      {
        Console.WriteLine(e);
        throw;
      }
    }
  }

  /// <summary>
  ///   Secure or insecure protocol communication https or http (Default http)
  /// </summary>
  public string Protocol { get; set; } = "http";

  /// <summary>
  ///   The connection address property to connect to the control plane
  /// </summary>
  public string ConnectionAddress { get; set; }

  /// <summary>
  ///   The option connection port to connect to control plane (Default : 5001)
  /// </summary>
  public int ConnectionPort { get; set; } = 5001;

  /// <summary>
  ///   The TaskOptions to pass to the session or the submission session
  /// </summary>
  public TaskOptions TaskOptions { get; set; }

  /// <summary>
  ///   The target name of the endpoint when ssl validation is disabled. Automatic if not set.
  /// </summary>
  public string TargetNameOverride { get; set; } = "";

  /// <summary>
  ///   Initial backoff from retries
  /// </summary>
  public TimeSpan RetryInitialBackoff { get; } = TimeSpan.FromSeconds(1);

  /// <summary>
  ///   Backoff multiplier for retries
  /// </summary>
  public double RetryBackoffMultiplier { get; } = 2;

  /// <summary>
  ///   Max backoff for retries
  /// </summary>
  public TimeSpan RetryMaxBackoff { get; } = TimeSpan.FromSeconds(30);
}
