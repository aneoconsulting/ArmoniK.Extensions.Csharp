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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using Grpc.Core;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Contrib.WaitAndRetry;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
///   This ArmoniKClient uses a retry policy in case of failure from the execution of the call to the underlying
///   ArmoniKClient
/// </summary>
[PublicAPI]
// TODO: should be in ArmoniK.Api
public class RetryArmoniKClient : IArmoniKClient
{
  private readonly IArmoniKClient              armoniKClient_;
  private readonly ILogger<RetryArmoniKClient> logger_;

  /// <summary>
  ///   Default constructor
  /// </summary>
  /// <param name="logger"></param>
  /// <param name="armoniKClient"></param>
  public RetryArmoniKClient(ILogger<RetryArmoniKClient> logger,
                            IArmoniKClient              armoniKClient)
  {
    logger_        = logger;
    armoniKClient_ = armoniKClient;
  }

  /// <inheritdoc />
  public async Task<IEnumerable<TaskInfo>> SubmitTasksAsync(string                      sessionId,
                                                            IEnumerable<TaskDefinition> definitions,
                                                            TaskOptions                 taskOptions,
                                                            int                         maxRetries        = 1,
                                                            double                      totalTimeoutMs    = 1e10,
                                                            CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.SubmitTasksAsync(sessionId,
                                                                       definitions.ToList(),
                                                                       taskOptions,
                                                                       1,
                                                                       1e10,
                                                                       token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<IEnumerable<TaskOutputIds>> GetResultIdsAsync(IReadOnlyCollection<string> taskIds,
                                                                  int                         maxRetries        = 1,
                                                                  double                      totalTimeoutMs    = 1e10,
                                                                  CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.GetResultIdsAsync(taskIds,
                                                                        1,
                                                                        1e10,
                                                                        token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<byte[]> DownloadResultAsync(string            sessionId,
                                                string            resultId,
                                                int               maxRetries        = 1,
                                                double            totalTimeoutMs    = 1e10,
                                                CancellationToken cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.DownloadResultAsync(sessionId,
                                                                          resultId,
                                                                          1,
                                                                          1e10,
                                                                          token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken,
                              null,
                              exception => true);

  /// <inheritdoc />
  public async Task<string> CreateSessionAsync(TaskOptions                 taskOptions,
                                               IReadOnlyCollection<string> partitions,
                                               int                         maxRetries        = 1,
                                               double                      totalTimeoutMs    = 1e10,
                                               CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.CreateSessionAsync(taskOptions,
                                                                         partitions,
                                                                         1,
                                                                         1e10,
                                                                         token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<IEnumerable<TaskIdStatus>> GetTaskStatusAsync(IReadOnlyCollection<string> taskIds,
                                                                  int                         maxRetries        = 1,
                                                                  double                      totalTimeoutMs    = 1e10,
                                                                  CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.GetTaskStatusAsync(taskIds,
                                                                         1,
                                                                         1e10,
                                                                         token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<IEnumerable<ResultIdStatus>> GetResultStatusAsync(string                      sessionId,
                                                                      IReadOnlyCollection<string> resultIds,
                                                                      int                         maxRetries        = 1,
                                                                      double                      totalTimeoutMs    = 1e10,
                                                                      CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.GetResultStatusAsync(sessionId,
                                                                           resultIds,
                                                                           1,
                                                                           1e10,
                                                                           token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<string?> TryGetTaskErrorAsync(string            sessionId,
                                                  string            taskId,
                                                  int               maxRetries        = 1,
                                                  double            totalTimeoutMs    = 1e10,
                                                  CancellationToken cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.TryGetTaskErrorAsync(sessionId,
                                                                           taskId,
                                                                           1,
                                                                           1e10,
                                                                           token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<ImmutableDictionary<TaskStatus, int>> WaitForCompletionAsync(string                      sessionId,
                                                                                 IReadOnlyCollection<string> taskIds,
                                                                                 bool                        stopOnFirstTaskCancellation,
                                                                                 bool                        stopOnFirstTaskError,
                                                                                 int                         maxRetries        = 1,
                                                                                 double                      totalTimeoutMs    = 1e10,
                                                                                 CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.WaitForCompletionAsync(sessionId,
                                                                             taskIds,
                                                                             stopOnFirstTaskCancellation,
                                                                             stopOnFirstTaskError,
                                                                             1,
                                                                             1e10,
                                                                             token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task WaitForAvailability(string            sessionId,
                                        string            resultId,
                                        int               maxRetries        = 1,
                                        double            totalTimeoutMs    = 1e10,
                                        CancellationToken cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.WaitForAvailability(sessionId,
                                                                          resultId,
                                                                          1,
                                                                          1e10,
                                                                          token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  /// <inheritdoc />
  public async Task<IEnumerable<KeyValuePair<string, string>>> CreateResultMetaDataAsync(string                      sessionId,
                                                                                         IReadOnlyCollection<string> resultNames,
                                                                                         int                         maxRetries        = 1,
                                                                                         double                      totalTimeoutMs    = 1e10,
                                                                                         CancellationToken           cancellationToken = default)
    => await ApplyRetryPolicy(token => armoniKClient_.CreateResultMetaDataAsync(sessionId,
                                                                                resultNames,
                                                                                1,
                                                                                1e10,
                                                                                token),
                              maxRetries,
                              TimeSpan.FromMilliseconds(totalTimeoutMs),
                              cancellationToken);

  internal async Task<T> ApplyRetryPolicy<T>(Func<CancellationToken, Task<T>> action,
                                             int                              maxRetries,
                                             TimeSpan                         totalTimeout,
                                             CancellationToken                cancellationToken,
                                             Func<T, bool>?                   resultValidator          = null,
                                             Func<Exception, bool>?           exceptionHandlePredicate = null,
                                             [CallerMemberName] string        callerName               = "")
  {
    var delays = GetRetryDelays<T>(maxRetries,
                                   totalTimeout);

    var policyResult = await Policy<T>.HandleInner<RpcException>(RpcExceptionPredicate)
                                      .OrResult(resultValidator         ?? (_ => false))
                                      .OrInner(exceptionHandlePredicate ?? (_ => false))
                                      .WaitAndRetryAsync(delays,
                                                         (result,
                                                          span,
                                                          context) =>
                                                         {
                                                           // TODO: use the context to return a more complete exception at the end.
                                                           logger_.LogWarning(result.Exception,
                                                                              "Error during execution of {method}. Nb of trials: {nbTrials}/{maxRetries}. Will retry in {time}ms",
                                                                              context.OperationKey,
                                                                              context["trial"],
                                                                              context[nameof(maxRetries)],
                                                                              span.TotalMilliseconds);
                                                         })
                                      .ExecuteAndCaptureAsync((_,
                                                               token) => action(token),
                                                              new Context(callerName,
                                                                          new Dictionary<string, object>
                                                                          {
                                                                            ["trial"]            = 1,
                                                                            [nameof(maxRetries)] = maxRetries,
                                                                          }),
                                                              cancellationToken);

    return policyResult.Outcome switch
           {
             OutcomeType.Failure => throw new ArmoniKException($"Call to {callerName} failed the retry policy.\r\n" + $"Reason is: {policyResult.FaultType}.\r\n" +
                                                               $"See previous log for details.",
                                                               policyResult.FinalException),
             OutcomeType.Successful => policyResult.Result,
             _                      => throw new InvalidOperationException(),
           };
  }

  internal async Task ApplyRetryPolicy(Func<CancellationToken, Task> action,
                                       int                           maxRetries,
                                       TimeSpan                      totalTimeout,
                                       CancellationToken             cancellationToken,
                                       Func<Exception, bool>?        exceptionHandlePredicate = null,
                                       [CallerMemberName] string     callerName               = "")
    => await ApplyRetryPolicy(async token =>
                              {
                                await action(token);
                                return true;
                              },
                              maxRetries,
                              totalTimeout,
                              cancellationToken,
                              null,
                              exceptionHandlePredicate,
                              callerName);

  private bool RpcExceptionPredicate(RpcException rpcException)
    => rpcException.StatusCode switch
       {
         // TODO: review carefully to ensure that no false negative is here
         StatusCode.OK                 => true,
         StatusCode.Cancelled          => true,
         StatusCode.Unknown            => true,
         StatusCode.InvalidArgument    => false,
         StatusCode.DeadlineExceeded   => true,
         StatusCode.NotFound           => false,
         StatusCode.PermissionDenied   => false,
         StatusCode.Unauthenticated    => false,
         StatusCode.ResourceExhausted  => true,
         StatusCode.FailedPrecondition => true,
         StatusCode.Aborted            => true,
         StatusCode.OutOfRange         => true,
         StatusCode.Unimplemented      => false,
         StatusCode.Internal           => true,
         StatusCode.Unavailable        => true,
         StatusCode.DataLoss           => true,
         StatusCode.AlreadyExists      => false,
         _                             => throw new InvalidOperationException(),
       };

  internal static IEnumerable<TimeSpan> GetRetryDelays<T>(int      maxRetries,
                                                          TimeSpan totalTimeout)
  {
    if (maxRetries <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(maxRetries),
                                            "number of retries should be strictly positive");
    }

    if (totalTimeout.TotalMilliseconds <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(totalTimeout),
                                            "totalTimeout should be strictly positive");
    }

    var medianFirstRetryDelay = TimeSpan.FromMilliseconds(totalTimeout.TotalMilliseconds / Math.Pow(2,
                                                                                                    maxRetries));

    var delays = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay,
                                                     maxRetries,
                                                     null,
                                                     true);
    return delays;
  }
}
