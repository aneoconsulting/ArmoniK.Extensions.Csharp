// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2024. All rights reserved.
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
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Helper to have a connection pool for gRPC services
/// </summary>
public sealed class ChannelPool
{
  private readonly Func<GrpcChannel> channelFactory_;

  private readonly ILogger<ChannelPool>? logger_;

  private readonly ConcurrentBag<GrpcChannel> pool_;

  /// <summary>
  ///   Constructs a new channelPool
  /// </summary>
  /// <param name="channelFactory">Function used to create new channels</param>
  /// <param name="loggerFactory">loggerFactory used to instantiate a logger for the pool</param>
  public ChannelPool(Func<GrpcChannel> channelFactory,
                     ILoggerFactory?   loggerFactory = null)
  {
    channelFactory_ = channelFactory;
    pool_           = new ConcurrentBag<GrpcChannel>();
    logger_         = loggerFactory?.CreateLogger<ChannelPool>();
  }

  /// <summary>
  ///   Get a channel from the pool. If the pool is empty, create a new channel
  /// </summary>
  /// <returns>A GrpcChannel used by nobody else</returns>
  private GrpcChannel AcquireChannel()
  {
    if (pool_.TryTake(out var channel))
    {
      if (ShutdownOnFailure(channel))
      {
        logger_?.LogDebug("Got an invalid channel {channel} from pool",
                          channel);
      }
      else
      {
        logger_?.LogDebug("Acquired already existing channel {channel} from pool",
                          channel);
        return channel;
      }
    }

    channel = channelFactory_();
    logger_?.LogInformation("Created and acquired new channel {channel} from pool",
                            channel);
    return channel;
  }

  /// <summary>
  ///   Release a GrpcChannel to the pool that could be reused later by someone else
  /// </summary>
  /// <param name="channel">Channel to release</param>
  private void ReleaseChannel(GrpcChannel channel)
  {
    if (ShutdownOnFailure(channel))
    {
      logger_?.LogDebug("Shutdown unhealthy channel {channel}",
                        channel);
    }
    else
    {
      logger_?.LogDebug("Released channel {channel} to pool",
                        channel);
      pool_.Add(channel);
    }
  }

  /// <summary>
  ///   Check the state of a channel and shutdown it in case of failure
  /// </summary>
  /// <param name="channel">Channel to check the state</param>
  /// <returns>True if the channel has been shut down</returns>
  private static bool ShutdownOnFailure(GrpcChannel channel)
  {
    try
    {
#if NET5_0_OR_GREATER
      switch (channel.State)
      {
        case ConnectivityState.TransientFailure:
          channel.ShutdownAsync()
                 .Wait();
          channel.Dispose();
          return true;
        case ConnectivityState.Shutdown:
          return true;
        case ConnectivityState.Idle:
        case ConnectivityState.Connecting:
        case ConnectivityState.Ready:
        default:
          return false;
      }
#else
      _ = channel;
      return false;
#endif
    }
    catch (InvalidOperationException)
    {
      return false;
    }
  }

  /// <summary>
  ///   Get a channel that will be automatically released when disposed
  /// </summary>
  /// <returns></returns>
  public ChannelGuard GetChannel()
    => new(this);

  /// <summary>
  ///   Call f with an acquired channel
  /// </summary>
  /// <param name="f">Function to be called</param>
  /// <typeparam name="T">Type of the return type of f</typeparam>
  /// <returns>Value returned by f</returns>
  public T WithChannel<T>(Func<GrpcChannel, T> f)
  {
    using var channel = GetChannel();
    return f(channel);
  }

  /// <summary>
  ///   Helper class that acquires a channel from a pool when constructed, and releases it when disposed
  /// </summary>
  public sealed class ChannelGuard : IDisposable
  {
    /// <summary>
    ///   Channel that is used by nobody else
    /// </summary>
    [SuppressMessage("Usage",
                     "CA2213:Disposable fields should be disposed")]
    private readonly GrpcChannel channel_;

    private readonly ChannelPool pool_;

    /// <summary>
    ///   Acquire a channel that will be released when disposed
    /// </summary>
    /// <param name="channelPool"></param>
    public ChannelGuard(ChannelPool channelPool)
    {
      pool_    = channelPool;
      channel_ = channelPool.AcquireChannel();
    }

    /// <inheritdoc />
    public void Dispose()
      => pool_.ReleaseChannel(channel_);

    /// <summary>
    ///   Implicit convert a ChannelGuard into a ChannelBase
    /// </summary>
    /// <param name="guard">ChannelGuard</param>
    /// <returns>GrpcChannel</returns>
    public static implicit operator GrpcChannel(ChannelGuard guard)
      => guard.channel_;
  }
}
