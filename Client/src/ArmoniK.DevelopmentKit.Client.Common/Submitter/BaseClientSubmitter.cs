// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client.Submitter;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Status;
using ArmoniK.DevelopmentKit.Common.Exceptions;

using Grpc.Core;

using JetBrains.Annotations;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Common.Submitter
{
  /// <summary>
  /// Base Object for all Client submitter
  ///
  /// Need to pass the child object Class Type
  /// </summary>
  public class BaseClientSubmitter<T>
  {
    /// <summary>
    /// Base Object for all Client submitter
    /// </summary>
    /// <param name="loggerFactory">the logger factory to pass for root object</param>
    public BaseClientSubmitter(ILoggerFactory loggerFactory)
    {
      Logger = loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    ///   Set or Get TaskOptions with inside MaxDuration, Priority, AppName, VersionName and AppNamespace
    /// </summary>
    public TaskOptions TaskOptions { get; set; }

    /// <summary>
    ///   Get SessionId object stored during the call of SubmitTask, SubmitSubTask,
    ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public Session SessionId { get; protected set; }


#pragma warning restore CS1591

    /// <summary>
    /// The logger to call the generate log in Seq
    /// </summary>

    protected ILogger<T> Logger { get; set; }

    /// <summary>
    /// The submitter and receiver Service to submit, wait and get the result
    /// </summary>
    protected Api.gRPC.V1.Submitter.Submitter.SubmitterClient ControlPlaneService { get; set; }


    /// <summary>
    /// Returns the status of the task
    /// </summary>
    /// <param name="taskId">The taskId of the task</param>
    /// <returns></returns>
    public TaskStatus GetTaskStatus(string taskId)
    {
      var status = GetTaskStatues(taskId).Single();

      return status.Item2;
    }

    /// <summary>
    /// Returns the list status of the tasks
    /// </summary>
    /// <param name="taskIds">The list of taskIds</param>
    /// <returns></returns>
    public IEnumerable<Tuple<string, TaskStatus>> GetTaskStatues(params string[] taskIds)
    {
      return ControlPlaneService.GetTaskStatus(new()
      {
        TaskIds =
        {
          taskIds,
        },
      }).IdStatuses.Select(x => Tuple.Create(x.TaskId,
                                           x.Status));
    }

    /// <summary>
    /// Return the taskOutput when error occurred
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public Output GetTaskOutputInfo(string taskId)
    {
      return ControlPlaneService.TryGetTaskOutput(new TaskOutputRequest()
      {
        TaskId  = taskId,
        Session = SessionId.Id
      });
    }


    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadsWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    [UsedImplicitly]
    public IEnumerable<TaskResultId> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadsWithDependencies)
    {
      var tuples = payloadsWithDependencies as Tuple<byte[], IList<string>>[] ?? payloadsWithDependencies.ToArray();


      //ReTriggerBuffer();

      var payloadsWithTaskIdAndDependencies = tuples.Select(payload =>
      {
        var taskId = Guid.NewGuid().ToString();
        return Tuple.Create(Tuple.Create(taskId,
                                         payload.Item1),
                            payload.Item2);
      }).ToArray();


      //foreach (var payload in payloadsWithTaskIdAndDependencies)
      //  bufferPayloads.Post(payload);

      //return payloadsWithTaskIdAndDependencies.Select(p => p.Item1.Item1).ToList();
      return SubmitTasksWithDependencies(payloadsWithTaskIdAndDependencies);
    }

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadsWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <param name="maxRetries">Set the number of retries Default Value 5</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    [UsedImplicitly]
    public IEnumerable<TaskResultId> SubmitTasksWithDependencies(IEnumerable<Tuple<Tuple<string, byte[]>, IList<string>>> payloadsWithDependencies, int maxRetries = 5)
    {
      using var _                = Logger.LogFunction();
      var       withDependencies = payloadsWithDependencies as Tuple<Tuple<string, byte[]>, IList<string>>[] ?? payloadsWithDependencies.ToArray();
      Logger.LogDebug("payload with dependencies {len}",
                      withDependencies.Count());
      var taskCreated = new List<TaskResultId>();

      withDependencies.Batch(1000).ToList().ForEach(tup =>
      {
        var taskRequests = new List<TaskRequest>();
        foreach (var (payload, dependencies) in tup)
        {
          var taskId = payload.Item1;

          Logger.LogDebug("Create task {task}",
                          taskId);
          var taskRequest = new TaskRequest
          {
            Payload = ByteString.CopyFrom(payload.Item2),

            ExpectedOutputKeys =
            {
              taskId,
            },
          };

          if (dependencies != null && dependencies.Count != 0)
          {
            taskRequest.DataDependencies.AddRange(dependencies);

            Logger.LogDebug("Dependencies : {dep}",
                            string.Join(", ",
                                        dependencies.Select(item => item.ToString())));
          }

          taskRequests.Add(taskRequest);
        }

        var nbRetry = 0;
        while (true)
        {
          try
          {
            var createTaskReply = ControlPlaneService.CreateTasksAsync(SessionId.Id,
                                                                       TaskOptions,
                                                                       taskRequests).Result;

            switch (createTaskReply.ResponseCase)
            {
              case CreateTaskReply.ResponseOneofCase.None:
                throw new Exception("Issue with Server !");
              case CreateTaskReply.ResponseOneofCase.CreationStatusList:
                taskCreated.AddRange(createTaskReply.CreationStatusList.CreationStatuses
                                                    .Where(status => status.StatusCase != CreateTaskReply.Types.CreationStatus.StatusOneofCase.Error).Select(
                                                      status => new TaskResultId
                                                      {
                                                        SessionId = SessionId.Id,
                                                        TaskId    = status.TaskInfo.TaskId,
                                                        ResultIds = status.TaskInfo.ExpectedOutputKeys,
                                                      }));
                break;
              case CreateTaskReply.ResponseOneofCase.Error:
                throw new Exception("Error while creating tasks");
              default:
                throw new ArgumentOutOfRangeException();
            }

            break;
          }
          catch (Exception e)
          {
            if (nbRetry >= maxRetries)
            {
              throw;
            }

            switch (e)
            {
              case AggregateException { InnerException: RpcException } ex:
                Logger.LogWarning(ex.InnerException,
                                  "Failure to submit");
                break;
              case AggregateException { InnerException: IOException } ex:
                Logger.LogWarning(ex.InnerException,
                                  "IOException : Failure to submit, Retrying");
                break;
              case IOException ex:
                Logger.LogWarning(ex,
                                  "IOException Failure to submit");
                break;
              default:
                throw;
            }

            nbRetry++;
          }
        }
      });
      Logger.LogDebug("Tasks created : {ids}",
                      taskCreated);

      return taskCreated;
    }

    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskId">
    ///   The task taskId of the task to wait for
    /// </param>
    /// <param name="retry">Option variable to set the number of retry (Default: 5)</param>
    public void WaitForTaskCompletion(string taskId, int retry = 5)
    {
      using var _ = Logger.LogFunction(taskId);

      WaitForTasksCompletion(new[]
      {
        taskId
      });
    }


    /// <summary>
    ///   User method to wait for only the parent task from the client
    /// </summary>
    /// <param name="taskIds">List of taskIds
    /// </param>
    [UsedImplicitly]
    public void WaitForTasksCompletion(IEnumerable<string> taskIds)
    {
      var ids = taskIds as string[] ?? taskIds.ToArray();
      using var _ = Logger.LogFunction(string.Join(", ",
                                                   ids));
      Retry.WhileException(5,
                           200,
                           () =>
                           {
                             var __ = ControlPlaneService.WaitForCompletion(new WaitRequest
                             {
                               Filter = new TaskFilter
                               {
                                 Task = new TaskFilter.Types.IdsRequest
                                 {
                                   Ids =
                                   {
                                     ids,
                                   },
                                 },
                               },
                               StopOnFirstTaskCancellation = true,
                               StopOnFirstTaskError        = true,
                             });
                           },
                           true,
                           typeof(IOException),
                           typeof(RpcException));
    }

    /// <summary>
    /// Get the result status of a list of taskId
    /// </summary>
    /// <param name="taskIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A ResultCollection sorted by Status Completed, Result in Error or missing</returns>
    public ResultStatusCollection GetResultStatus(IEnumerable<ResultIds> taskIds, CancellationToken cancellationToken = default)
    {
      IEnumerable<Tuple<ResultIds, ResultStatus>> idStatus = Retry.WhileException(5,
                                                                               200,
                                                                               () =>
                                                                               {
                                                                                 var resultStatusReply = ControlPlaneService.GetResultStatus(new GetResultStatusRequest()
                                                                                 {
                                                                                   ResultIds =
                                                                                   {
                                                                                     taskIds.Select(id => id.Ids.Single()),
                                                                                   },
                                                                                   SessionId = SessionId.Id,
                                                                                 });
                                                                                 return resultStatusReply.IdStatuses.Select(x => Tuple.Create(new ResultIds
                                                                                                                                              {
                                                                                                                                                Ids = new List<string>
                                                                                                                                                {
                                                                                                                                                  x.ResultId,
                                                                                                                                                },
                                                                                                                                                SessionId = SessionId.Id,
                                                                                                                                              },
                                                                                                                                              x.Status));
                                                                               },
                                                                               true,
                                                                               typeof(IOException),
                                                                               typeof(RpcException));


      var ids       = taskIds.SelectMany(id => id.Ids);
      var statusIds = idStatus as Tuple<ResultIds, ResultStatus>[] ?? idStatus.ToArray();

      var resultStatusList = new ResultStatusCollection()
      {
        IdsResultError = statusIds.Where(x => x.Item2 is ResultStatus.Aborted or ResultStatus.Unspecified),
        IdsError = ids.Where(x => statusIds.All(rId => rId.Item1.Ids.Single() != x)).Select(x => new ResultIds
        {
          Ids = new List<string>
          {
            x,
          },
          SessionId = SessionId.Id,
        }),
        IdsReady    = statusIds.Where(x => x.Item2 is ResultStatus.Completed),
        IdsNotReady = statusIds.Where(x => x.Item2 is ResultStatus.Created),
      };

      return resultStatusList;
    }

    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <param name="cancellationToken">The optional cancellationToken</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] GetResult(ResultIds taskId, CancellationToken cancellationToken = default)
    {
      using var _ = Logger.LogFunction(taskId.Ids.Single());
      var resultRequest = new ResultRequest
      {
        ResultId     = taskId.Ids.Single(),
        Session = SessionId.Id,
      };

      byte[] res = null;

      Retry.WhileException(5,
                           200,
                           () =>
                           {
                             var availabilityReply = ControlPlaneService.WaitForAvailability(resultRequest,
                                                                                             cancellationToken: cancellationToken);

                             switch (availabilityReply.TypeCase)
                             {
                               case AvailabilityReply.TypeOneofCase.None:
                                 throw new Exception("Issue with Server !");
                               case AvailabilityReply.TypeOneofCase.Ok:
                                 break;
                               case AvailabilityReply.TypeOneofCase.Error:
                                 throw new ClientResultsException(
                                   $"result in Error - {taskId.Ids.Single()}\nMessage :\n{string.Join("Inner message:\n", availabilityReply.Error.Errors)}");
                               case AvailabilityReply.TypeOneofCase.NotCompletedTask:
                                 throw new DataException($"Task {taskId} was not yet completed");
                               default:
                                 throw new ArgumentOutOfRangeException();
                             }
                           },
                           true,
                           typeof(IOException),
                           typeof(RpcException));

      res = TryGetResult(taskId,
                         cancellationToken: cancellationToken);

      if (res != null)
      {
        return res;
      }
      else
      {
        throw new ClientResultsException($"Cannot retrieve result {taskId.Ids.Single()}");
      }
    }


    /// <summary>
    ///   Method to GetResults when the result is returned by a task
    /// </summary>
    /// <param name="taskIds">The Task Ids list of the tasks which the result is expected</param>
    /// <param name="cancellationToken">The optional cancellationToken</param>
    /// <returns>return a dictionary with key taskId and payload</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public IEnumerable<Tuple<ResultIds, byte[]>> GetResults(IEnumerable<ResultIds> taskIds, CancellationToken cancellationToken = default)
    {
      return taskIds.Select(id =>
      {
        var res = GetResult(id,
                            cancellationToken);

        return new Tuple<ResultIds, byte[]>(id,
                                            res);
      });
    }

    /// <summary>
    /// Try to get the result if it is available
    /// </summary>
    /// <param name="resultRequest">Request specifying the result to fetch</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<byte[]> TryGetResultAsync(ResultRequest                  resultRequest,
                                                       CancellationToken              cancellationToken = default)
    {
      var streamingCall = ControlPlaneService.TryGetResultStream(resultRequest,
                                                    cancellationToken: cancellationToken);

      var result = new List<byte>();

      while (await streamingCall.ResponseStream.MoveNext(cancellationToken))
      {
        var reply = streamingCall.ResponseStream.Current;

        switch (reply.TypeCase)
        {
          case ResultReply.TypeOneofCase.Result:
            if (!reply.Result.DataComplete)
            {
              if (MemoryMarshal.TryGetArray(reply.Result.Data.Memory,
                                            out var segment))
              {
                // Success. Use the ByteString's underlying array.
                result.AddRange(segment);
              }
              else
              {
                // TryGetArray didn't succeed. Fall back to creating a copy of the data with ToByteArray.
                result.AddRange(reply.Result.Data.ToByteArray());
              }
            }

            break;
          case ResultReply.TypeOneofCase.None:
            return null;

          case ResultReply.TypeOneofCase.Error:
            throw new($"Error in task {reply.Error.TaskId} {string.Join("Message is : ", reply.Error.Errors.Select(x => x.Detail))}");

          case ResultReply.TypeOneofCase.NotCompletedTask:
            return null;

          default:
            throw new ArgumentOutOfRangeException("Got a reply with an unexpected message type.",
                                                  (Exception)null);
        }
      }

      return result.ToArray();
    }


    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <param name="checkOutput"></param>
    /// <param name="cancellationToken">The optional cancellationToken</param>
    /// <returns>Returns the result or byte[0] if there no result or null if task is not yet ready</returns>
    [UsedImplicitly]
    public byte[] TryGetResult(ResultIds taskId, bool checkOutput = true, CancellationToken cancellationToken = default)
    {
      using var _ = Logger.LogFunction(taskId.Ids.Single());
      var resultRequest = new ResultRequest
      {
        ResultId     = taskId.Ids.Single(),
        Session = SessionId.Id,
      };

      var resultReply = Retry.WhileException(5,
                                             200,
                                             () =>
                                             {
                                               try
                                               {
                                                 var response = TryGetResultAsync(resultRequest,
                                                                                                  cancellationToken).Result;
                                                 return response;
                                               }
                                               catch (AggregateException ex)
                                               {
                                                 if (ex.InnerException == null)
                                                 {
                                                   throw;
                                                 }

                                                 var rpcException = ex.InnerException;

                                                 switch (rpcException)
                                                 {
                                                   //Not yet available return from the tryGetResult
                                                   case RpcException { StatusCode: StatusCode.NotFound }:
                                                     return null;

                                                   //We lost the communication rethrow to retry :
                                                   case RpcException { StatusCode: StatusCode.Unavailable }:
                                                     throw;

                                                   case RpcException { StatusCode: StatusCode.Aborted or StatusCode.Cancelled }:

                                                     Logger.LogError(rpcException,
                                                                     rpcException.Message);
                                                     return null;

                                                   default:
                                                     throw;
                                                 }
                                               }
                                             },
                                             true,
                                             typeof(IOException),
                                             typeof(RpcException));

      return resultReply;
    }

    /// <summary>
    /// Try to get result of a list of taskIds 
    /// </summary>
    /// <param name="taskIds"></param>
    /// <returns>Returns an Enumerable pair of </returns>
    public IList<Tuple<ResultIds, byte[]>> TryGetResults(IEnumerable<ResultIds> taskIds)
    {
      var resultStatus = GetResultStatus(taskIds);

      if (!resultStatus.IdsReady.Any() && !resultStatus.IdsNotReady.Any())
      {
        if (resultStatus.IdsError.Any() || resultStatus.IdsResultError.Any())
        {
          var msg =
            $"The missing result is in error or canceled. Please check log for more information on Armonik grid server list of results in Error : [ {string.Join(", ", resultStatus.IdsResultError.Select(x => x.Item1))}";

          if (resultStatus.IdsError.Any())
          {
            if (resultStatus.IdsResultError.Any())
              msg += ", ";

            msg += $"{string.Join(", ", resultStatus.IdsError)}";
          }

          msg += $" ]\n";

          Logger.LogError(msg);

          throw new ClientResultsException(msg);
        }
      }


      return resultStatus.IdsReady.Select(pair =>
      {
        var (id, _) = pair;

        var res = TryGetResult(id,
                               false);
        return res == null
          ? null
          : new Tuple<ResultIds, byte[]>(id,
                                      res);
      }).Where(el => el != null).ToList();
    }
  }
}