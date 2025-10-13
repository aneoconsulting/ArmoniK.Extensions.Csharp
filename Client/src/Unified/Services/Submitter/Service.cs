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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using ArmoniK.Api.Client;
using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.DevelopmentKit.Client.Common;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;
using ArmoniK.DevelopmentKit.Client.Common.Status;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Common;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.Utils;
using ArmoniK.Utils.Pool;

using Grpc.Core;
using Grpc.Net.Client;

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
  /// <summary>
  ///   The default constructor to open connection with the control plane
  ///   and create the session to ArmoniK, or use an existing session
  /// </summary>
  /// <remarks>
  ///   If <paramref name="sessionId" /> is null, a new session is created.
  ///   Otherwise, the specified session is opened, and the task options configured
  ///   during the session creation will be used instead of <paramref name="properties" />'s task options.
  /// </remarks>
  /// <param name="properties">The properties containing TaskOptions and information to communicate with Control plane and </param>
  /// <param name="loggerFactory"></param>
  /// <param name="sessionId">The ID of the session to open</param>
  public Service(Properties      properties,
                 ILoggerFactory? loggerFactory = null,
                 string?         sessionId     = null)
    : base(loggerFactory)
  {
    Properties            = properties;
    SessionServiceFactory = new SessionServiceFactory(LoggerFactory);
    SessionService = string.IsNullOrEmpty(sessionId)
                       ? SessionServiceFactory.CreateSession(properties)
                       : SessionServiceFactory.OpenSession(properties,
                                                           sessionId!);
    CancellationResultTaskSource = new CancellationTokenSource();
    Logger                       = LoggerFactory.CreateLogger<Service>();
    Logger.BeginPropertyScope(("SessionId", SessionService.SessionId),
                              ("Class", "Service"));

    var submitChannel = Channel.CreateUnbounded<TaskSubmission>();
    SubmitChannel = submitChannel.Writer;

    var cancellationToken = CancellationResultTaskSource.Token;
    var requests          = submitChannel.Reader.ToAsyncEnumerable(cancellationToken);

    SubmitTask = Task.Run(() => requests.ToChunksAsync(properties.MaxTasksPerBuffer,
                                                       properties.TimeTriggerBuffer ?? TimeSpan.FromSeconds(1),
                                                       cancellationToken)
                                        .ParallelForEach(new ParallelTaskOptions(properties.MaxConcurrentBuffers,
                                                                                 cancellationToken),
                                                         async chunk =>
                                                         {
                                                           Logger?.LogInformation("Submitting batch of {NbTask}/{MaxTask}",
                                                                                  chunk.Length,
                                                                                  properties.MaxTasksPerBuffer);

                                                           List<(string taskId, string resultId)> tasks;
                                                           try
                                                           {
                                                             var taskRequests = chunk.Select(req => ((string?)null, req.Payload, req.Dependencies, req.TaskOptions));
                                                             var response = SessionService.ChunkSubmitTasksWithDependenciesAsync(taskRequests,
                                                                                                                                 cancellationToken: cancellationToken);
                                                             tasks = await response.ToListAsync(cancellationToken)
                                                                                   .ConfigureAwait(false);
                                                           }
                                                           catch (Exception e)
                                                           {
                                                             Logger?.LogError(e,
                                                                              "Error while submitting tasks");
                                                             foreach (var req in chunk)
                                                             {
                                                               req.Tcs.SetException(e);
                                                             }

                                                             return;
                                                           }

                                                           foreach (var (task, submission) in tasks.Zip(chunk,
                                                                                                        (s,
                                                                                                         submission) => (s, submission)))
                                                           {
                                                             ResultHandlerDictionary[task.taskId] = submission.Handler;
                                                             submission.Tcs.SetResult(task.taskId);
                                                           }
                                                         }));


    HandlerResponse = Task.Run(() => ResultTask(CancellationResultTaskSource.Token),
                               CancellationResultTaskSource.Token);
  }

  private Task                          SubmitTask    { get; }
  private ChannelWriter<TaskSubmission> SubmitChannel { get; }

  private Properties Properties { get; }

  /// <summary>
  ///   Property Get the SessionId
  /// </summary>
  [PublicAPI]
  public SessionService SessionService { get; }

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
                         argument,
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

    foreach (var awaitable in new[]
                              {
                                HandlerResponse,
                                SubmitTask,
                              })
    {
      try
      {
        awaitable.WaitSync();
      }
      catch (OperationCanceledException)
      {
      }

      awaitable.Dispose();
    }

    CancellationResultTaskSource.Dispose();

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
    var tcs = new TaskCompletionSource<string>();
    await SubmitChannel.WriteAsync(new TaskSubmission(new ArmonikPayload(methodName,
                                                                         argument,
                                                                         serializedArguments).Serialize(),
                                                      Array.Empty<string>(),
                                                      taskOptions,
                                                      handler,
                                                      tcs),
                                   token)
                       .ConfigureAwait(false);

    return await tcs.Task.ConfigureAwait(false);
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
      if (status != TaskStatus.Completed)
      {
        var output = SessionService.GetTaskOutputInfo(taskId);
        details = output.TypeCase == Output.TypeOneofCase.Error
                    ? output.Error.Details
                    : e.Message + e.StackTrace;
      }

      throw new ServiceInvocationException(e is AggregateException
                                             ? e.InnerException ?? e
                                             : e,
                                           status.ToArmonikStatusCode())
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
  /// <param name="cancellationToken"></param>
  private ValueTask<int> ProxyTryGetResults(IEnumerable<string>                taskIds,
                                            Action<string, byte[]>             responseHandler,
                                            Action<string, TaskStatus, string> errorHandler,
                                            int                                chunkResultSize   = 200,
                                            CancellationToken                  cancellationToken = default)
    => taskIds.ToChunks(chunkResultSize)
              .ToAsyncEnumerable()
              // Get all result status (by chunk)
              .SelectAwait(bucket => SessionService.GetResultStatusAsync(bucket,
                                                                         cancellationToken))

              // Aggregate all results that are ready, in error and not found
              .SelectMany(results => results.IdsReady.Concat(results.IdsResultError)
                                            .Concat(results.Canceled)
                                            .ToAsyncEnumerable())

              // Process all results in parallel
              .ParallelSelect(new ParallelTaskOptions(Properties.MaxParallelChannels),
                              async result =>
                              {
                                switch (result.Status)
                                {
                                  // Result has never been submitted
                                  case ResultStatus.Notfound:
                                    try
                                    {
                                      errorHandler(result.TaskId,
                                                   TaskStatus.Unspecified,
                                                   "Task is missing");
                                    }
                                    catch (Exception e)
                                    {
                                      Logger.LogError(e,
                                                      "An error occurred while handling a Task error {status}: {details}",
                                                      TaskStatus.Unspecified,
                                                      "Task is missing");
                                    }

                                    break;

                                  // Result has been completed
                                  case ResultStatus.Completed:
                                    try
                                    {
                                      Logger.LogTrace("Response handler for {taskId}",
                                                      result.TaskId);

                                      // Download the result data with retry
                                      var data = await Retry.WhileException(5,
                                                                            2000,
                                                                            async retry =>
                                                                            {
                                                                              if (retry > 1)
                                                                              {
                                                                                Logger.LogWarning("Try {try} for {funcName}",
                                                                                                  retry,
                                                                                                  nameof(SessionService.TryGetResultAsync));
                                                                              }

                                                                              await using var channel = await SessionService.ChannelPool.GetAsync(cancellationToken)
                                                                                                                            .ConfigureAwait(false);
                                                                              var resultsClient = new Results.ResultsClient(channel);

                                                                              try
                                                                              {
                                                                                return await resultsClient.DownloadResultData(SessionId,
                                                                                                                              result.ResultId,
                                                                                                                              cancellationToken)
                                                                                                          .ConfigureAwait(false);
                                                                              }
                                                                              catch (Exception e)
                                                                              {
                                                                                channel.Exception = e;
                                                                                throw;
                                                                              }
                                                                            },
                                                                            true,
                                                                            Logger,
                                                                            cancellationToken,
                                                                            typeof(IOException),
                                                                            typeof(RpcException))
                                                            .ConfigureAwait(false);
                                      responseHandler(result.TaskId,
                                                      data);
                                    }
                                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                                    {
                                      throw;
                                    }
                                    catch (Exception e)
                                    {
                                      Logger.LogWarning(e,
                                                        "Response handler for {taskId} threw an error",
                                                        result.TaskId);
                                      try
                                      {
                                        errorHandler(result.TaskId,
                                                     TaskStatus.Error,
                                                     e.Message + e.StackTrace);
                                      }
                                      catch (Exception e2)
                                      {
                                        Logger.LogError(e2,
                                                        "An error occurred while handling another error: {details}",
                                                        e);
                                      }
                                    }

                                    break;

                                  // Result has not been completed and is still waiting
                                  case ResultStatus.Created:
                                    return 0;

                                  // Result is in error
                                  case ResultStatus.Unspecified:
                                  case ResultStatus.Aborted:
                                  default:
                                    string details;

                                    // Get Task status to produce better error messages
                                    var taskStatus = await SessionService.GetTaskStatusAsync(result.TaskId,
                                                                                             cancellationToken)
                                                                         .ConfigureAwait(false);

                                    switch (taskStatus)
                                    {
                                      case TaskStatus.Cancelling:
                                      case TaskStatus.Cancelled:
                                        details = $"Task {result.TaskId} was canceled";
                                        break;
                                      default:
                                        var outputInfo = await SessionService.GetTaskOutputInfoAsync(result.TaskId,
                                                                                                     cancellationToken)
                                                                             .ConfigureAwait(false);
                                        details = outputInfo.TypeCase == Output.TypeOneofCase.Error
                                                    ? outputInfo.Error.Details
                                                    : "Result is in status : " + result.Status + ", look for task in error in logs.";
                                        break;
                                    }

                                    Logger.LogDebug("Error handler for {taskId}, {taskStatus}: {details}",
                                                    result.TaskId,
                                                    taskStatus,
                                                    details);
                                    try
                                    {
                                      errorHandler(result.TaskId,
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

                                    break;
                                }

                                // Result has been handled and has been removed from the processingDict
                                return 1;
                              })

              // Count the number of results that were removed from the processingDict (resultHandler called)
              .SumAsync(cancellationToken);

  private async Task ResultTask(CancellationToken cancellationToken)
  {
    var waitInMilliseconds = new[]
                             {
                               10,
                               1000,
                               5000,
                               10000,
                               20000,
                               30000,
                             };
    var waitIndex = 0;

    while (!(CancellationResultTaskSource.Token.IsCancellationRequested && ResultHandlerDictionary.IsEmpty))
    {
      try
      {
        var n = await ProxyTryGetResults(ResultHandlerDictionary.Keys.ToList(),
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
                                             const ArmonikStatusCode statusCode = ArmonikStatusCode.Unknown;

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
                                             var statusCode = taskStatus.ToArmonikStatusCode();

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
                                         },
                                         cancellationToken: cancellationToken);

        if (n > 0)
        {
          waitIndex = 0;
        }
        else
        {
          waitIndex += 1;

          if (waitIndex >= waitInMilliseconds.Length)
          {
            waitIndex = waitInMilliseconds.Length - 1;
          }

          Logger.LogDebug("No Results are ready. Wait for {timeWait} seconds before new retry",
                          waitInMilliseconds[waitIndex] / 1000);
        }

        await Task.Delay(TimeSpan.FromMilliseconds(waitInMilliseconds[waitIndex]),
                         cancellationToken)
                  .ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        break;
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
  ///   Get a new channel to communicate with the control plane
  /// </summary>
  /// <returns>gRPC channel</returns>
  // TODO: Refactor test to remove this
  // ReSharper disable once UnusedMember.Global
  public ObjectPool<GrpcChannel> GetChannelPool()
    => SessionService.ChannelPool;

  private record TaskSubmission(
    byte[]                        Payload,
    IList<string>                 Dependencies,
    TaskOptions?                  TaskOptions,
    IServiceInvocationHandler     Handler,
    TaskCompletionSource<string>? Tcs);

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
