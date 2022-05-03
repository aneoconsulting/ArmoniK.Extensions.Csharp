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
using ArmoniK.Extensions.Common.StreamWrapper.Client;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Common.Submitter
{
  /// <summary>
  /// 
  /// </summary>
  public class BaseClientSubmitter<T>
  {
    public BaseClientSubmitter(ILoggerFactory loggerFactory)
    {
      Logger        = loggerFactory.CreateLogger<T>();
      LoggerFactory = loggerFactory;
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

    protected ILoggerFactory LoggerFactory { get; set; }

    protected ILogger<T> Logger { get; set; }

    /// <summary>
    /// The submitter and receiver Service to submit, wait and get the result
    /// </summary>
    protected Api.gRPC.V1.Submitter.SubmitterClient ControlPlaneService { get; set; }


    /// <summary>
    /// Check if any error occurred during the result retrieving 
    /// </summary>
    /// <param name="taskId">the taskId to check</param>
    [UsedImplicitly]
    public void HealthCheckResult(string taskId)
    {
      var resultRequest = new ResultRequest
      {
        Key     = taskId,
        Session = SessionId.Id,
      };

      Retry.WhileException(5,
                           200,
                           () =>
                           {
                             var taskOutput = ControlPlaneService.TryGetTaskOutput(resultRequest);

                             switch (taskOutput.TypeCase)
                             {
                               case Output.TypeOneofCase.None:
                                 Logger.LogError($"Issue retrieving result of task : {taskId} from session {SessionId.Id}");
                                 throw new Exception("Issue with Server !");
                               case Output.TypeOneofCase.Ok:
                                 break;
                               case Output.TypeOneofCase.Error:
                                 throw new Exception($"HealthCheck result: Task in Error - {taskId}\nMessage :\n[{taskOutput.Error.Details}]");
                               default:
                                 throw new ArgumentOutOfRangeException();
                             }
                           },
                           true,
                           typeof(IOException));
    }

    /// <summary>
    ///   The method to submit several tasks with dependencies tasks. This task will wait for
    ///   to start until all dependencies are completed successfully
    /// </summary>
    /// <param name="payloadsWithDependencies">A list of Tuple(taskId, Payload) in dependence of those created tasks</param>
    /// <returns>return a list of taskIds of the created tasks </returns>
    [UsedImplicitly]
    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<byte[], IList<string>>> payloadsWithDependencies)
    {
      var tuples           = payloadsWithDependencies as Tuple<byte[], IList<string>>[] ?? payloadsWithDependencies.ToArray();
      var withDependencies = payloadsWithDependencies as Tuple<byte[], IList<string>>[] ?? tuples.ToArray();

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
    /// <returns>return a list of taskIds of the created tasks </returns>
    [UsedImplicitly]
    public IEnumerable<string> SubmitTasksWithDependencies(IEnumerable<Tuple<Tuple<string, byte[]>, IList<string>>> payloadsWithDependencies)
    {
      using var _                = Logger.LogFunction();
      var       withDependencies = payloadsWithDependencies as Tuple<Tuple<string, byte[]>, IList<string>>[] ?? payloadsWithDependencies.ToArray();
      Logger.LogDebug("payload with dependencies {len}",
                      withDependencies.Count());
      var taskRequests = new List<TaskRequest>();

      foreach (var (payload, dependencies) in withDependencies)
      {
        var taskId = payload.Item1;
        Logger.LogDebug("Create task {task}",
                        taskId);
        var taskRequest = new TaskRequest
        {
          Id      = taskId,
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

      var createTaskReply = ControlPlaneService.CreateTasksAsync(SessionId.Id,
                                                                 TaskOptions,
                                                                 taskRequests).Result;
      switch (createTaskReply.DataCase)
      {
        case CreateTaskReply.DataOneofCase.NonSuccessfullIds:
          throw new Exception($"NonSuccessFullIds : {createTaskReply.NonSuccessfullIds}");
        case CreateTaskReply.DataOneofCase.None:
          throw new Exception("Issue with Server !");
        case CreateTaskReply.DataOneofCase.Successfull:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      var taskCreated = taskRequests.Select(t => t.Id);

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

      WaitForTasksCompletion(new[] { taskId });
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
                           typeof(IOException));
    }

    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <param name="cancellationToken">The optional cancellationToken</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    public byte[] GetResult(string taskId, CancellationToken cancellationToken = default)
    {
      using var _ = Logger.LogFunction(taskId);
      var resultRequest = new ResultRequest
      {
        Key     = taskId,
        Session = SessionId.Id,
      };


      var res = TryGetResult(taskId,
                             checkOutput: false,
                             cancellationToken);
      if (res.Length != 0) return res;

      var availabilityReply = ControlPlaneService.WaitForAvailability(resultRequest,
                                                                      cancellationToken: cancellationToken);

      switch (availabilityReply.TypeCase)
      {
        case AvailabilityReply.TypeOneofCase.None:
          throw new Exception("Issue with Server !");
        case AvailabilityReply.TypeOneofCase.Ok:
          break;
        case AvailabilityReply.TypeOneofCase.Error:
          throw new Exception($"Task in Error - {taskId}\nMessage :\n{string.Join("Inner message:\n", availabilityReply.Error.Error)}");
        case AvailabilityReply.TypeOneofCase.NotCompletedTask:
          throw new DataException($"Task {taskId} was not yet completed");
        default:
          throw new ArgumentOutOfRangeException();
      }

      HealthCheckResult(taskId);


      res = TryGetResult(taskId,
                         cancellationToken: cancellationToken);

      if (res.Length != 0) return res;
      else
      {
        throw new ArgumentException($"Cannot retrieve result for taskId {taskId}");
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
    public IEnumerable<Tuple<string, byte[]>> GetResults(IEnumerable<string> taskIds, CancellationToken cancellationToken = default)
    {
      return taskIds.Select(id =>
      {
        var res = GetResult(id,
                            cancellationToken);

        return new Tuple<string, byte[]>(id,
                                         res);
      });
    }


    /// <summary>
    ///   Try to find the result of One task. If there no result, the function return byte[0]
    /// </summary>
    /// <param name="taskId">The task Id trying to get result</param>
    /// <param name="checkOutput"></param>
    /// <param name="cancellationToken">The optional cancellationToken</param>
    /// <param name="WaitForResult">Set to true if you want to set up to except when no result is received</param>
    /// <returns>Returns the result or byte[0] if there no result</returns>
    [UsedImplicitly]
    public byte[] TryGetResult(string taskId, bool checkOutput = true, CancellationToken cancellationToken = default)
    {
      using var _ = Logger.LogFunction(taskId);
      var resultRequest = new ResultRequest
      {
        Key     = taskId,
        Session = SessionId.Id,
      };

      var resultReply = Retry.WhileException(5,
                                             200,
                                             () =>
                                             {
                                               GetStatusReply status;
                                               try
                                               {
                                                 status = ControlPlaneService.GetStatus(new GetStatusrequest()
                                                 {
                                                   TaskId = taskId
                                                 });


                                                 switch (status.Status)
                                                 {
                                                   case TaskStatus.Creating:
                                                   case TaskStatus.Dispatched:
                                                   case TaskStatus.Processed:
                                                   case TaskStatus.Processing:
                                                   case TaskStatus.Submitted:
                                                     return new byte[] { };

                                                   case TaskStatus.Completed:

                                                     ////TODO: Workaround Waiting for a simple error from TryGetResultAsync

                                                     //var availabilityReply = ControlPlaneService.WaitForAvailability(new ResultRequest()
                                                     //                                                                {
                                                     //                                                                  Key     = taskId,
                                                     //                                                                  Session = SessionId.Id,
                                                     //                                                                },
                                                     //                                                                cancellationToken: cancellationToken);
                                                     //switch (availabilityReply.TypeCase)
                                                     //{
                                                     //  case AvailabilityReply.TypeOneofCase.None:
                                                     //    throw new Exception("Issue with Server !");
                                                     //  case AvailabilityReply.TypeOneofCase.Ok:
                                                     //    break;
                                                     //  case AvailabilityReply.TypeOneofCase.Error:
                                                     //    throw new Exception(
                                                     //      $"Task in Error - {taskId}\nMessage :\n{string.Join("Inner message:\n", availabilityReply.Error.Error)}");
                                                     //  case AvailabilityReply.TypeOneofCase.NotCompletedTask:
                                                     //    throw new DataException($"Task {taskId} was not yet completed");
                                                     //  default:
                                                     //    throw new ArgumentOutOfRangeException();
                                                     //}

                                                     break;

                                                   case TaskStatus.Timeout:
                                                   case TaskStatus.Failed:
                                                   case TaskStatus.Canceling:
                                                   case TaskStatus.Canceled:
                                                   case TaskStatus.Error:
                                                   case TaskStatus.Unspecified:
                                                     throw new IOException(
                                                       $"Result {taskId} cannot be retrieved. taskStatus is {Enum.GetName(typeof(TaskStatus), status.Status)}");
                                                   default:
                                                     break;
                                                 }
                                               }
                                               catch (Exception ex)
                                               {
                                                 Logger.LogWarning($"TaskId {taskId} not yet found");
                                                 throw new IOException($"{taskId} not yet in DB. Retrying");
                                               }

                                               Task<byte[]> response;
                                               try
                                               {
                                                 response = ControlPlaneService.TryGetResultAsync(resultRequest,
                                                                                                      cancellationToken);


                                                 if (response.Result != null && response.Result.Length != 0)
                                                   return response.Result;

                                                 
                                               }
                                               catch (Exception ex)
                                               {
                                                 throw ex;
                                               }

                                               if (!checkOutput) return response.Result;

                                               var taskOutput = ControlPlaneService.TryGetTaskOutput(resultRequest,
                                                                                                     cancellationToken: cancellationToken);
                                               if (taskOutput.Status != ArmoniK.Api.gRPC.V1.TaskStatus.Error)
                                                 return response.Result;

                                               switch (taskOutput.TypeCase)
                                               {
                                                 case Output.TypeOneofCase.None:
                                                 case Output.TypeOneofCase.Ok:
                                                   break;
                                                 case Output.TypeOneofCase.Error when !string.IsNullOrEmpty(taskOutput.Error.Details):
                                                   throw new Exception($"Task in Error - {taskId} : {taskOutput.Error.Details}");
                                                 default:
                                                   break;
                                               }

                                               return response.Result;
                                             },
                                             true,
                                             typeof(IOException));

      return resultReply;
    }

    /// <summary>
    /// Try to get result of a list of taskIds 
    /// </summary>
    /// <param name="taskIds"></param>
    /// <returns>Returns an Enumerable pair of </returns>
    public IEnumerable<Tuple<string, byte[]>> TryGetResults(IEnumerable<string> taskIds)
    {
      return taskIds.Select((id, idx) =>
      {
        var res = TryGetResult(id,
                               false);
        
        return res.Length == 0
          ? null
          : new Tuple<string, byte[]>(id,
                                      res);
      }).ToList().Where(el => el != null);
    }
  }
}