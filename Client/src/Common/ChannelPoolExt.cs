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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1.Events;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Sessions;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.Utils;
using ArmoniK.Utils.Pool;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Common;

public static class ChannelPoolExt
{
  public static ChannelPoolFluent<Tasks.TasksClient> WithTaskClient(this ObjectPool<GrpcChannel> pool,
                                                                    ILogger?                     logger = null)
    => new(pool,
           static channel => new Tasks.TasksClient(channel),
           logger);

  public static ChannelPoolFluent<Sessions.SessionsClient> WithSessionClient(this ObjectPool<GrpcChannel> pool,
                                                                             ILogger?                     logger = null)
    => new(pool,
           static channel => new Sessions.SessionsClient(channel),
           logger);

  public static ChannelPoolFluent<Results.ResultsClient> WithResultClient(this ObjectPool<GrpcChannel> pool,
                                                                          ILogger?                     logger = null)
    => new(pool,
           static channel => new Results.ResultsClient(channel),
           logger);

  public static ChannelPoolFluent<Events.EventsClient> WithEventClient(this ObjectPool<GrpcChannel> pool,
                                                                       ILogger?                     logger = null)
    => new(pool,
           static channel => new Events.EventsClient(channel),
           logger);

  public static ChannelPoolFluent<Api.gRPC.V1.Submitter.Submitter.SubmitterClient> WithSubmitterClient(this ObjectPool<GrpcChannel> pool,
                                                                                                       ILogger?                     logger = null)
    => new(pool,
           static channel => new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(channel),
           logger);

  public sealed class ChannelPoolFluent<TService>
  {
    private readonly ILogger?                    logger_;
    private readonly ObjectPool<GrpcChannel>     pool_;
    private readonly Func<GrpcChannel, TService> serviceFactory_;
    private          int                         backoffDelay_ = 100;
    private          Func<Exception, bool>       mustRetry_    = static _ => true;
    private          int                         retries_      = 1;

    internal ChannelPoolFluent(ObjectPool<GrpcChannel>     pool,
                               Func<GrpcChannel, TService> serviceFactory,
                               ILogger?                    logger)
    {
      pool_           = pool;
      serviceFactory_ = serviceFactory;
      logger_         = logger;
    }

    public ChannelPoolFluent<TService> WithDefaultRetries(int retries = 5)
      => WithRetries(retries,
                     ex => ex is IOException or RpcException
                                                {
                                                  Status.StatusCode: StatusCode.Internal or StatusCode.Unavailable or StatusCode.Unknown or StatusCode.Aborted or
                                                                     StatusCode.Cancelled,
                                                });

    public ChannelPoolFluent<TService> WithRetries(int                   retries,
                                                   Func<Exception, bool> mustRetry)
    {
      retries_   = retries;
      mustRetry_ = mustRetry;
      return this;
    }

    public ChannelPoolFluent<TService> WithRetries(int            retries,
                                                   bool           allowDerivedExceptions,
                                                   params Type[]? exceptions)
      => WithRetries(retries,
                     ex =>
                     {
                       if (exceptions != null && allowDerivedExceptions && ex is AggregateException &&
                           exceptions.Any(e => ex.InnerException != null && ex.InnerException.GetType() == e))
                       {
                         return true;
                       }

                       if (exceptions == null || exceptions.Any(e => e == ex.GetType()) || (allowDerivedExceptions && exceptions.Any(e => ex.GetType()
                                                                                                                                            .IsSubclassOf(e))))
                       {
                         return true;
                       }

                       return false;
                     });

    public ChannelPoolFluent<TService> WithBackoff(int backoffDelay)
    {
      backoffDelay_ = backoffDelay;
      return this;
    }

    public async ValueTask<TOut> ExecuteAsync<TOut>(Func<TService, ValueTask<TOut>> func,
                                                    CancellationToken               cancellationToken = default)
    {
      Exception? lastException = null;
      for (var retry = 1; retry <= retries_; retry++)
      {
        await using var service = await pool_.GetAsync(serviceFactory_,
                                                       cancellationToken)
                                             .ConfigureAwait(false);

        try
        {
          return await func(service)
                   .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          lastException     = ex;
          service.Exception = ex;
          if (retry < retries_ && mustRetry_(ex))
          {
            logger_?.LogWarning(ex,
                                "Got exception while executing function to retry {retry}/{retries}",
                                retry,
                                retries_);
            await Task.Delay(backoffDelay_,
                             cancellationToken)
                      .ConfigureAwait(false);
          }
          else
          {
            throw;
          }
        }
      }

      throw new Exception("Unreachable: out of retries",
                          lastException);
    }

    public ValueTask<TOut> ExecuteAsync<TOut>(Func<TService, TOut> func,
                                              CancellationToken    cancellationToken = default)
      => ExecuteAsync(service => new ValueTask<TOut>(func(service)),
                      cancellationToken);

    public async ValueTask ExecuteAsync(Func<TService, ValueTask> func,
                                        CancellationToken         cancellationToken = default)
      => await ExecuteAsync(service => func(service)
                              .AndThen(static () => new ValueTuple()),
                            cancellationToken)
           .ConfigureAwait(false);

    public async ValueTask ExecuteAsync(Action<TService>  func,
                                        CancellationToken cancellationToken = default)
      => await ExecuteAsync(service =>
                            {
                              func(service);
                              return new ValueTask<ValueTuple>(new ValueTuple());
                            },
                            cancellationToken)
           .ConfigureAwait(false);

    public async ValueTask<TOut> ExecuteAsync<TOut>(Func<TService, AsyncUnaryCall<TOut>> func,
                                                    CancellationToken                    cancellationToken = default)
      => await ExecuteAsync(service => new ValueTask<TOut>(func(service)
                                                             .ResponseAsync),
                            cancellationToken)
           .ConfigureAwait(false);

    public ValueTask<TOut> ExecuteAsync<TOut>(Func<TService, Task<TOut>> func,
                                              CancellationToken          cancellationToken = default)
      => ExecuteAsync(service => new ValueTask<TOut>(func(service)),
                      cancellationToken);

    public ValueTask ExecuteAsync(Func<TService, Task> func,
                                  CancellationToken    cancellationToken = default)
      => ExecuteAsync(service => new ValueTask(func(service)),
                      cancellationToken);
  }
}
