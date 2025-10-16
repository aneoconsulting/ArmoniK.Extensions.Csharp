// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2025. All rights reserved.
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
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Utils.Pool;

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
                    ReusePorts            = properties.ReusePorts,
                    HttpMessageHandler    = properties.HttpMessageHandler,
                  };

    var poolPolicy = new PoolPolicy<ChannelHandle>().SetCreate(() => new ChannelHandle(options,
                                                                                       loggerFactory))
                                                    .SetValidateAcquire(ChannelHandle.ValidateAcquire)
                                                    .SetValidateRelease(ChannelHandle.ValidateRelease);

    using var pool = new ObjectPool<ChannelHandle>(poolPolicy);

    return pool.Project(handle => handle.Channel);
  }

  private sealed class ChannelHandle : IDisposable
  {
    private static    int         nbChannels;
    internal readonly GrpcChannel Channel;
    private readonly  string      display_;
    private           bool        error_;
    private readonly  ILogger?    logger_;

    public ChannelHandle(GrpcClient      options,
                         ILoggerFactory? loggerFactory)
    {
      var idChannel = Interlocked.Increment(ref nbChannels);
      logger_ = loggerFactory?.CreateLogger(typeof(ClientServiceConnector));
      Channel = GrpcChannelFactory.CreateChannel(options,
                                                 logger_,
                                                 loggerFactory);
      error_   = false;
      display_ = $"{options.Endpoint} #{idChannel}";
      logger_?.LogDebug("Channel {Endpoint} created",
                        display_);
    }

    /// <inheritdoc />
    public void Dispose()
    {
      logger_?.LogDebug("Channel {Endpoint} destroyed",
                        display_);
      Channel.Dispose();
    }

    /// <summary>
    ///   Validate if the Acquired channel is valid
    /// </summary>
    /// <param name="handle">Handle which should be verified</param>
    /// <returns>Whether the handle is valid</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal static async ValueTask<bool> ValidateAcquire(ChannelHandle handle)
    {
#if NET5_0_OR_GREATER
      switch (handle.channel.State)
      {
        case ConnectivityState.TransientFailure:
          handle.logger_?.LogDebug("Channel {Endpoint} has a transient failure, channel will be destroyed",
                                   handle.display_);
          await handle.channel.ShutdownAsync()
                       .ConfigureAwait(false);
          return false;
        case ConnectivityState.Shutdown:
          handle.logger_?.LogDebug("Channel {Endpoint} is shutting down, channel will be destroyed",
                                   handle.display_);
          return false;
        case ConnectivityState.Idle:
        case ConnectivityState.Connecting:
        case ConnectivityState.Ready:
        default:
          return true;
      }
#else
      _ = handle;
      return true;
#endif
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    /// <summary>
    ///   Validate if the Released channel is valid
    /// </summary>
    /// <param name="handle">Handle which should be verified</param>
    /// <param name="error">Error that happened while using the channel, if any</param>
    /// <returns>Whether the handle is valid</returns>
    internal static async ValueTask<bool> ValidateRelease(ChannelHandle handle,
                                                          Exception?    error)
    {
      if (error is null)
      {
        handle.error_ = false;
        return await ValidateAcquire(handle)
                 .ConfigureAwait(false);
      }

      if (handle.error_)
      {
        handle.logger_?.LogDebug(error,
                                 "Too many errors for channel {Endpoint}, channel will be destroyed",
                                 handle.display_);
        return false;
      }

      handle.logger_?.LogDebug(error,
                               "Error for channel {Endpoint}, next error will trigger channel destruction",
                               handle.display_);

      handle.error_ = true;

      return await ValidateAcquire(handle)
               .ConfigureAwait(false);
    }
  }
}
