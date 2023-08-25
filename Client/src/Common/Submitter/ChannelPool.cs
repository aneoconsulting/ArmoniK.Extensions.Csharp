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
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#endif

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Helper to have a connection pool for gRPC services
/// </summary>
public sealed class ChannelPool
{
  private readonly Func<ChannelBase> channelFactory_;

  private readonly ILogger<ChannelPool> logger_;

  private readonly ConcurrentBag<ChannelBase> pool_;

  /// <summary>
  ///   Constructs a new channelPool
  /// </summary>
  /// <param name="channelFactory">Function used to create new channels</param>
  /// <param name="loggerFactory">loggerFactory used to instantiate a logger for the pool</param>
  public ChannelPool(Func<ChannelBase>    channelFactory,
                     ILogger<ChannelPool> logger)
  {
    channelFactory_ = channelFactory;
    pool_           = new ConcurrentBag<ChannelBase>();
    logger_         = logger;
  }

  /// <summary>
  ///   Get a channel from the pool. If the pool is empty, create a new channel
  /// </summary>
  /// <returns>A ChannelBase used by nobody else</returns>
  private async Task<ChannelBase> AcquireChannelAsync()
  {
    while (pool_.TryTake(out var channel))
    {
      if (await IsChannelFailedAsync(channel))
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

    var newChannel = channelFactory_();
    logger_?.LogInformation("Created and acquired new channel {channel} from pool",
                            newChannel);
    return newChannel;
  }

  /// <summary>
  ///   Release a ChannelBase to the pool that could be reused later by someone else
  /// </summary>
  /// <param name="channel">Channel to release</param>
  private async Task ReleaseChannelAsync(ChannelBase channel)
  {
    if (await IsChannelFailedAsync(channel))
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
  private static async Task<bool> IsChannelFailedAsync(ChannelBase channel)
  {
    try
    {
      switch (channel)
      {
        case Channel chan:
          switch (chan.State)
          {
            case ChannelState.TransientFailure:
              await chan.ShutdownAsync();
              return true;
            case ChannelState.Shutdown:
              return true;
            case ChannelState.Idle:
            case ChannelState.Connecting:
            case ChannelState.Ready:
            default:
              return false;
          }
#if NET5_0_OR_GREATER
        case GrpcChannel chan:
          switch (chan.State)
          {
            case ConnectivityState.TransientFailure:
              await chan.ShutdownAsync();
              return true;
            case ConnectivityState.Shutdown:
              return true;
            case ConnectivityState.Idle:
            case ConnectivityState.Connecting:
            case ConnectivityState.Ready:
            default:
              return false;
          }
#endif
        default:
          return false;
      }
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
  public T WithChannel<T>(Func<ChannelBase, T> f)
    => WithChannel(channel => Task.FromResult(f(channel)))
      .Result;

  /// <summary>
  ///   Call f with an acquired channel
  /// </summary>
  /// <param name="f">Function to be called</param>
  /// <typeparam name="T">Type of the return type of f</typeparam>
  /// <returns>Value returned by f</returns>
  public async Task<T> WithChannel<T>(Func<ChannelBase, Task<T>> f)
  {
    await using var channel = GetChannel();
    return await f(channel.Channel);
  }

  /// <summary>
  ///   Helper class that acquires a channel from a pool when constructed, and releases it when disposed
  /// </summary>
  public sealed class ChannelGuard : IAsyncDisposable
  {
    private readonly ChannelPool pool_;

    /// <summary>
    ///   Acquire a channel that will be released when disposed
    /// </summary>
    /// <param name="channelPool"></param>
    public ChannelGuard(ChannelPool channelPool)
    {
      pool_   = channelPool;
      Channel = channelPool.AcquireChannelAsync().Result;
    }

    /// <summary>
    ///   Channel that is used by nobody else
    /// </summary>
    public ChannelBase Channel { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
      await pool_.ReleaseChannelAsync(Channel);
    }
  }
}
