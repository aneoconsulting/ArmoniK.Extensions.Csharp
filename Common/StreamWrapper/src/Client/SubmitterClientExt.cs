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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf;

using JetBrains.Annotations;

namespace ArmoniK.Extensions.Common.StreamWrapper.Client
{
  /// <summary>
  /// Provide SubmitterClient with functions to use the stream capabilities of the client more easily.
  /// </summary>
  [PublicAPI]
  public static class SubmitterClientExt
  {
    /// <summary>
    /// Creates new tasks
    /// </summary>
    /// <param name="client">The <code>Submitter.SubmitterClient</code> client to use for the tasks creation.</param>
    /// <param name="sessionId">The sessionId for the new tasks.</param>
    /// <param name="taskOptions">The task options for the tasks.</param>
    /// <param name="taskRequests">The list of task to create.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns></returns>
    public static async Task<CreateTaskReply> CreateTasksAsync(this Submitter.SubmitterClient client,
                                                               string                         sessionId,
                                                               TaskOptions                    taskOptions,
                                                               IEnumerable<TaskRequest>       taskRequests,
                                                               CancellationToken              cancellationToken = default)
    {
      var serviceConfiguration = await client.GetServiceConfigurationAsync(new(),
                                                                           cancellationToken: cancellationToken);

      using var stream = client.CreateLargeTasks(cancellationToken: cancellationToken);

      foreach (var createLargeTaskRequest in taskRequests.ToRequestStream(sessionId,
                                                                          taskOptions,
                                                                          serviceConfiguration.DataChunkMaxSize))
      {
        await stream.RequestStream.WriteAsync(createLargeTaskRequest);
      }

      await stream.RequestStream.CompleteAsync();

      return await stream.ResponseAsync;
    }


    private static IEnumerable<CreateLargeTaskRequest> ToRequestStream(this IEnumerable<TaskRequest> taskRequests,
                                                                      string                        sessionId,
                                                                      TaskOptions                   taskOptions,
                                                                      int                           chunkMaxSize)
    {
      yield return new()
      {
        InitRequest = new()
        {
          SessionId   = sessionId,
          TaskOptions = taskOptions,
        },
      };

      using var taskRequestEnumerator = taskRequests.GetEnumerator();

      if (!taskRequestEnumerator.MoveNext())
      {
        yield break;
      }

      var currentRequest = taskRequestEnumerator.Current;

      while (taskRequestEnumerator.MoveNext())
      {
        foreach (var createLargeTaskRequest in currentRequest.ToRequestStream(false,
                                                                              chunkMaxSize))
        {
          yield return createLargeTaskRequest;
        }


        currentRequest = taskRequestEnumerator.Current;
      }

      foreach (var createLargeTaskRequest in currentRequest.ToRequestStream(true,
                                                                            chunkMaxSize))
      {
        yield return createLargeTaskRequest;
      }
    }

    private static IEnumerable<CreateLargeTaskRequest> ToRequestStream(this TaskRequest taskRequest,
                                                                      bool             isLast,
                                                                      int              chunkMaxSize)
    {
      yield return new()
      {
        InitTask = new()
        {
          Header = new()
          {
            DataDependencies =
            {
              taskRequest.DataDependencies,
            },
            ExpectedOutputKeys =
            {
              taskRequest.ExpectedOutputKeys,
            },
            Id = taskRequest.Id,
          },
        },
      };

      var start = 0;

      while (start < taskRequest.Payload.Length)
      {
        var chunkSize = Math.Min(chunkMaxSize,
                                 taskRequest.Payload.Length - start);

        yield return new()
        {
          TaskPayload = new()
          {
            Data = UnsafeByteOperations.UnsafeWrap(taskRequest.Payload.Memory.Slice(start,
                                                                                  chunkSize)),
          },
        };

        start += chunkSize;
      }

      yield return new()
      {
        TaskPayload = new()
        {
          DataComplete = true,
        },
      };

      if (isLast)
      {
        yield return new()
        {
          InitTask = new()
          {
            LastTask = true,
          },
        };
      }
    }


    public static async Task<byte[]> GetResultAsync(this Submitter.SubmitterClient client,
                                                    ResultRequest                  resultRequest,
                                                    CancellationToken              cancellationToken = default)
    {
      var streamingCall = client.TryGetResultStream(resultRequest,
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
            throw new ("Issue with Server !");
          case ResultReply.TypeOneofCase.Error:
            throw new ($"Error in task {reply.Error.TaskId}");
          case ResultReply.TypeOneofCase.NotCompletedTask:
            throw new ($"Task {reply.NotCompletedTask} not completed");
          default:
            throw new ArgumentOutOfRangeException("Got a reply with an unexpected message type.",
                                                  (Exception)null);
        }
      } 

      return result.ToArray();
    }

    /// <summary>
    /// Try to get the result if it is available
    /// </summary>
    /// <param name="client">The <code>Submitter.SubmitterClient</code> client to use for the result retrieval.</param>
    /// <param name="resultRequest">Request specifying the result to fetch</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static async Task<byte[]> TryGetResultAsync(this Submitter.SubmitterClient client,
                                                    ResultRequest                  resultRequest,
                                                    CancellationToken              cancellationToken = default)
    {
      var streamingCall = client.TryGetResultStream(resultRequest,
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
            throw new($"Error in task {reply.Error.TaskId} {string.Join("Message is : ", reply.Error.Error.Select(x => x.Detail))}");

          case ResultReply.TypeOneofCase.NotCompletedTask:
            return null;

          default:
            throw new ArgumentOutOfRangeException("Got a reply with an unexpected message type.",
                                                  (Exception)null);
        }
      }

      return result.ToArray();
    }
  }
}