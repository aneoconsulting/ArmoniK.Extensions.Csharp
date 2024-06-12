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

using System.Threading.Tasks;

using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Utils;

using Grpc.Net.Client;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   ClientServiceConnector is the class to connection to the control plane with different
///   like address,port, insecure connection, TLS, and mTLS
/// </summary>
public class ClientServiceConnector
{
  /// <summary>
  ///   Create a connection pool to the control plane with mTLS authentication
  /// </summary>
  /// <param name="properties">Configuration Properties</param>
  /// <param name="loggerFactory">Optional logger factory</param>
  /// <returns>The connection pool</returns>
  public static ObjectPool<GrpcChannel> ControlPlaneConnectionPool(Properties      properties,
                                                                   ILoggerFactory? loggerFactory = null)
  {
    var options = new GrpcClient
                  {
                    AllowUnsafeConnection = !properties.ConfSslValidation,
                    CaCert                = properties.CaCertFilePem,
                    CertP12               = properties.ClientP12File,
                    CertPem               = properties.ClientCertFilePem,
                    KeyPem                = properties.ClientKeyFilePem,
                    Endpoint              = properties.ControlPlaneUri.ToString(),
                    OverrideTargetName    = properties.TargetNameOverride,
                    BackoffMultiplier     = properties.RetryBackoffMultiplier,
                    InitialBackOff        = properties.RetryInitialBackoff,
                    Proxy                 = properties.Proxy,
                    ProxyUsername         = properties.ProxyUsername,
                    ProxyPassword         = properties.ProxyPassword,
                  };

    return new ObjectPool<GrpcChannel>(ct => new ValueTask<GrpcChannel>(GrpcChannelFactory.CreateChannel(options,
                                                                                                         loggerFactory?.CreateLogger(typeof(ClientServiceConnector)))),


#if NET5_0_OR_GREATER
                                       async (channel, ct) =>
                                       {
switch (channel.State)
          {
            case ConnectivityState.TransientFailure:
              await channel.ShutdownAsync()
                  .ConfigureAwait(false);
              return false;
            case ConnectivityState.Shutdown:
              return false;
            case ConnectivityState.Idle:
            case ConnectivityState.Connecting:
            case ConnectivityState.Ready:
            default:
              return true;
          }
                                       }
#else
                                       (_,
                                        _) => new ValueTask<bool>(true)
#endif
                                      );
  }
}
