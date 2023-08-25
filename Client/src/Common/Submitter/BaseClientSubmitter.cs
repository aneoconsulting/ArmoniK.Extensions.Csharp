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

using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common.Status;
using ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.Utils;

using Google.Protobuf;

using Grpc.Core;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Base Object for all Client submitter
///   Need to pass the child object Class Type
/// </summary>
[PublicAPI]
// TODO: This should not be a public API. Public API should be defined in an interface.
public abstract class BaseClientSubmitter<T>
{
  /// <summary>
  ///   The number of chunk to split the payloadsWithDependencies
  /// </summary>
  private readonly int chunkSubmitSize_;

  /// <summary>
  ///   Base Object for all Client submitter
  /// </summary>
  /// <param name="properties">Properties used to create grpc clients</param>
  /// <param name="loggerFactory">the logger factory to pass for root object</param>
  /// <param name="taskOptions"></param>
  /// <param name="session"></param>
  /// <param name="chunkSubmitSize">The size of chunk to split the list of tasks</param>
  protected BaseClientSubmitter(Properties     properties,
                                ILoggerFactory loggerFactory,
                                TaskOptions    taskOptions,
                                Session?       session,
                                int            chunkSubmitSize = 500)
  {
    Logger = loggerFactory.CreateLogger<T>();


    var channelPool = ClientServiceConnector.ControlPlaneConnectionPool(properties,
                                                                        loggerFactory);

    ArmoniKClient = new RetryArmoniKClient(loggerFactory.CreateLogger<RetryArmoniKClient>(),
                                           new GrpcArmoniKClient(() => channelPool.GetChannel()));

    TaskOptions      = taskOptions;
    chunkSubmitSize_ = chunkSubmitSize;

    SessionId = session ?? CreateSession(new[]
                                         {
                                           TaskOptions.PartitionId,
                                         });
  }

  /// <summary>
  ///   Base Object for all Client submitter
  /// </summary>
  /// <param name="armoniKClient">ArmoniKClient instance to be used for all the calls to ArmoniK's Control Plane</param>
  /// <param name="logger">the logger for current object</param>
  /// <param name="taskOptions"></param>
  /// <param name="session"></param>
  /// <param name="chunkSubmitSize">The size of chunk to split the list of tasks</param>
  internal BaseClientSubmitter(IArmoniKClient armoniKClient,
                               ILogger<T>     logger,
                               TaskOptions    taskOptions,
                               Session?       session,
                               int            chunkSubmitSize = 500)
  {
    TaskOptions      = taskOptions;
    ArmoniKClient    = armoniKClient;
    Logger           = logger;
    chunkSubmitSize_ = chunkSubmitSize;

    SessionId = session ?? CreateSession(new[]
                                         {
                                           TaskOptions.PartitionId,
                                         });
  }

  private IArmoniKClient ArmoniKClient { get; }

  /// <summary>
  ///   Set or Get TaskOptions with inside MaxDuration, Priority, AppName, VersionName and AppNamespace
  /// </summary>
  public TaskOptions TaskOptions { get; }

  /// <summary>
  ///   Get SessionId object stored during the call of SubmitTask, SubmitSubTask,
  ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
  /// </summary>
  public Session SessionId { get; }

  /// <summary>
  ///   The logger to call the generate log in Seq
  /// </summary>

  protected ILogger<T> Logger { get; }


  /// <inheritdoc />
  public override string ToString()
    => SessionId.Id ?? "Session_Not_ready";

  private Session CreateSession(IReadOnlyCollection<string> partitionIds)
  {
    using var _ = Logger.LogFunction();
    Logger.LogDebug("Creating Session... ");

    var session = ArmoniKClient.CreateSessionAsync(TaskOptions,
                                                   partitionIds,
                                                   5,
                                                   cancellationToken: CancellationToken.None)
                               .Result;

    Logger.LogDebug("Session Created {SessionId}",
                    SessionId);
    return new Session
           {
             Id = session,
           };
  }


  /// <summary>
  ///   Returns the status of the task
  /// </summary>
  /// <param name="taskId">The taskId of the task</param>
  /// <returns></returns>
  public ArmonikTaskStatusCode GetTaskStatus(string taskId)
    => ArmoniKClient.GetTaskStatusAsync(new[]
                                        {
                                          taskId,
                                        },
                                        5,
                                        cancellationToken: CancellationToken.None)
                    .Result.Single()
                    .TaskStatus;

  /// <summary>
  ///   Return the taskOutput when error occurred
  /// </summary>
  /// <param name="taskId"></param>
  /// <returns></returns>
  public string? TryGetTaskError(string taskId)
    => ArmoniKClient.TryGetTaskErrorAsync(SessionId.Id,
                                          taskId,
                                          5,
                                          cancellationToken: CancellationToken.None)
                    .Result;

  /// <summary>
  ///   The method to submit several tasks with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payloadsWithDependencies">
  ///   A list of Tuple(resultId, payload, parent dependencies) in dependence of those
  ///   created tasks
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">TaskOptions overrides if non null override default in Session</param>
  /// <remarks>The result ids must first be created using <see cref="CreateResultsMetadata" /></remarks>
  /// <returns>return a list of taskIds of the created tasks </returns>
  [PublicAPI]
  public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<string, byte[], IList<string>>> payloadsWithDependencies,
                                                         int                                               maxRetries  = 5,
                                                         TaskOptions?                                      taskOptions = null)
    => payloadsWithDependencies.ToChunks(chunkSubmitSize_)
                               .SelectMany(chunk => SubmitTaskChunkWithDependencies(chunk,
                                                                                    maxRetries,
                                                                                    taskOptions ?? TaskOptions));

  /// <summary>
  ///   The method to submit several tasks with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payloadsWithDependencies">
  ///   A list of Tuple(Payload, parent dependencies) in dependence of those created
  ///   tasks
  /// </param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>return a list of taskIds of the created tasks </returns>
  [PublicAPI]
  public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadsWithDependencies,
                                                         int                                       maxRetries  = 5,
                                                         TaskOptions?                              taskOptions = null)
    => payloadsWithDependencies.ToChunks(chunkSubmitSize_)
                               .SelectMany(chunk =>
                                           {
                                             // Create the result metadata before sending the tasks.
                                             var resultsMetadata = CreateResultsMetadata(Enumerable.Range(0,
                                                                                                          chunk.Length)
                                                                                                   .Select(_ => Guid.NewGuid()
                                                                                                                    .ToString()));
                                             return SubmitTaskChunkWithDependencies(chunk.Zip(resultsMetadata,
                                                                                              (payloadWithDependencies,
                                                                                               metadata) => Tuple.Create(metadata.Value,
                                                                                                                         payloadWithDependencies.Item1,
                                                                                                                         payloadWithDependencies.Item2)),
                                                                                    maxRetries,
                                                                                    taskOptions ?? TaskOptions);
                                           });


  /// <summary>
  ///   The method to submit several tasks with dependencies tasks. This task will wait for
  ///   to start until all dependencies are completed successfully
  /// </summary>
  /// <param name="payloadsWithDependencies">A list of Tuple(resultId, Payload) in dependence of those created tasks</param>
  /// <param name="maxRetries">Set the number of retries Default Value 5</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>return the ids of the created tasks</returns>
  private IEnumerable<string> SubmitTaskChunkWithDependencies(IEnumerable<Tuple<string, byte[], IList<string>>> payloadsWithDependencies,
                                                              int                                               maxRetries,
                                                              TaskOptions?                                      taskOptions = null)
  {
    using var _ = Logger.LogFunction();

    var taskDefinitions = payloadsWithDependencies.Select(tuple => new TaskDefinition("",
                                                                                      UnsafeByteOperations.UnsafeWrap(tuple.Item2),
                                                                                      tuple.Item3.ToArray(),
                                                                                      new[]
                                                                                      {
                                                                                        tuple.Item1,
                                                                                      }));

    return ArmoniKClient.SubmitTasksAsync(SessionId.Id,
                                          // ReSharper disable once PossibleMultipleEnumeration
                                          // Only occurs in case of retry
                                          taskDefinitions,
                                          taskOptions ?? TaskOptions,
                                          maxRetries,
                                          cancellationToken: CancellationToken.None)
                        // TODO: Store the taskId->ResultId Mapping
                        .Result.Select(info => info.TaskId);
  }

  /// <summary>
  ///   User method to wait for only the parent task from the client
  /// </summary>
  /// <param name="taskId">
  ///   The task taskId of the task to wait for
  /// </param>
  /// <param name="maxRetries">Max number of retries for the underlying calls</param>
  /// <param name="delayMs">Delay between retries</param>
  public void WaitForTaskCompletion(string taskId,
                                    int    maxRetries = 5,
                                    int    delayMs    = 20000)
  {
    using var _ = Logger.LogFunction(taskId);

    WaitForTasksCompletion(new[]
                           {
                             taskId,
                           },
                           maxRetries,
                           delayMs);
  }

  /// <summary>
  ///   User method to wait for only the parent task from the client
  /// </summary>
  /// <param name="taskIds">
  ///   List of taskIds
  /// </param>
  /// <param name="maxRetries">Max number of retries</param>
  /// <param name="delayMs"></param>
  [PublicAPI]
  public void WaitForTasksCompletion(IEnumerable<string> taskIds,
                                     int                 maxRetries = 5,
                                     int                 delayMs    = 20000)
  {
    using var _ = Logger.LogFunction();

    ArmoniKClient.WaitForCompletionAsync(SessionId.Id,
                                         taskIds.ToList(),
                                         true,
                                         true,
                                         maxRetries,
                                         maxRetries * delayMs / Math.Pow(2,
                                                                         maxRetries),
                                         CancellationToken.None)
                 .Wait();
  }

  /// <summary>
  ///   Get the result status of a list of results
  /// </summary>
  /// <param name="taskIds">Collection of task ids from which to retrieve results</param>
  /// <param name="cancellationToken"></param>
  /// <returns>A ResultCollection sorted by TaskStatus Completed, Result in Error or missing</returns>
  public ResultStatusCollection GetResultStatus(IEnumerable<string> taskIds,
                                                CancellationToken   cancellationToken = default)
  {
    var taskList       = taskIds.ToList();
    var mapTaskResults = GetResultIds(taskList);

    var result2TaskDic = mapTaskResults.ToDictionary(result => result.OutputIds.Single(),
                                                     result => result.TaskId);

    var missingTasks = taskList.Count > result2TaskDic.Count
                         ? taskList.Except(result2TaskDic.Values)
                                   .Select(tid => new ResultStatusData(string.Empty,
                                                                       tid,
                                                                       ArmoniKResultStatus.Unknown))
                         : Array.Empty<ResultStatusData>();

    var idStatuses = ArmoniKClient.GetResultStatusAsync(SessionId.Id,
                                                        result2TaskDic.Keys,
                                                        5,
                                                        5 * 2000 / Math.Pow(2,
                                                                            5),
                                                        cancellationToken)
                                  .Result.Where(status => status.TaskStatus != ArmoniKResultStatus.Unknown)
                                  .ToLookup(idStatus => idStatus.TaskStatus,
                                            idStatus =>
                                            {
                                              var taskId = result2TaskDic[idStatus.ResultId];
                                              result2TaskDic.Remove(idStatus.ResultId);
                                              return new ResultStatusData(idStatus.ResultId,
                                                                          taskId,
                                                                          idStatus.TaskStatus);
                                            });


    var resultStatusList = new ResultStatusCollection(idStatuses[ArmoniKResultStatus.Available]
                                                        .ToImmutableList(),
                                                      idStatuses[ArmoniKResultStatus.Error]
                                                        .ToImmutableList(),
                                                      result2TaskDic.Values.ToList(),
                                                      idStatuses[ArmoniKResultStatus.NotReady]
                                                        .ToImmutableList(),
                                                      missingTasks.ToList());

    return resultStatusList;
  }

  /// <summary>
  ///   Gets the result ids for a given list of task ids.
  /// </summary>
  /// <param name="taskIds">The list of task ids.</param>
  /// <returns>A collection of map task results.</returns>
  public IEnumerable<TaskOutputIds> GetResultIds(IEnumerable<string> taskIds)
    => ArmoniKClient.GetResultIdsAsync(taskIds.ToList(),
                                       5,
                                       5 * 2000 / Math.Pow(2,
                                                           5),
                                       CancellationToken.None)
                    .Result;


  /// <summary>
  ///   Try to find the result of One task. If there no result, the function return byte[0]
  /// </summary>
  /// <param name="taskId">The Id of the task</param>
  /// <param name="cancellationToken">The optional cancellationToken</param>
  /// <returns>Returns the result or byte[0] if there no result</returns>
  public byte[] GetResult(string            taskId,
                          CancellationToken cancellationToken = default)
  {
    using var _ = Logger.LogFunction(taskId);

    try
    {
      var resultId = GetResultIds(new[]
                                  {
                                    taskId,
                                  })
                     .Single()
                     .OutputIds.Single();

      ArmoniKClient.WaitForAvailability(SessionId.Id,
                                        resultId,
                                        5,
                                        5 * 2000 / Math.Pow(2,
                                                            5),
                                        cancellationToken)
                   .Wait(cancellationToken);

      return ArmoniKClient.DownloadResultAsync(SessionId.Id,
                                               resultId,
                                               5,
                                               5 * 2000 / Math.Pow(2,
                                                                   5),
                                               cancellationToken)
                          .Result!;
    }
    catch (Exception ex)
    {
      throw new ClientResultsException($"Cannot retrieve result for task : {taskId}",
                                       ex,
                                       taskId);
    }
  }


  /// <summary>
  ///   Retrieve results from control plane
  /// </summary>
  /// <param name="taskIds">Collection of task ids</param>
  /// <param name="cancellationToken">The optional cancellationToken</param>
  /// <returns>return a dictionary with key taskId and payload</returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="ArgumentException"></exception>
  public IEnumerable<Tuple<string, byte[]>> GetResults(IEnumerable<string> taskIds,
                                                       CancellationToken   cancellationToken = default)
    => taskIds.AsParallel()
              .Select(id =>
                      {
                        var res = GetResult(id,
                                            cancellationToken);

                        return new Tuple<string, byte[]>(id,
                                                         res);
                      });

  /// <summary>
  ///   Try to get the result if it is available
  /// </summary>
  /// <param name="resultRequest">Request specifying the result to fetch</param>
  /// <param name="cancellationToken">The token used to cancel the operation.</param>
  /// <returns>Returns the result or byte[0] if there no result or null if task is not yet ready</returns>
  /// <exception cref="Exception"></exception>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  // TODO: return a compound type to avoid having a nullable that holds the information and return an empty array.
  public async Task<byte[]?> TryGetResultAsync(ResultRequest     resultRequest,
                                               CancellationToken cancellationToken = default)
  {
    try
    {
      return await ArmoniKClient.DownloadResultAsync(resultRequest.Session,
                                                     resultRequest.ResultId,
                                                     5,
                                                     5 * 2000 / Math.Pow(2,
                                                                         5),
                                                     cancellationToken);
    }
    catch (RpcException e)
    {
      if (e.StatusCode == StatusCode.NotFound)
      {
        return null;
      }

      throw;
    }
    catch (AggregateException ae)
    {
      ae.Handle(exception => exception is RpcException
                                          {
                                            StatusCode: StatusCode.NotFound or StatusCode.Aborted or StatusCode.Cancelled,
                                          } or KeyNotFoundException);
      return null;
    }
  }

  /// <summary>
  ///   Try to find the result of One task. If there no result, the function return byte[0]
  /// </summary>
  /// <param name="taskId">The Id of the task</param>
  /// <param name="checkOutput"></param>
  /// <param name="cancellationToken">The optional cancellationToken</param>
  /// <returns>Returns the result or byte[0] if there no result or null if task is not yet ready</returns>
  // TODO: return a compound type to avoid having a nullable that holds the information and return an empty array.
  [PublicAPI]
  [Obsolete("Use version without the checkOutput parameter.")]
  public byte[]? TryGetResult(string            taskId,
                              bool              checkOutput,
                              CancellationToken cancellationToken = default)
    => TryGetResult(taskId,
                    cancellationToken);


  /// <summary>
  ///   Try to find the result of One task. If there no result, the function return byte[0]
  /// </summary>
  /// <param name="taskId">The Id of the task</param>
  /// <param name="cancellationToken">The optional cancellationToken</param>
  /// <returns>Returns the result or byte[0] if there no result or null if task is not yet ready</returns>
  // TODO: return a compound type to avoid having a nullable that holds the information and return an empty array.
  [PublicAPI]
  public byte[]? TryGetResult(string            taskId,
                              CancellationToken cancellationToken = default)
  {
    using var _ = Logger.LogFunction(taskId);
    var resultId = GetResultIds(new[]
                                {
                                  taskId,
                                })
                   .Single()
                   .OutputIds.Single();

    var resultReply = TryGetResultAsync(new ResultRequest
                                        {
                                          ResultId = resultId,
                                          Session  = SessionId.Id,
                                        },
                                        cancellationToken)
      .Result;

    return resultReply;
  }

  /// <summary>
  ///   Try to get result of a list of taskIds
  /// </summary>
  /// <param name="resultIds">A list of result ids</param>
  /// <returns>Returns an Enumerable pair of </returns>
  public IList<Tuple<string, byte[]>> TryGetResults(IList<string> resultIds)
  {
    var resultStatus = GetResultStatus(resultIds);

    if (!resultStatus.IdsReady.Any() && !resultStatus.IdsNotReady.Any())
    {
      if (resultStatus.IdsError.Any() || resultStatus.IdsResultError.Any())
      {
        var taskList = string.Join(", ",
                                   resultStatus.IdsResultError.Select(x => x.TaskId));

        if (resultStatus.IdsError.Any())
        {
          if (resultStatus.IdsResultError.Any())
          {
            taskList += ", ";
          }

          taskList += string.Join(", ",
                                  resultStatus.IdsError);
        }

        var taskIdInError = resultStatus.IdsError.Any()
                              ? resultStatus.IdsError[0]
                              : resultStatus.IdsResultError[0].TaskId;

        const string message = "The missing result is in error or canceled. "                                                          +
                               "Please check log for more information on Armonik grid server list of taskIds in Error: [{taskList}]\n" +
                               "1st result id where the task which should create it is in error : {taskIdInError}";

        Logger.LogError(message,
                        taskList,
                        taskIdInError);

        throw new
          ClientResultsException($"The missing result is in error or canceled. Please check log for more information on Armonik grid server list of taskIds in Error: [{taskList}]" +
                                 $"1st result id where the task which should create it is in error : {taskIdInError}",
                                 resultStatus.IdsError.ToArray());
      }
    }

    return resultStatus.IdsReady.Select(resultStatusData =>
                                        {
                                          var res = ArmoniKClient.DownloadResultAsync(SessionId.Id,
                                                                                      resultStatusData.ResultId)
                                                                 .Result;
                                          return res == null
                                                   ? null
                                                   : new Tuple<string, byte[]>(resultStatusData.TaskId,
                                                                               res);
                                        })
                       .Where(tuple => tuple is not null)
                       .Select(tuple => tuple!)
                       .ToList();
  }

  /// <summary>
  ///   Creates the results metadata
  /// </summary>
  /// <param name="resultNames">Results names</param>
  /// <returns>Dictionary where each result name is associated with its result id</returns>
  [PublicAPI]
  public IDictionary<string, string> CreateResultsMetadata(IEnumerable<string> resultNames)
    => ArmoniKClient.CreateResultMetaDataAsync(SessionId.Id,
                                               resultNames.ToList())
                    .Result.ToImmutableDictionary(pair => pair.Key,
                                                  pair => pair.Value);
}
