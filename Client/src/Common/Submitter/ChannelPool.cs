// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;

using Grpc.Core;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Helper to have a connection pool for gRPC services
/// </summary>
public sealed class ChannelPool
{
  private readonly Func<ChannelBase> channelFactory_;

  private readonly ILogger<ChannelPool>? logger_;

  private readonly ConcurrentBag<ChannelBase> pool_;

  /// <summary>
  ///   Constructs a new channelPool
  /// </summary>
  /// <param name="channelFactory">Function used to create new channels</param>
  /// <param name="loggerFactory">loggerFactory used to instantiate a logger for the pool</param>
  public ChannelPool(Func<ChannelBase> channelFactory,
                     ILoggerFactory?   loggerFactory = null)
  {
    channelFactory_ = channelFactory;
    pool_           = new ConcurrentBag<ChannelBase>();
    logger_         = loggerFactory?.CreateLogger<ChannelPool>();
  }

  /// <summary>
  ///   Get a channel from the pool. If the pool is empty, create a new channel
  /// </summary>
  /// <returns>A ChannelBase used by nobody else</returns>
  private ChannelBase AcquireChannel()
  {
    if (pool_.TryTake(out var channel))
    {
      logger_?.LogDebug("Acquired already existing channel {channel} from pool",
                        channel);
      return channel;
    }

    channel = channelFactory_();
    logger_?.LogInformation("Created and acquired new channel {channel} from pool",
                            channel);
    return channel;
  }

  /// <summary>
  ///   Release a ChannelBase to the pool that could be reused later by someone else
  /// </summary>
  /// <param name="channel">Channel to release</param>
  private void ReleaseChannel(ChannelBase channel)
  {
    logger_?.LogDebug("Released channel {channel} to pool",
                      channel);
    pool_.Add(channel);
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
  {
    using var channel = GetChannel();
    return f(channel.Channel);
  }

  /// <summary>
  ///   Call f with an acquired channel
  /// </summary>
  /// <param name="f">Function to be called</param>
  public void WithChannel(Action<ChannelBase> f)
  {
    using var channel = GetChannel();
    f(channel.Channel);
  }

  /// <summary>
  ///   Helper class that acquires a channel from a pool when constructed, and releases it when disposed
  /// </summary>
  public sealed class ChannelGuard : IDisposable
  {
    /// <summary>
    ///   Channel that is used by nobody else
    /// </summary>
    public readonly ChannelBase Channel;

    private readonly ChannelPool pool_;

    /// <summary>
    ///   Acquire a channel that will be released when disposed
    /// </summary>
    /// <param name="channelPool"></param>
    public ChannelGuard(ChannelPool channelPool)
    {
      pool_   = channelPool;
      Channel = channelPool.AcquireChannel();
    }

    /// <inheritdoc />
    public void Dispose()
      => pool_.ReleaseChannel(Channel);

    /// <summary>
    ///   Implicit convert a ChannelGuard into a ChannelBase
    /// </summary>
    /// <param name="guard">ChannelGuard</param>
    /// <returns>ChannelBase</returns>
    public static implicit operator ChannelBase(ChannelGuard guard)
      => guard.Channel;
  }
}
