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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Client.Common.Status;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Common;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.Utils;

using Grpc.Core;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;

/// <summary>
///   This class is instantiated by ServiceFactory and allows to execute task on ArmoniK
///   Grid.
/// </summary>
[MarkDownDoc]
public class Service : AbstractClientService, ISubmitterService
{
  private readonly RequestTaskMap requestTaskMap_ = new();


  private readonly SemaphoreSlim semaphoreSlim_;

  /// <summary>
  ///   The default constructor to open connection with the control plane
  ///   and create the session to ArmoniK
  /// </summary>
  /// <param name="properties">The properties containing TaskOptions and information to communicate with Control plane and </param>
  /// <param name="loggerFactory"></param>
  public Service(Properties      properties,
                 ILoggerFactory? loggerFactory = null)
    : base(loggerFactory)
  {
    var timeOutSending = properties.TimeTriggerBuffer ?? TimeSpan.FromSeconds(1);

    var maxTasksPerBuffer = properties.MaxTasksPerBuffer;

    semaphoreSlim_ = new SemaphoreSlim(properties.MaxConcurrentBuffers * maxTasksPerBuffer);

    SessionServiceFactory = new SessionServiceFactory(LoggerFactory);

    SessionService = SessionServiceFactory.CreateSession(properties);

    CancellationResultTaskSource = new CancellationTokenSource();

    HandlerResponse = Task.Run(ResultTask,
                               CancellationResultTaskSource.Token);

    Logger = LoggerFactory.CreateLogger<Service>();
    Logger.BeginPropertyScope(("SessionId", SessionService.SessionId),
                              ("Class", "Service"));

    BufferSubmit = new BatchUntilInactiveBlock<BlockRequest>(maxTasksPerBuffer,
                                                             timeOutSending,
                                                             new ExecutionDataflowBlockOptions
                                                             {
                                                               BoundedCapacity        = properties.MaxParallelChannels,
                                                               MaxDegreeOfParallelism = properties.MaxParallelChannels,
                                                             });

    BufferSubmit.ExecuteAsync(blockRequests =>
                              {
                                var blockRequestList = blockRequests.ToList();

                                try
                                {
                                  if (blockRequestList.Count == 0)
                                  {
                                    return;
                                  }

                                  Logger.LogInformation("Submitting buffer of {count} task...",
                                                        blockRequestList.Count);
                                  var query = blockRequestList.GroupBy(blockRequest => blockRequest.TaskOptions);

                                  foreach (var groupBlockRequest in query)
                                  {
                                    var maxRetries = groupBlockRequest.First()
                                                                      .MaxRetries;
                                    //Generate resultId
                                    var resultsIds = SessionService.CreateResultsMetadata(groupBlockRequest.Select(_ => Guid.NewGuid()
                                                                                                                            .ToString()))
                                                                   .Values.ToList();

                                    foreach (var (request, index) in groupBlockRequest.Select((r,
                                                                                               i) => (r, i)))
                                    {
                                      request.ResultId = resultsIds[index];
                                    }

                                    for (var retry = 0; retry < maxRetries; retry++)
                                    {
                                      try
                                      {
                                        var taskIds =
                                          SessionService.SubmitTasksWithDependencies(groupBlockRequest.Select(x => new Tuple<string, byte[], IList<string>>(x.ResultId,
                                                                                                                                                            x.Payload!
                                                                                                                                                             .Serialize(),
                                                                                                                                                            Array
                                                                                                                                                              .Empty<
                                                                                                                                                                string>())),
                                                                                     1,
                                                                                     groupBlockRequest.First()
                                                                                                      .TaskOptions);


                                        var ids            = taskIds.ToList();
                                        var mapTaskResults = SessionService.GetResultIds(ids);
                                        var taskIdsResultIds = mapTaskResults.ToDictionary(result => result.OutputIds.Single(),
                                                                                           result => result.TaskId);


                                        foreach (var pairTaskIdResultId in taskIdsResultIds)
                                        {
                                          var blockRequest = groupBlockRequest.FirstOrDefault(x => x.ResultId == pairTaskIdResultId.Key) ??
                                                             throw new InvalidOperationException($"Cannot find BlockRequest with result id {pairTaskIdResultId.Value}");

                                          ResultHandlerDictionary[pairTaskIdResultId.Value] = blockRequest.Handler;

                                          requestTaskMap_.PutResponse(blockRequest.SubmitId,
                                                                      pairTaskIdResultId.Value);
                                        }

                                        if (ids.Count > taskIdsResultIds.Count)
                                        {
                                          Logger.LogWarning("Fail to submit all tasks at once, retry with missing tasks");

                                          throw new Exception("Fail to submit all tasks at once. Retrying...");
                                        }

                                        break;
                                      }
                                      catch (Exception e)
                                      {
                                        if (retry >= maxRetries - 1)
                                        {
                                          Logger.LogError(e,
                                                          "Fail to retry {count} times of submission. Stop trying to submit",
                                                          maxRetries);
                                          throw;
                                        }

                                        Logger.LogWarning(e,
                                                          "Fail to submit, {retry}/{maxRetries} retrying",
                                                          retry,
                                                          maxRetries);

                                        //Delay before submission
                                        Task.Delay(TimeSpan.FromMilliseconds(1000));
                                      }
                                    }

                                    foreach (var blockRequest in groupBlockRequest)
                                    {
                                      blockRequest.Lock.Release();
                                    }
                                  }
                                }
                                catch (Exception e)
                                {
                                  Logger.LogError(e,
                                                  "Fail to submit buffer with {count} tasks inside",
                                                  blockRequestList.Count);

                                  requestTaskMap_.BufferFailures(blockRequestList.Select(block => block.SubmitId),
                                                                 e);
                                }
                              });
  }

  /// <summary>
  ///   Property Get the SessionId
  /// </summary>
  [PublicAPI]
  public SessionService SessionService { get; }


  private BatchUntilInactiveBlock<BlockRequest> BufferSubmit { get; }

  private ILogger Logger { get; }

  private SessionServiceFactory SessionServiceFactory { get; }

  private CancellationTokenSource CancellationResultTaskSource { get; }

  /// <summary>
  ///   The handler to send the response
  /// </summary>
  private Task HandlerResponse { get; }

  /// <summary>
  ///   The sessionId
  /// </summary>
  public string SessionId
    => SessionService.SessionId.Id;


  /// <inheritdoc />
  public IEnumerable<string> Submit(string                    methodName,
                                    IEnumerable<object[]>     arguments,
                                    IServiceInvocationHandler handler,
                                    int                       maxRetries  = 5,
                                    TaskOptions?              taskOptions = null)
    => Submit(methodName,
              arguments.Select(ProtoSerializer.Serialize),
              handler,
              maxRetries,
              null,
              false);


  /// <inheritdoc />
  public IEnumerable<string> Submit(string                    methodName,
                                    IEnumerable<byte[]>       arguments,
                                    IServiceInvocationHandler handler,
                                    int                       maxRetries  = 5,
                                    TaskOptions?              taskOptions = null)
    => Submit(methodName,
              arguments,
              handler,
              maxRetries,
              taskOptions,
              true);

  /// <inheritdoc />
  public async Task<string> SubmitAsync(string                    methodName,
                                        object[]                  argument,
                                        IServiceInvocationHandler handler,
                                        int                       maxRetries  = 5,
                                        TaskOptions?              taskOptions = null,
                                        CancellationToken         token       = default)
    => await SubmitAsync(methodName,
                         ProtoSerializer.Serialize(argument),
                         handler,
                         maxRetries,
                         taskOptions,
                         false,
                         token)
         .ConfigureAwait(false);


  /// <inheritdoc />
  public async Task<string> SubmitAsync(string                    methodName,
                                        byte[]                    argument,
                                        IServiceInvocationHandler handler,
                                        int                       maxRetries  = 5,
                                        TaskOptions?              taskOptions = null,
                                        CancellationToken         token       = default)
    => await SubmitAsync(methodName,
                         ProtoSerializer.Serialize(argument),
                         handler,
                         maxRetries,
                         taskOptions,
                         true,
                         token)
         .ConfigureAwait(false);

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public override void Dispose()
  {
    CancellationResultTaskSource.Cancel();
    HandlerResponse.Wait();
    HandlerResponse.Dispose();
    semaphoreSlim_.Dispose();

    GC.SuppressFinalize(this);
  }

  /// <summary>
  ///   The method submit list of task with Enumerable list of serialized arguments that will be already serialized for
  ///   byte[] MethodName(byte[] argument).
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="arguments">List of serialized arguments that will already serialize for MethodName.</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="serializedArguments">defines whether the arguments should be passed as serialized to the compute function</param>
  /// <returns>Return the taskId string</returns>
  private IEnumerable<string> Submit(string                    methodName,
                                     IEnumerable<byte[]>       arguments,
                                     IServiceInvocationHandler handler,
                                     int                       maxRetries,
                                     TaskOptions?              taskOptions,
                                     bool                      serializedArguments)
  {
    var armonikPayloads = arguments.Select(args => new ArmonikPayload(methodName,
                                                                      args,
                                                                      serializedArguments));

    var taskIds = SessionService.SubmitTasks(armonikPayloads.Select(p => p.Serialize()),
                                             maxRetries,
                                             taskOptions);
    var submitted = taskIds as string[] ?? taskIds.ToArray();
    foreach (var taskId in submitted)
    {
      ResultHandlerDictionary[taskId] = handler;
    }

    return submitted;
  }

  /// <summary>
  ///   The method submit with one serialized argument that will send as byte[] MethodName(byte[]   argument).
  /// </summary>
  /// <param name="methodName">The name of the method inside the service</param>
  /// <param name="argument">One serialized argument that will already serialize for MethodName.</param>
  /// <param name="handler">The handler callBack implemented as IServiceInvocationHandler to get response or result or error</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="serializedArguments">defines whether the arguments should be passed as serialized to the compute function</param>
  /// <param name="token">The cancellation token to set to cancel the async task</param>
  /// <returns>Return the taskId string</returns>
  private async Task<string> SubmitAsync(string                    methodName,
                                         byte[]                    argument,
                                         IServiceInvocationHandler handler,
                                         int                       maxRetries,
                                         TaskOptions?              taskOptions,
                                         bool                      serializedArguments,
                                         CancellationToken         token)
  {
    await semaphoreSlim_.WaitAsync(token);

    return await SubmitAsync(new BlockRequest
                             {
                               SubmitId = Guid.NewGuid(),
                               Payload = new ArmonikPayload(methodName,
                                                            argument,
                                                            serializedArguments),
                               Handler     = handler,
                               MaxRetries  = maxRetries,
                               TaskOptions = taskOptions ?? SessionService.TaskOptions,
                               Lock        = semaphoreSlim_,
                             },
                             token)
             .ConfigureAwait(false);
  }

  private async Task<string> SubmitAsync(BlockRequest      blockRequest,
                                         CancellationToken token = default)
  {
    await BufferSubmit.SendAsync(blockRequest,
                                 token)
                      .ConfigureAwait(false);

    return await requestTaskMap_.GetResponseAsync(blockRequest.SubmitId);
  }

  /// <summary>
  ///   This function execute code locally with the same configuration as Armonik Grid execution
  ///   The method needs the Service to execute, the method name to call and arguments of method to pass
  /// </summary>
  /// <param name="service">The instance of object containing the method to call</param>
  /// <param name="methodName">The string name of the method</param>
  /// <param name="arguments">the array of object to pass as arguments for the method</param>
  /// <returns>Returns an object as result of the method call</returns>
  /// <exception cref="WorkerApiException"></exception>
  // TODO: mark with [PublicApi] ?
  // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
  public ServiceResult LocalExecute(object service,
#pragma warning restore CA1822
                                    string   methodName,
                                    object[] arguments)
  {
    var methodInfo = service.GetType()
                            .GetMethod(methodName) ?? throw new InvalidOperationException($"MethodName [{methodName}] was not found");


    var result = methodInfo.Invoke(service,
                                   arguments)!;

    return new ServiceResult(Guid.NewGuid()
                                 .ToString(),
                             result);
  }

  /// <summary>
  ///   This method is used to execute task and waiting after the result.
  ///   the method will return the result of the execution until the grid returns the task result
  /// </summary>
  /// <param name="methodName">The string name of the method</param>
  /// <param name="arguments">the array of object to pass as arguments for the method</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Returns a tuple with the taskId string and an object as result of the method call</returns>
  // TODO: mark with [PublicApi] ?
  // ReSharper disable once UnusedMember.Global
  public ServiceResult Execute(string       methodName,
                               object[]     arguments,
                               int          maxRetries  = 5,
                               TaskOptions? taskOptions = null)
    => Execute(methodName,
               ProtoSerializer.Serialize(arguments),
               maxRetries,
               taskOptions,
               false);

  /// <summary>
  ///   This method is used to execute task and waiting after the result.
  ///   the method will return the result of the execution until the grid returns the task result
  /// </summary>
  /// <param name="methodName">The string name of the method</param>
  /// <param name="dataArg">the array of byte to pass as argument for the methodName(byte[] dataArg)</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <returns>Returns a tuple with the taskId string and an object as result of the method call</returns>
  // TODO: mark with [PublicApi] ?
  // ReSharper disable once UnusedMember.Global
  public ServiceResult Execute(string       methodName,
                               byte[]       dataArg,
                               int          maxRetries  = 5,
                               TaskOptions? taskOptions = null)
    => Execute(methodName,
               dataArg,
               maxRetries,
               taskOptions,
               true);

  /// <summary>
  ///   This method is used to execute task and waiting after the result.
  ///   the method will return the result of the execution until the grid returns the task result
  /// </summary>
  /// <param name="methodName">The string name of the method</param>
  /// <param name="dataArg">the array of byte to pass as argument for the methodName(byte[] dataArg)</param>
  /// <param name="maxRetries">The number of retry before fail to submit task. Default = 5 retries</param>
  /// <param name="taskOptions">
  ///   TaskOptions argument to override default taskOptions in Session.
  ///   If non null it will override the default taskOptions in SessionService for client or given by taskHandler for worker
  /// </param>
  /// <param name="serializedArguments">defines whether the arguments should be passed as serialized to the compute function</param>
  /// <returns>Returns a tuple with the taskId string and an object as result of the method call</returns>
  private ServiceResult Execute(string       methodName,
                                byte[]       dataArg,
                                int          maxRetries,
                                TaskOptions? taskOptions,
                                bool         serializedArguments)
  {
    ArmonikPayload unifiedPayload = new(methodName,
                                        dataArg,
                                        serializedArguments);

    var       taskId = "not-TaskId";
    object?[] result;

    try
    {
      taskId = SessionService.SubmitTask(unifiedPayload.Serialize(),
                                         maxRetries: maxRetries,
                                         taskOptions: taskOptions);

      result = ProtoSerializer.Deserialize<object[]>(SessionService.GetResult(taskId))!;
    }
    catch (Exception e)
    {
      var status = SessionService.GetTaskStatus(taskId);

      var details = string.Empty;

      // ReSharper disable once InvertIf
      if (status != ArmonikTaskStatusCode.TaskCompleted)
      {
        var output = SessionService.TryGetTaskError(taskId);
        details = output ?? e.Message + e.StackTrace;
      }

      throw new ServiceInvocationException(e is AggregateException
                                             ? e.InnerException ?? e
                                             : e,
                                           status)
            {
              OutputDetails = details,
            };
    }

    return new ServiceResult(taskId,
                             result[0]);
  }

  /// <summary>
  ///   Retrieves the results for the given taskIds.
  /// </summary>
  /// <param name="taskIds">The taskIds to retrieve results for.</param>
  /// <param name="responseHandler">The action to take when a response is received.</param>
  /// <param name="errorHandler">The action to take when an error occurs.</param>
  /// <param name="chunkResultSize">The size of the chunk to retrieve results in.</param>
  private void ProxyTryGetResults(IEnumerable<string>                           taskIds,
                                  Action<string, byte[]>                        responseHandler,
                                  Action<string, ArmonikTaskStatusCode, string> errorHandler,
                                  int                                           chunkResultSize = 200)
  {
    var missing  = new HashSet<string>(taskIds);
    var holdPrev = missing.Count;
    var waitInSeconds = new List<int>
                        {
                          10,
                          1000,
                          5000,
                          10000,
                          20000,
                          30000,
                        };
    var idx = 0;

    while (missing.Count != 0)
    {
      foreach (var bucket in missing.ToList()
                                    .ToChunks(chunkResultSize))
      {
        var resultStatusCollection = SessionService.GetResultStatus(bucket);

        foreach (var resultStatusData in resultStatusCollection.IdsReady)
        {
          try
          {
            Logger.LogTrace("Response handler for {taskId}",
                            resultStatusData.TaskId);
            responseHandler(resultStatusData.TaskId,
                            Retry.WhileException(5,
                                                 2000,
                                                 retry =>
                                                 {
                                                   if (retry > 1)
                                                   {
                                                     Logger.LogWarning("Try {try} for {funcName}",
                                                                       retry,
                                                                       nameof(SessionService.TryGetResultAsync));
                                                   }

                                                   return SessionService.TryGetResultAsync(new ResultRequest
                                                                                           {
                                                                                             ResultId = resultStatusData.ResultId,
                                                                                             Session  = SessionId,
                                                                                           },
                                                                                           CancellationToken.None)
                                                                        .Result;
                                                 },
                                                 true,
                                                 typeof(IOException),
                                                 typeof(RpcException))!);
          }
          catch (Exception e)
          {
            Logger.LogWarning(e,
                              "Response handler for {taskId} threw an error",
                              resultStatusData.TaskId);
            try
            {
              errorHandler(resultStatusData.TaskId,
                           ArmonikTaskStatusCode.TaskFailed,
                           e.Message + e.StackTrace);
            }
            catch (Exception e2)
            {
              Logger.LogError(e2,
                              "An error occurred while handling another error: {details}",
                              e);
            }
          }
        }

        missing.ExceptWith(resultStatusCollection.IdsReady.Select(x => x.TaskId));

        foreach (var resultStatusData in resultStatusCollection.IdsResultError)
        {
          string details;

          var taskStatus = SessionService.GetTaskStatus(resultStatusData.TaskId);

          switch (taskStatus)
          {
            case ArmonikTaskStatusCode.TaskCancelled:
              details = $"Task {resultStatusData.TaskId} was canceled";
              break;
            default:
              var outputInfo = SessionService.TryGetTaskError(resultStatusData.TaskId);
              details = outputInfo ?? "Result is in status : " + resultStatusData.Status + ", look for task in error in logs.";
              break;
          }

          Logger.LogDebug("Error handler for {taskId}, {taskStatus}: {details}",
                          resultStatusData.TaskId,
                          taskStatus,
                          details);
          try
          {
            errorHandler(resultStatusData.TaskId,
                         taskStatus,
                         details);
          }
          catch (Exception e)
          {
            Logger.LogError(e,
                            "An error occurred while handling a Task error {status}: {details}",
                            taskStatus,
                            details);
          }
        }

        missing.ExceptWith(resultStatusCollection.IdsResultError.Select(x => x.TaskId));

        foreach (var resultStatusData in resultStatusCollection.Canceled)
        {
          try
          {
            errorHandler(resultStatusData.TaskId,
                         ArmonikTaskStatusCode.Unknown,
                         "Task is missing");
          }
          catch (Exception e)
          {
            Logger.LogError(e,
                            "An error occurred while handling a Task error {status}: {details}",
                            TaskStatus.Unspecified,
                            "Task is missing");
          }
        }

        missing.ExceptWith(resultStatusCollection.Canceled.Select(x => x.TaskId));

        if (holdPrev == missing.Count)
        {
          idx = idx >= waitInSeconds.Count - 1
                  ? waitInSeconds.Count - 1
                  : idx                 + 1;

          Logger.LogDebug("No Results are ready. Wait for {timeWait} seconds before new retry",
                          waitInSeconds[idx] / 1000);
        }
        else
        {
          idx = 0;
        }

        holdPrev = missing.Count;

        Thread.Sleep(waitInSeconds[idx]);
      }
    }
  }

  private void ResultTask()
  {
    while (!(CancellationResultTaskSource.Token.IsCancellationRequested && ResultHandlerDictionary.IsEmpty))
    {
      try
      {
        if (!ResultHandlerDictionary.IsEmpty)
        {
          ProxyTryGetResults(ResultHandlerDictionary.Keys.ToList(),
                             (taskId,
                              byteResult) =>
                             {
                               try
                               {
                                 var result = ProtoSerializer.Deserialize<object?[]>(byteResult);
                                 ResultHandlerDictionary[taskId]
                                   .HandleResponse(result![0],
                                                   taskId);
                               }
                               catch (Exception e)
                               {
                                 const ArmonikTaskStatusCode statusCode = ArmonikTaskStatusCode.Unknown;

                                 ServiceInvocationException ex;

                                 var ae = e as AggregateException;

                                 if (ae is not null && ae.InnerExceptions.Count > 1)
                                 {
                                   ex = new ServiceInvocationException(ae,
                                                                       statusCode);
                                 }
                                 else if (ae is not null)
                                 {
                                   ex = new ServiceInvocationException(ae.InnerException ?? ae,
                                                                       statusCode);
                                 }
                                 else
                                 {
                                   ex = new ServiceInvocationException(e,
                                                                       statusCode);
                                 }

                                 ResultHandlerDictionary[taskId]
                                   .HandleError(ex,
                                                taskId);
                               }
                               finally
                               {
                                 ResultHandlerDictionary.TryRemove(taskId,
                                                                   out _);
                               }
                             },
                             (taskId,
                              taskStatus,
                              ex) =>
                             {
                               try
                               {
                                 var statusCode = taskStatus;

                                 ResultHandlerDictionary[taskId]
                                   .HandleError(new ServiceInvocationException(ex,
                                                                               statusCode),
                                                taskId);
                               }
                               finally
                               {
                                 ResultHandlerDictionary.TryRemove(taskId,
                                                                   out _);
                               }
                             });
        }
        else
        {
          Thread.Sleep(100);
        }
      }
      catch (Exception e)
      {
        Logger.LogError("An error occurred while fetching results: {e}",
                        e);
      }
    }

    if (!ResultHandlerDictionary.IsEmpty)
    {
      Logger.LogWarning("Results not processed : [{resultsNotProcessed}]",
                        string.Join(", ",
                                    ResultHandlerDictionary.Keys));
    }
  }

  /// <summary>
  ///   Class to return TaskId and the result
  /// </summary>
  public class ServiceResult
  {
    /// <summary>
    ///   Constructor
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="result"></param>
    public ServiceResult(string  taskId,
                         object? result)
    {
      TaskId = taskId;
      Result = result;
    }

    /// <summary>
    ///   The getter to return the taskId
    /// </summary>
    // TODO: mark with [PublicApi] ?
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string TaskId { get; }

    /// <summary>
    ///   The getter to return the result in object type format
    /// </summary>
    // TODO: mark with [PublicApi] ?
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public object? Result { get; }
  }
}
