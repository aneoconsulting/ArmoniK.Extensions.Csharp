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
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.DevelopmentKit.Client.Common.Status;

using JetBrains.Annotations;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
///   Implements the <c>ISubmitter</c> by using the ArmoniK gRPC API.
/// </summary>
[PublicAPI]
// TODO: should be in ArmoniK.Api
public class GrpcArmoniKClient : IArmoniKClient
{
  private readonly Func<ChannelPool.ChannelGuard> channelFactory_;

  /// <summary>
  ///   Builds an instance of the armoniKClient
  /// </summary>
  /// <param name="channelFactory">Used to call the grpc API</param>
  public GrpcArmoniKClient(Func<ChannelPool.ChannelGuard> channelFactory)
    => channelFactory_ = channelFactory;

  /// <inheritdoc />
  public async Task<IEnumerable<TaskInfo>> SubmitTasksAsync(string                      sessionId,
                                                            IEnumerable<TaskDefinition> definitions,
                                                            TaskOptions                 taskOptions,
                                                            int                         maxRetries        = 1,
                                                            double                      totalTimeoutMs    = 1e10,
                                                            CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    try
    {
      var nbRequests = 0;
      var taskRequests = definitions.Select(definition =>
                                            {
                                              nbRequests++;
                                              return definition.ToTaskRequest();
                                            });

      using var guard = channelFactory_();

      var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);

      var response = await service.CreateTasksAsync(sessionId,
                                                    taskOptions,
                                                    taskRequests,
                                                    cancellationToken)
                                  .ConfigureAwait(false);

      var output = response.ResponseCase switch
                   {
                     CreateTaskReply.ResponseOneofCase.CreationStatusList => response.CreationStatusList.CreationStatuses,
                     // Maybe: use another kind of exception to ease the definition of a better retry policy?
                     CreateTaskReply.ResponseOneofCase.Error => throw new Exception("Error while creating tasks"),
                     CreateTaskReply.ResponseOneofCase.None  => throw new Exception("Issue with remote service"),
                     _                                       => throw new InvalidOperationException(),
                   };


      var nbTaskIds = 0;
      foreach (var status in output)
      {
        cancellationToken.ThrowIfCancellationRequested();
        nbTaskIds++;
        switch (status.StatusCase)
        {
          case CreateTaskReply.Types.CreationStatus.StatusOneofCase.TaskInfo:
            break;
          case CreateTaskReply.Types.CreationStatus.StatusOneofCase.Error:
            throw new ArmoniKException(status.Error);
          case CreateTaskReply.Types.CreationStatus.StatusOneofCase.None:
          default:
            throw new ArmoniKException($"TaskStatus error for a task in {nameof(SubmitTasksAsync)}");
        }
      }

      if (nbRequests != nbTaskIds)
      {
        throw new ArmoniKException($"Number of taskId received ({nbTaskIds}) does not correspond to the number of tasks sent ({nbRequests}.");
      }

      return output.Select(status => new TaskInfo(status.TaskInfo));
    }
    catch (ArmoniKException)
    {
      throw;
    }
    catch (Exception e)
    {
      throw new ArmoniKException($"An unexpected error occurred: {e.Message}",
                                 e);
    }
  }

  /// <inheritdoc />
  public async Task<IEnumerable<TaskOutputIds>> GetResultIdsAsync(IReadOnlyCollection<string> taskIds,
                                                                  int                         maxRetries        = 1,
                                                                  double                      totalTimeoutMs    = 1e10,
                                                                  CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Tasks.TasksClient(guard.Channel);

    var response = await service.GetResultIdsAsync(new GetResultIdsRequest
                                                   {
                                                     TaskId =
                                                     {
                                                       taskIds,
                                                     },
                                                   },
                                                   cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

    return response.TaskResults.Select(result => new TaskOutputIds(result));
  }

  /// <inheritdoc />
  public async Task<byte[]> DownloadResultAsync(string            sessionId,
                                                string            resultId,
                                                int               maxRetries        = 1,
                                                double            totalTimeoutMs    = 1e10,
                                                CancellationToken cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Results.ResultsClient(guard.Channel);

    return await service.DownloadResultData(sessionId,
                                            resultId,
                                            cancellationToken);
  }

  /// <inheritdoc />
  public async Task<string> CreateSessionAsync(TaskOptions                 taskOptions,
                                               IReadOnlyCollection<string> partitions,
                                               int                         maxRetries        = 1,
                                               double                      totalTimeoutMs    = 1e10,
                                               CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);

    var session = await service.CreateSessionAsync(new CreateSessionRequest
                                                   {
                                                     DefaultTaskOption = taskOptions,
                                                     PartitionIds =
                                                     {
                                                       partitions,
                                                     },
                                                   },
                                                   cancellationToken: cancellationToken)
                               .ConfigureAwait(false);

    return session.SessionId;
  }

  /// <inheritdoc />
  public async Task<IEnumerable<TaskIdStatus>> GetTaskStatusAsync(IReadOnlyCollection<string> taskIds,
                                                                  int                         maxRetries        = 1,
                                                                  double                      totalTimeoutMs    = 1e10,
                                                                  CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);

    var statuses = await service.GetTaskStatusAsync(new GetTaskStatusRequest
                                                    {
                                                      TaskIds =
                                                      {
                                                        taskIds,
                                                      },
                                                    },
                                                    cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

    return statuses.IdStatuses.Select(status => new TaskIdStatus(status.TaskId,
                                                                 status.Status.ToArmonikStatusCode()));
  }

  /// <inheritdoc />
  public async Task<IEnumerable<ResultIdStatus>> GetResultStatusAsync(string                      sessionId,
                                                                      IReadOnlyCollection<string> resultIds,
                                                                      int                         maxRetries        = 1,
                                                                      double                      totalTimeoutMs    = 1e10,
                                                                      CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);


    // TODO: replace with a logic based on service.TryGetResultStream
    var statuses = await service.GetResultStatusAsync(new GetResultStatusRequest
                                                      {
                                                        ResultIds =
                                                        {
                                                          resultIds,
                                                        },
                                                        SessionId = sessionId,
                                                      },
                                                      cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

    return statuses.IdStatuses.Select(status => new ResultIdStatus(status.ResultId,
                                                                   status.Status.ToArmoniKResultStatus()));
  }

  /// <inheritdoc />
  public async Task<string?> TryGetTaskErrorAsync(string            sessionId,
                                                  string            taskId,
                                                  int               maxRetries        = 1,
                                                  double            totalTimeoutMs    = 1e10,
                                                  CancellationToken cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);

    var output = await service.TryGetTaskOutputAsync(new TaskOutputRequest
                                                     {
                                                       Session = sessionId,
                                                       TaskId  = taskId,
                                                     },
                                                     cancellationToken: cancellationToken)
                              .ConfigureAwait(false);

    return output.TypeCase switch
           {
             Output.TypeOneofCase.Ok    => null,
             Output.TypeOneofCase.Error => output.Error.Details,
             Output.TypeOneofCase.None  => throw new InvalidOperationException(),
             _                          => throw new InvalidOperationException(),
           };
  }

  /// <inheritdoc />
  public async Task<ImmutableDictionary<TaskStatus, int>> WaitForCompletionAsync(string                      sessionId,
                                                                                 IReadOnlyCollection<string> taskIds,
                                                                                 bool                        stopOnFirstTaskCancellation,
                                                                                 bool                        stopOnFirstTaskError,
                                                                                 int                         maxRetries        = 1,
                                                                                 double                      totalTimeoutMs    = 1e10,
                                                                                 CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);

    var count = await service.WaitForCompletionAsync(new WaitRequest
                                                     {
                                                       Filter = new TaskFilter
                                                                {
                                                                  Task = new TaskFilter.Types.IdsRequest
                                                                         {
                                                                           Ids =
                                                                           {
                                                                             taskIds,
                                                                           },
                                                                         },
                                                                  Session = new TaskFilter.Types.IdsRequest
                                                                            {
                                                                              Ids =
                                                                              {
                                                                                sessionId,
                                                                              },
                                                                            },
                                                                },
                                                       StopOnFirstTaskCancellation = stopOnFirstTaskCancellation,
                                                       StopOnFirstTaskError        = stopOnFirstTaskError,
                                                     },
                                                     cancellationToken: cancellationToken)
                             .ConfigureAwait(false);

    return count.Values.ToImmutableDictionary(statusCount => statusCount.Status,
                                              statusCount => statusCount.Count);
  }

  /// <inheritdoc />
  public async Task WaitForAvailability(string            sessionId,
                                        string            resultId,
                                        int               maxRetries        = 1,
                                        double            totalTimeoutMs    = 1e10,
                                        CancellationToken cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Api.gRPC.V1.Submitter.Submitter.SubmitterClient(guard.Channel);

    // TODO: replace with a logic based on service.TryGetResultStream
    var reply = await service.WaitForAvailabilityAsync(new ResultRequest
                                                       {
                                                         ResultId = resultId,
                                                         Session  = sessionId,
                                                       },
                                                       cancellationToken: cancellationToken)
                             .ConfigureAwait(false);

    switch (reply.TypeCase)
    {
      case AvailabilityReply.TypeOneofCase.Ok:
        return;
      case AvailabilityReply.TypeOneofCase.Error:
        throw new ArmoniKException($"Result in Error - {resultId}\nMessage :\n{string.Join("Inner message:\n", reply.Error.Errors)}");
      case AvailabilityReply.TypeOneofCase.NotCompletedTask:
        throw new ArmoniKException($"Result {resultId} was not yet completed");
      case AvailabilityReply.TypeOneofCase.None:
      default:
        throw new InvalidOperationException();
    }
  }

  /// <inheritdoc />
  public async Task<IEnumerable<KeyValuePair<string, string>>> CreateResultMetaDataAsync(string                      sessionId,
                                                                                         IReadOnlyCollection<string> resultNames,
                                                                                         int                         maxRetries        = 1,
                                                                                         double                      totalTimeoutMs    = 1e10,
                                                                                         CancellationToken           cancellationToken = default)
  {
    ValidateRetryArguments(maxRetries,
                           totalTimeoutMs);

    using var guard = channelFactory_();

    var service = new Results.ResultsClient(guard.Channel);


    var resultsMetaData = await service.CreateResultsMetaDataAsync(new CreateResultsMetaDataRequest
                                                                   {
                                                                     SessionId = sessionId,
                                                                     Results =
                                                                     {
                                                                       resultNames.Select(resultName => new CreateResultsMetaDataRequest.Types.ResultCreate
                                                                                                        {
                                                                                                          Name = resultName,
                                                                                                        }),
                                                                     },
                                                                   },
                                                                   cancellationToken: cancellationToken)
                                       .ConfigureAwait(false);

    return resultsMetaData.Results.Select(result => new KeyValuePair<string, string>(result.Name,
                                                                                     result.ResultId));
  }

  internal static void ValidateRetryArguments(int    maxRetries,
                                              double totalTimeoutMs)
  {
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    if (maxRetries != 1 || totalTimeoutMs != 1e10)
    {
      throw new ArgumentOutOfRangeException(nameof(maxRetries),
                                            $"{nameof(GrpcArmoniKClient)} has no retry policy and only support a single call.\r\n" +
                                            $"{maxRetries} must be equal to 1.\r\n" + $"{totalTimeoutMs} must be equal to 1e10 ({1e10}).");
    }
  }
}
